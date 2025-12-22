using GameManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct FindTargetSystem : ISystem
{
    private EntityQuery _directorQuery;
    private EntityQuery _wallQuery;
    private EntityQuery _beaconQuery;
    private EntityQuery _castleQuery;
    
    private Entity _cachedBeacon;
    private Entity _cachedCastle;
    private bool _hasDiscoveredReferences;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleDirector>();
        state.RequireForUpdate<FindTargetConfigReference>();

        _directorQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BattleDirector>(),
            ComponentType.ReadOnly<EnemyUnitReference>(),
            ComponentType.ReadOnly<AllyUnitReference>()
        );

        _wallQuery = SystemAPI.QueryBuilder()
            .WithAll<WallSection, LocalTransform>()
            .Build();

        _beaconQuery = SystemAPI.QueryBuilder()
            .WithAll<BeaconTag>()
            .Build();

        _castleQuery = SystemAPI.QueryBuilder()
            .WithAll<Castle>()
            .Build();

        _hasDiscoveredReferences = false;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_hasDiscoveredReferences)
        {
            if (_beaconQuery.CalculateEntityCount() > 0 && _castleQuery.CalculateEntityCount() > 0)
            {
                _cachedBeacon = _beaconQuery.GetSingletonEntity();
                _cachedCastle = _castleQuery.GetSingletonEntity();
                _hasDiscoveredReferences = true;
            }
            else
            {
                return;
            }
        }
        
        var directorEntity = _directorQuery.GetSingletonEntity();
        var director = state.EntityManager.GetComponentData<BattleDirector>(directorEntity);
        // if (!director.IsDirty) return;

        director.IsDirty = false;
        state.EntityManager.SetComponentData(directorEntity, director);

        var configRef = SystemAPI.GetSingleton<FindTargetConfigReference>();
        var allyBuffer = state.EntityManager.GetBuffer<AllyUnitReference>(directorEntity, true);
        
        var wallEntities = _wallQuery.ToEntityArray(Allocator.TempJob);
        var wallTransforms = _wallQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var targetLookup = SystemAPI.GetComponentLookup<Target>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var castleData = state.EntityManager.GetComponentData<Castle>(_cachedCastle);
        
        var job = new FindTargetJob
        {
            ConfigBlob = configRef.ConfigBlob,
            AllyEntities = allyBuffer.Reinterpret<Entity>().AsNativeArray(),
            
            WallEntities = wallEntities,
            WallTransforms = wallTransforms,
            
            TargetLookup = targetLookup,
            TransformLookup = transformLookup,
            
            BeaconEntity = _cachedBeacon,
            CastleBreached = castleData.hasBeenBreached,
            
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        
        state.Dependency = wallEntities.Dispose(state.Dependency);
        state.Dependency = wallTransforms.Dispose(state.Dependency);
    }
}
