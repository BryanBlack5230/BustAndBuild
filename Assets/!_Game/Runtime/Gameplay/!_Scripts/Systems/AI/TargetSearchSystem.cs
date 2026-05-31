using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    public partial struct TargetSearchSystem : ISystem
    {
        private EntityQuery _coordinatorQuery;
        private EntityQuery _wallQuery;
        private EntityQuery _beaconQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleCoordinator>();
        
            _coordinatorQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BattleCoordinator>(),
                ComponentType.ReadOnly<EnemyUnitReference>(),
                ComponentType.ReadOnly<AllyUnitReference>()
            );

            _wallQuery = SystemAPI.QueryBuilder().WithAll<WallSection, LocalToWorld>().Build();
            _beaconQuery = SystemAPI.QueryBuilder().WithAll<BeaconTag, LocalToWorld>().Build();
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
        
            var wallEntities = _wallQuery.ToEntityArray(Allocator.TempJob);
            var wallTransforms = _wallQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

            var beaconEnt = Entity.Null;
            if (_beaconQuery.CalculateEntityCount() > 0) beaconEnt = _beaconQuery.GetSingletonEntity();
        
            var job = new TargetScorerJob
            {
                ProfilesBlob = profilesConfig.Blob,
            
                GlobalEnemies = enemies,
                GlobalAllies = allies,
            
                WallEntities = wallEntities,
                WallTransforms = wallTransforms,
            
                BeaconEntity = beaconEnt,
            
                UnitLookup = SystemAPI.GetComponentLookup<Unit>(true),
                EnemyTypeLookup = SystemAPI.GetComponentLookup<EnemyUnitType>(true),
                AllyTypeLookup = SystemAPI.GetComponentLookup<AllyUnitType>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                TargetLookup = SystemAPI.GetComponentLookup<Target>(true),
            
                ElapsedTime = elapsedTime,
                IsBattleActive = coord.IsBattleActive,
                ForceUpdate = coord.ForceGlobalReevaluation,
                CastleIsBreached = coord.WasCastleBreached
            };
        
            if (coord.ForceGlobalReevaluation)
            {
                var coordRW = SystemAPI.GetComponentRW<BattleCoordinator>(coordEntity);
                coordRW.ValueRW.ForceGlobalReevaluation = false;
            }

            state.Dependency = job.ScheduleParallel(state.Dependency);
        
            state.Dependency = wallEntities.Dispose(state.Dependency);
            state.Dependency = wallTransforms.Dispose(state.Dependency);
        }
    }
}