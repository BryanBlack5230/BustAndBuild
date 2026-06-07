using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

using Random = Unity.Mathematics.Random;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateAfter(typeof(BattleCoordinatorSystem))]
    public partial struct EnemyEscapeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleCoordinator>();
            state.RequireForUpdate<FactionBases>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bases = SystemAPI.GetSingleton<FactionBases>();
            if (!bases.IsInitialized) return;

            var coordinator = SystemAPI.GetSingleton<BattleCoordinator>();

            var prefab = SystemAPI.HasSingleton<PearlSpawnPrefab>()
                ? SystemAPI.GetSingleton<PearlSpawnPrefab>().Prefab
                : Entity.Null;
            var settings = SystemAPI.HasSingleton<PearlSettings>()
                ? SystemAPI.GetSingleton<PearlSettings>()
                : default;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new EnemyEscapeJob
            {
                EnemyBaseBounds = bases.EnemyBaseBounds,
                IsDayPhaseActive = coordinator.IsDayPhaseActive,
                DeltaTime = SystemAPI.Time.DeltaTime,
                PearlPrefab = prefab,
                Settings = settings,
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
        public Entity PearlPrefab;
        public PearlSettings Settings;
        public uint Seed;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            in LocalToWorld worldTransform,
            in EmotionalState emotion,
            in PearlDropOnDeath drop,
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

            if (isScared && PearlPrefab != Entity.Null)
            {
                var rand = Random.CreateFromIndex(Seed + (uint)entity.Index * 2654435761u);
                var rolled = rand.NextInt(drop.MinCount, drop.MaxCount + 1);
                var halfCount = rolled / 2;
                var source = hasLeftBaseData.LastOutsidePosition;

                for (var i = 0; i < halfCount; i++)
                {
                    var angle = rand.NextFloat(0f, math.PI2);
                    var radius = math.sqrt(rand.NextFloat()) * Settings.Scatter;
                    var pos = new float3(source.x + math.cos(angle) * radius, source.y + Settings.SpawnHeight, source.z + math.sin(angle) * radius);

                    var pearl = ECB.Instantiate(sortKey, PearlPrefab);
                    ECB.SetComponent(sortKey, pearl, LocalTransform.FromPosition(pos));
                    ECB.SetComponent(sortKey, pearl, new Pearl { Value = drop.ValuePerPearl });
                    ECB.SetComponent(sortKey, pearl, new PearlLifetime
                    {
                        TimeRemaining = Settings.Lifetime,
                        NextBlinkToggleAt = 0f,
                        VisibleState = 1,
                    });
                    ECB.SetComponent(sortKey, pearl, new PearlFloat
                    {
                        Amplitude = Settings.FloatAmplitude,
                        Period = Settings.FloatPeriod,
                        PhaseOffset = rand.NextFloat(0f, math.PI2),
                        RestY = pos.y,
                        RestTimer = 0f,
                    });
                    ECB.SetComponent(sortKey, pearl, new PhysicsGravityFactor { Value = 1f });
                    ECB.SetComponentEnabled<PearlSettled>(sortKey, pearl, false);
                }
            }

            ECB.DestroyEntity(sortKey, entity);
        }
    }
}
