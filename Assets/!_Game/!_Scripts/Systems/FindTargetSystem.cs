using GameManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct FindTargetSystem : ISystem
{
    private EntityQuery _coordinatorQuery;
    private EntityQuery _wallQuery;
    private EntityQuery _beaconQuery;
    private EntityQuery _castleQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleCoordinator>();
        
        _coordinatorQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BattleCoordinator>(),
            ComponentType.ReadOnly<EnemyUnitReference>(),
            ComponentType.ReadOnly<AllyUnitReference>()
        );

        _wallQuery = SystemAPI.QueryBuilder().WithAll<WallSection, LocalTransform>().Build();
        _beaconQuery = SystemAPI.QueryBuilder().WithAll<BeaconTag, LocalTransform>().Build();
        _castleQuery = SystemAPI.QueryBuilder().WithAll<Castle, LocalTransform>().Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<TargetProfiles>(out var profilesConfig)) return;
        
        var coordEntity = _coordinatorQuery.GetSingletonEntity();


        var enemies = state.EntityManager.GetBuffer<EnemyUnitReference>(coordEntity, true).Reinterpret<Entity>().AsNativeArray();
        var allies = state.EntityManager.GetBuffer<AllyUnitReference>(coordEntity, true).Reinterpret<Entity>().AsNativeArray();
        
        var wallEntities = _wallQuery.ToEntityArray(Allocator.TempJob);
        var wallTransforms = _wallQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);


        var beaconEnt = Entity.Null;
        if (_beaconQuery.CalculateEntityCount() > 0) beaconEnt = _beaconQuery.GetSingletonEntity();

        var castleBreached = false;
        if (_castleQuery.CalculateEntityCount() > 0)
        {
            castleBreached = _castleQuery.GetSingleton<Castle>().hasBeenBreached;
        }

        
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
            TargetLookup = SystemAPI.GetComponentLookup<Target>(true),
            
            DeltaTime = SystemAPI.Time.DeltaTime,
            CastleIsBreached = castleBreached
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        
        state.Dependency = wallEntities.Dispose(state.Dependency);
        state.Dependency = wallTransforms.Dispose(state.Dependency);
    }
}