using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;

using Random = Unity.Mathematics.Random;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateAfter(typeof(BattleCoordinatorSystem))]
    public partial struct EnemyEscapeSystem : ISystem
    {
        private uint _groundLayerBit;

        // OnCreate is not [BurstCompile] — LayerMask.NameToLayer is a managed call
        public void OnCreate(ref SystemState state)
        {
            _groundLayerBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground);

            state.RequireForUpdate<BattleCoordinator>();
            state.RequireForUpdate<FactionBases>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bases = SystemAPI.GetSingleton<FactionBases>();
            if (!bases.IsInitialized) return;

            var coordinator = SystemAPI.GetSingleton<BattleCoordinator>();

            // The spawner is optional here — escapees still get destroyed in scenes without one.
            // PickupSettings + the PickupPrefabRef buffer live on the same baked spawner entity.
            var hasSpawner = SystemAPI.HasSingleton<PickupSettings>();
            var settings = hasSpawner ? SystemAPI.GetSingleton<PickupSettings>() : default;

            var map = default(PickupPrefabMap);
            if (hasSpawner)
            {
                var prefabBuffer = SystemAPI.GetSingletonBuffer<PickupPrefabRef>(true);
                for (var i = 0; i < prefabBuffer.Length; i++) map.Entries.Add(prefabBuffer[i]);
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new EnemyEscapeJob
            {
                EnemyBaseBounds = bases.EnemyBaseBounds,
                IsDayPhaseActive = coordinator.IsDayPhaseActive,
                DeltaTime = SystemAPI.Time.DeltaTime,
                PrefabMap = map,
                Settings = settings,
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                GroundLayerBit = _groundLayerBit,
                Seed = (uint)math.max(1, (int)(SystemAPI.Time.ElapsedTime * 10007)),
                ECB = ecb,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithPresent(typeof(HasLeftBase), typeof(Escaped))]
    public partial struct EnemyEscapeJob : IJobEntity
    {
        private const float EscapeDwellSeconds = 2f;

        public Aabb EnemyBaseBounds;
        public bool IsDayPhaseActive;
        public float DeltaTime;
        public PickupPrefabMap PrefabMap;
        public PickupSettings Settings;
        [ReadOnly] public CollisionWorld CollisionWorld;
        public uint GroundLayerBit;
        public uint Seed;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            in LocalToWorld worldTransform,
            in EmotionalState emotion,
            [ReadOnly] DynamicBuffer<ResourceDrop> drops,
            ref HasLeftBase hasLeftBaseData,
            EnabledRefRW<HasLeftBase> hasLeftBase,
            EnabledRefRW<Escaped> escaped)
        {
            if (escaped.ValueRO) return;

            var insideBase = EnemyBaseBounds.Contains(worldTransform.Position);

            if (!hasLeftBase.ValueRO)
            {
                if (!insideBase) hasLeftBase.ValueRW = true;
                hasLeftBaseData.DwellTimer = 0f;
                hasLeftBaseData.LastOutsidePosition = worldTransform.Position;
                return;
            }

            if (!insideBase)
            {
                hasLeftBaseData.LastOutsidePosition = worldTransform.Position;
                hasLeftBaseData.DwellTimer = 0f;
                return;
            }

            // Has left base before and now back inside — escape only after dwelling 2s while day ended or scared.
            var isScared = emotion.Value == Emotion.Scared;
            var qualifies = !IsDayPhaseActive || isScared;
            if (!qualifies)
            {
                hasLeftBaseData.DwellTimer = 0f;
                return;
            }

            hasLeftBaseData.DwellTimer += DeltaTime;
            if (hasLeftBaseData.DwellTimer < EscapeDwellSeconds) return;

            escaped.ValueRW = true;

            // Scared escapees drop HALF their rolled loot at the base boundary (reachable by the player).
            if (isScared)
            {
                var rand = Random.CreateFromIndex(Seed + (uint)entity.Index * 2654435761u);

                for (var i = 0; i < drops.Length; i++)
                {
                    var drop = drops[i];

                    var idx = (int)drop.Type;
                    if (idx < 0 || idx >= PrefabMap.Entries.Length) continue;

                    var prefabEntry = PrefabMap.Entries[idx];
                    if (prefabEntry.Prefab == Entity.Null) continue;

                    if (rand.NextFloat() > drop.Chance) continue;

                    var halfCount = rand.NextInt(drop.MinCount, drop.MaxCount + 1) / 2;
                    if (halfCount <= 0) continue;

                    PickupSpawnUtility.Spawn(ref ECB, sortKey, prefabEntry.Prefab, prefabEntry.Scale, Settings, in CollisionWorld, GroundLayerBit, drop.Type, hasLeftBaseData.LastOutsidePosition, halfCount, drop.Value, ref rand);
                }
            }

            ECB.DestroyEntity(sortKey, entity);
        }
    }
}
