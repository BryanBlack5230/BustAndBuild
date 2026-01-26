using GameManagement;
using Unity.Entities;

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
        
        var enemyBuffer = SystemAPI.GetBuffer<EnemyUnitReference>(coordEntity);
        var allyBuffer = SystemAPI.GetBuffer<AllyUnitReference>(coordEntity);
        
        var castleEntity = SystemAPI.GetSingletonEntity<Castle>();
        var isBreached = SystemAPI.GetComponent<Castle>(castleEntity).hasBeenBreached;

        var wallChanged = isBreached != coordinator.WasCastleBreached;
        coordinator.WasCastleBreached = isBreached;

        coordinator.IsBattleActive = enemyBuffer.Length > 0 && allyBuffer.Length > 0;
        coordinator.ForceGlobalReevaluation = coordinator.ForceGlobalReevaluation || wallChanged;
    }
}