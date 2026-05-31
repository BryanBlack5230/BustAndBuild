using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Settings;

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct InAirCollisionSystem : ISystem
{
    private ComponentLookup<InAir> _inAirLookup;
    private ComponentLookup<PhysicsVelocity> _velocityLookup;
    private ComponentLookup<PhysicsCollider> _colliderLookup;
    private ComponentLookup<BounceDamage> _bounceDamageLookup;
    private BufferLookup<DamageBufferElement> _damageLookup;
    private uint _groundLayerBit;

    // OnCreate is not [BurstCompile] — LayerMask.NameToLayer is a managed call
    public void OnCreate(ref SystemState state)
    {
        _inAirLookup = state.GetComponentLookup<InAir>(true);
        _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);
        _colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
        _bounceDamageLookup = state.GetComponentLookup<BounceDamage>(true);
        _damageLookup = state.GetBufferLookup<DamageBufferElement>();

        _groundLayerBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground);

        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<InAir>(),
            ComponentType.ReadOnly<PhysicsVelocity>()
        ));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _inAirLookup.Update(ref state);
        _velocityLookup.Update(ref state);
        _colliderLookup.Update(ref state);
        _bounceDamageLookup.Update(ref state);
        _damageLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        var minVel = 5f;
        var maxVel = 10f;
        var curveSamples = new FixedList512Bytes<float>();
        if (SystemAPI.TryGetSingleton<ThrowVelocitySettings>(out var velSettings))
        {
            minVel = velSettings.MinVelocity;
            maxVel = velSettings.MaxVelocity;
            curveSamples = velSettings.CurveSamples;
        }

        state.Dependency = new InAirCollisionJob
        {
            InAirLookup = _inAirLookup,
            VelocityLookup = _velocityLookup,
            ColliderLookup = _colliderLookup,
            BounceDamageLookup = _bounceDamageLookup,
            DamageLookup = _damageLookup,
            GroundLayerBit = _groundLayerBit,
            MinVelocity = minVel,
            MaxVelocity = maxVel,
            CurveSamples = curveSamples,
            Ecb = ecb,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
}

[BurstCompile]
public struct InAirCollisionJob : ICollisionEventsJob
{
    [ReadOnly] public ComponentLookup<InAir> InAirLookup;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> VelocityLookup;
    [ReadOnly] public ComponentLookup<PhysicsCollider> ColliderLookup;
    [ReadOnly] public ComponentLookup<BounceDamage> BounceDamageLookup;
    public BufferLookup<DamageBufferElement> DamageLookup;
    public EntityCommandBuffer Ecb;
    public uint GroundLayerBit;
    public float MinVelocity;
    public float MaxVelocity;
    public FixedList512Bytes<float> CurveSamples;

    public void Execute(CollisionEvent collisionEvent)
    {
        var entityA = collisionEvent.EntityA;
        var entityB = collisionEvent.EntityB;

        var aHasInAir = InAirLookup.HasComponent(entityA);
        var bHasInAir = InAirLookup.HasComponent(entityB);
        var aInAir = aHasInAir && InAirLookup.IsComponentEnabled(entityA);
        var bInAir = bHasInAir && InAirLookup.IsComponentEnabled(entityB);

        if (!aInAir && !bInAir) return;

        // Normal points B→A
        var normalBtoA = collisionEvent.Normal;

        // Both actively flying → bounce off each other
        if (aInAir && bInAir)
        {
            Bounce(entityA, normalBtoA);
            Bounce(entityB, -normalBtoA);
            return;
        }

        // One flying, one grounded-but-has-InAir → soft land
        if (aHasInAir && bHasInAir)
        {
            var flyingEntity   = aInAir ? entityA : entityB;
            var groundedEntity = aInAir ? entityB : entityA;
            SoftLand(flyingEntity, groundedEntity);
            return;
        }

        // One flying, other is a non-InAir entity → land or bounce off wall/obstacle
        var thrownEntity = aInAir ? entityA : entityB;
        var otherEntity  = aInAir ? entityB : entityA;
        var normal       = aInAir ? normalBtoA : -normalBtoA;

        var isGround = ColliderLookup.TryGetComponent(otherEntity, out var otherCollider)
                       && (otherCollider.Value.Value.GetCollisionFilter().BelongsTo & GroundLayerBit) != 0;

        if (isGround)
            Landed(thrownEntity);
        else
            Bounce(thrownEntity, normal);
    }

    private void Landed(Entity entity)
    {
        Ecb.SetComponentEnabled<InAir>(entity, false);

        if (!VelocityLookup.TryGetComponent(entity, out var velocity)) return;
        if (!BounceDamageLookup.TryGetComponent(entity, out var bounceDmg)) return;

        var velocityPower = ComputeVelocityPower(velocity.Linear);
        var finalDamage = bounceDmg.BaseDamage * velocityPower
                          + velocityPower * bounceDmg.BounceCount * bounceDmg.BounceDamageMultiplier;

        if (DamageLookup.HasBuffer(entity))
            DamageLookup[entity].Add(new DamageBufferElement { Value = finalDamage });

        // Log.Battle.D($"{entity} has landed. Velocity: {math.length(velocity.Linear)}, VelocityPower: {velocityPower}, Bounced: {bounceDmg.BounceCount}, Damage: {finalDamage}");
        bounceDmg.BounceCount = 0;
        Ecb.SetComponent(entity, bounceDmg);
    }

    private void Bounce(Entity entity, float3 normal)
    {
        if (!VelocityLookup.TryGetComponent(entity, out var velocity)) return;

        // normal points away from the surface toward the entity;
        // if dot >= 0 the entity is already separating → stale/speculative contact, skip
        if (math.dot(velocity.Linear, normal) >= 0f) return;

        var velocityPower = ComputeVelocityPower(velocity.Linear);
        velocity.Linear = math.reflect(velocity.Linear, normal);

        if (!BounceDamageLookup.TryGetComponent(entity, out var bounceDmg)) return;
        velocity.Linear *= bounceDmg.BounceElasticity;
        Ecb.SetComponent(entity, velocity);

        var dmg = 0.5f * bounceDmg.BaseDamage * velocityPower;
        if (DamageLookup.HasBuffer(entity))
            DamageLookup[entity].Add(new DamageBufferElement { Value = dmg });

        bounceDmg.BounceCount++;
        // Log.Battle.D($"{entity} has bounced. Velocity: {math.length(velocity.Linear)}, VelocityPower: {velocityPower}, Bounced: {bounceDmg.BounceCount}, Damage: {dmg}");
        Ecb.SetComponent(entity, bounceDmg);
    }

    private void SoftLand(Entity flyingEntity, Entity groundedEntity)
    {
        Ecb.SetComponentEnabled<InAir>(flyingEntity, false);

        if (!VelocityLookup.TryGetComponent(flyingEntity, out var flyingVelocity)) return;
        if (!BounceDamageLookup.TryGetComponent(flyingEntity, out var bounceDmg)) return;

        var velocityPower = ComputeVelocityPower(flyingVelocity.Linear);
        var finalDamage = bounceDmg.BaseDamage * velocityPower
                          + velocityPower * bounceDmg.BounceCount * bounceDmg.BounceDamageMultiplier;

        if (DamageLookup.HasBuffer(flyingEntity))
            DamageLookup[flyingEntity].Add(new DamageBufferElement { Value = finalDamage * 0.8f });

        if (DamageLookup.HasBuffer(groundedEntity))
            DamageLookup[groundedEntity].Add(new DamageBufferElement { Value = finalDamage * 0.2f });

        bounceDmg.BounceCount = 0;
        Ecb.SetComponent(flyingEntity, bounceDmg);

        var knockMagnitude = math.length(flyingVelocity.Linear) * 0.2f;
        var horizontalDir  = math.normalizesafe(new float3(flyingVelocity.Linear.x, 0f, flyingVelocity.Linear.z));

        if (VelocityLookup.TryGetComponent(groundedEntity, out var groundedVelocity))
        {
            groundedVelocity.Linear += horizontalDir * knockMagnitude;
            Ecb.SetComponent(groundedEntity, groundedVelocity);
        }
    }

    private float ComputeVelocityPower(float3 velocity)
    {
        var t = math.saturate((math.length(velocity) - MinVelocity) / (MaxVelocity - MinVelocity));
        if (CurveSamples.Length < 2) return t;

        var sampleT = t * (CurveSamples.Length - 1);
        var lo = (int)sampleT;
        var hi = math.min(lo + 1, CurveSamples.Length - 1);
        return math.lerp(CurveSamples[lo], CurveSamples[hi], sampleT - lo);
    }
}
