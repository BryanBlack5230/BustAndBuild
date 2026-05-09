using GameEngine.AI;
using GameManagement;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateBefore(typeof(UnitMoverSystem))]
public partial struct AbleToActEvaluationSystem : ISystem
{
    private ComponentLookup<Grabbed> _grabbedLookup;
    private ComponentLookup<InAir> _inAirLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _grabbedLookup = state.GetComponentLookup<Grabbed>(true);
        _inAirLookup   = state.GetComponentLookup<InAir>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _grabbedLookup.Update(ref state);
        _inAirLookup.Update(ref state);
        var em = state.EntityManager;

        foreach (var (unableToAct, health, entity)
                 in SystemAPI.Query<
                     EnabledRefRW<UnableToAct>,
                     RefRO<Health>>()
                     .WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            var isGrabbed     = _grabbedLookup.HasComponent(entity) && _grabbedLookup.IsComponentEnabled(entity);
            var isInAir       = _inAirLookup.HasComponent(entity)   && _inAirLookup.IsComponentEnabled(entity);
            var isUnableToAct = isGrabbed || isInAir || health.ValueRO.IsDead;

            em.SetComponentEnabled<UnableToAct>(entity, isUnableToAct);
        }
    }
}