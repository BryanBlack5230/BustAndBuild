using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Infrastructure.GameLoop;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateAfter(typeof(UnitMoverSystem))]
public partial struct DamagePushSystem : ISystem
{
    private ComponentLookup<InAir> _inAirLookup;
    private ComponentLookup<Stun> _stunLookup;

    public void OnCreate(ref SystemState state)
    {
        _inAirLookup = state.GetComponentLookup<InAir>(true);
        _stunLookup = state.GetComponentLookup<Stun>();

        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<DamagePushConfig>(),
            ComponentType.ReadOnly<DamagePushState>()));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _inAirLookup.Update(ref state);
        _stunLookup.Update(ref state);

        foreach (var (config, pushState, pushEnabled, velocity, entity) in SystemAPI
                     .Query<RefRO<DamagePushConfig>, RefRO<DamagePushState>, EnabledRefRW<DamagePushState>,
                            RefRW<PhysicsVelocity>>()
                     .WithEntityAccess())
        {
            // Single-tick consumer: whatever happens, the dispatch signal is done after this.
            pushEnabled.ValueRW = false;

            // Ground-only push: a unit already airborne is being handled by physics; don't double-dip.
            var inAir = _inAirLookup.HasComponent(entity) && _inAirLookup.IsComponentEnabled(entity);
            if (inAir) continue;

            var dirWorld = pushState.ValueRO.HitDirWorld;
            var horizontal = math.normalizesafe(new float3(dirWorld.x, 0f, dirWorld.z), float3.zero);
            var impulse = horizontal * config.ValueRO.HorizontalImpulse
                          + new float3(0f, config.ValueRO.UpKick, 0f);
            velocity.ValueRW.Linear += impulse;

            if (_stunLookup.HasComponent(entity))
            {
                _stunLookup[entity] = new Stun { Remaining = config.ValueRO.StunDuration };
                _stunLookup.SetComponentEnabled(entity, true);
            }
        }
    }
}
