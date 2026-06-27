using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    public partial struct TargetSearchSystem : ISystem
    {
        private EntityQuery _coordinatorQuery;
        private EntityQuery _beaconQuery;
        private CollisionFilter _targetFilter;

        // Not [BurstCompile] — LayerMask.NameToLayer is a managed call.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleCoordinator>();
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _coordinatorQuery = SystemAPI.QueryBuilder()
                .WithAll<BattleCoordinator, EnemyUnitReference, AllyUnitReference>()
                .Build();

            _beaconQuery = SystemAPI.QueryBuilder().WithAll<BeaconTag, LocalToWorld>().Build();

            // Targeting broadphase filter. Units actually live on the GRABBABLE layer (8) — they're grabbable/
            // throwable, NOT on the "Unit" layer (which no unit prefab uses) — plus walls on Obstacle (9). The
            // Unit bit is kept defensively in case a future unit is placed there. The beacon sits on Default and
            // is excluded (scored separately, no range gate, D6). Non-unit/non-wall hits on these layers are
            // rejected by UnitLookup/WallLookup in the collector, so the extra membership is safe.
            var unitBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Unit);
            var grabbableBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Grabbable);
            var obstacleBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Obstacle);
            _targetFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = unitBit | grabbableBit | obstacleBit,
                GroupIndex = 0,
            };
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TargetProfiles>(out var profilesConfig)) return;
        
            var coordEntity = _coordinatorQuery.GetSingletonEntity();
            var coord = SystemAPI.GetComponent<BattleCoordinator>(coordEntity);
        
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            foreach (var (expirationTimestamp, cooldownEnabled) in SystemAPI.Query<RefRO<TargetSearchCooldownExpirationTimestamp>, EnabledRefRW<TargetSearchCooldownExpirationTimestamp>>())
            {
                if (expirationTimestamp.ValueRO.Value > elapsedTime) continue;
                cooldownEnabled.ValueRW = false;
            }
            // Log.Battle.D($"Target search system, coordinator data: battle active: {coord.IsBattleActive}, force update: {coord.ForceGlobalReevaluation}");

            var enemies = state.EntityManager.GetBuffer<EnemyUnitReference>(coordEntity, true).Reinterpret<Entity>().AsNativeArray();
            var allies = state.EntityManager.GetBuffer<AllyUnitReference>(coordEntity, true).Reinterpret<Entity>().AsNativeArray();

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var beaconEnt = Entity.Null;
            if (_beaconQuery.CalculateEntityCount() > 0) beaconEnt = _beaconQuery.GetSingletonEntity();

            var targetSnapshot = new NativeParallelHashMap<Entity, Entity>(enemies.Length + allies.Length, Allocator.TempJob);
            state.Dependency = new SnapshotTargetsJob
            {
                Enemies = enemies,
                Allies = allies,
                TargetLookup = SystemAPI.GetComponentLookup<Target>(true),
                Snapshot = targetSnapshot,
            }.Schedule(state.Dependency);

            var job = new TargetScorerJob
            {
                ProfilesBlob = profilesConfig.Blob,

                PhysicsWorld = physicsWorld.PhysicsWorld,
                TargetFilter = _targetFilter,

                BeaconEntity = beaconEnt,

                UnitLookup = SystemAPI.GetComponentLookup<Unit>(true),
                WallLookup = SystemAPI.GetComponentLookup<WallSection>(true),
                EnemyTypeLookup = SystemAPI.GetComponentLookup<EnemyUnitType>(true),
                AllyTypeLookup = SystemAPI.GetComponentLookup<AllyUnitType>(true),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                BeaconBoundsLookup = SystemAPI.GetComponentLookup<TargetBounds>(true),
                TargetSnapshot = targetSnapshot,

                ElapsedTime = elapsedTime,
                IsBattleActive = coord.IsBattleActive,
                ForceUpdate = coord.ForceGlobalReevaluation,
                CastleIsBreached = coord.WasCastleBreached,
                IsDayPhaseActive = coord.IsDayPhaseActive,
            };
        
            if (coord.ForceGlobalReevaluation)
            {
                var coordRW = SystemAPI.GetComponentRW<BattleCoordinator>(coordEntity);
                coordRW.ValueRW.ForceGlobalReevaluation = false;
            }

            state.Dependency = job.ScheduleParallel(state.Dependency);

            state.Dependency = targetSnapshot.Dispose(state.Dependency);
        }

        [BurstCompile]
        private struct SnapshotTargetsJob : IJob
        {
            [ReadOnly] public NativeArray<Entity> Enemies;
            [ReadOnly] public NativeArray<Entity> Allies;
            [ReadOnly] public ComponentLookup<Target> TargetLookup;
            public NativeParallelHashMap<Entity, Entity> Snapshot;

            public void Execute()
            {
                AddRange(Enemies);
                AddRange(Allies);
            }

            private void AddRange(NativeArray<Entity> units)
            {
                for (var i = 0; i < units.Length; i++)
                {
                    var unit = units[i];
                    if (TargetLookup.HasComponent(unit)) Snapshot.TryAdd(unit, TargetLookup[unit].TargetEntity);
                }
            }
        }
    }
}