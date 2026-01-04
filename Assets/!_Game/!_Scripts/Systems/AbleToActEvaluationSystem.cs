using GameEngine.AI;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(UnitMoverSystem))]
public partial struct AbleToActEvaluationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        foreach (var (unableToAct, grabbed, health, entity) 
                 in SystemAPI.Query<
                     EnabledRefRW<UnableToAct>, 
                     EnabledRefRO<Grabbed>,
                     RefRO<Health>>()
                     .WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            var isUnableToAct = grabbed.ValueRO || 
                                health.ValueRO.IsDead;
            
            em.SetComponentEnabled<UnableToAct>(entity, isUnableToAct);
        }
    }
}