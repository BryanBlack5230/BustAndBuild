using Unity.Burst;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [UpdateInGroup(typeof(GameLoopSystemGroup))]
    [UpdateBefore(typeof(UnitMoverSystem))]
    public partial struct AbleToActEvaluationSystem : ISystem
    {
        private ComponentLookup<Grabbed> _grabbedLookup;
        private ComponentLookup<InAir> _inAirLookup;
        private ComponentLookup<Stun> _stunLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _grabbedLookup = state.GetComponentLookup<Grabbed>(true);
            _inAirLookup   = state.GetComponentLookup<InAir>(true);
            _stunLookup    = state.GetComponentLookup<Stun>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _grabbedLookup.Update(ref state);
            _inAirLookup.Update(ref state);
            _stunLookup.Update(ref state);
            var em = state.EntityManager;

            foreach (var (unableToAct, isDeadRef, entity)
                     in SystemAPI.Query<EnabledRefRW<UnableToAct>, EnabledRefRO<IsDead>>()
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var isGrabbed     = _grabbedLookup.HasComponent(entity) && _grabbedLookup.IsComponentEnabled(entity);
                var isInAir       = _inAirLookup.HasComponent(entity)   && _inAirLookup.IsComponentEnabled(entity);
                var isStunned     = _stunLookup.HasComponent(entity)    && _stunLookup.IsComponentEnabled(entity);
                var isDead        = isDeadRef.ValueRO;
                var isUnableToAct = isGrabbed || isInAir || isStunned || isDead;

                em.SetComponentEnabled<UnableToAct>(entity, isUnableToAct);
            }
        }
    }
}