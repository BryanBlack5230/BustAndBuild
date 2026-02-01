using GameManagement;
using Unity.Entities;
using Unity.Physics;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateBefore(typeof(TargetSearchSystem))]
public partial struct BattleCoordinatorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BattleCoordinator>();
        state.RequireForUpdate<Castle>(); 
    }

    public void OnUpdate(ref SystemState state)
    {
        var coordEntity = SystemAPI.GetSingletonEntity<BattleCoordinator>();
        ref var coordinator = ref SystemAPI.GetComponentRW<BattleCoordinator>(coordEntity).ValueRW;
        ref var bases = ref SystemAPI.GetComponentRW<FactionBases>(coordEntity).ValueRW;
        
        if (!bases.IsInitialized)
        {
            SetBases(ref state, ref coordEntity, ref bases);
        }
        
        var enemyBuffer = SystemAPI.GetBuffer<EnemyUnitReference>(coordEntity);
        var allyBuffer = SystemAPI.GetBuffer<AllyUnitReference>(coordEntity);
        
        var castleEntity = SystemAPI.GetSingletonEntity<Castle>();
        var isBreached = SystemAPI.GetComponent<Castle>(castleEntity).hasBeenBreached;

        var wallChanged = isBreached != coordinator.WasCastleBreached;
        coordinator.WasCastleBreached = isBreached;

        coordinator.IsBattleActive = enemyBuffer.Length > 0 && allyBuffer.Length > 0;
        coordinator.ForceGlobalReevaluation = coordinator.ForceGlobalReevaluation || wallChanged;
    }

    private void SetBases(ref SystemState state, ref Entity battleCoordinatorEntity, ref FactionBases bases)
    {
        
        var baseQuery = SystemAPI.QueryBuilder()
            .WithAll<BaseArea, PhysicsCollider>() 
            .Build();

        if (baseQuery.CalculateEntityCount() < 2) return;
        
        var entities = baseQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        var areas = baseQuery.ToComponentDataArray<BaseArea>(Unity.Collections.Allocator.Temp);
        var colliders = baseQuery.ToComponentDataArray<PhysicsCollider>(Unity.Collections.Allocator.Temp);

        for (var i = 0; i < entities.Length; i++)
        {
            switch (areas[i].Faction)
            {
                case Faction.Ally:
                    bases.AllyBaseBounds = colliders[i].Value.Value.CalculateAabb();
                    break;
                case Faction.Enemy:
                    bases.EnemyBaseBounds = colliders[i].Value.Value.CalculateAabb();
                    break;
            }
        }

        bases.IsInitialized = true;
                
        entities.Dispose();
        areas.Dispose();
        colliders.Dispose();
    }
}