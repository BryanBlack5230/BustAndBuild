using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct InAirCollisionSystem : ISystem
{
    private ComponentLookup<InAir> _inAirLookup;
    private ComponentLookup<PhysicsVelocity> _velocityLookup;
    private ComponentLookup<PhysicsCollider> _colliderLookup;
    private ComponentLookup<BounceDamage> _bounceDamageLookup;
    private ComponentLookup<Unit> _unitLookup;
    private ComponentLookup<EnemyUnitType> _enemyTypeLookup;
    private ComponentLookup<AllyUnitType> _allyTypeLookup;
    private BufferLookup<DamageBufferElement> _damageLookup;
    private BufferLookup<HitFeedbackBufferElement> _feedbackLookup;
    private uint _groundLayerBit;
    private uint _pickUpsLayerBit;

    // OnCreate is not [BurstCompile] — LayerMask.NameToLayer is a managed call
    public void OnCreate(ref SystemState state)
    {
        _inAirLookup = state.GetComponentLookup<InAir>(true);
        _velocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);
        _colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
        _bounceDamageLookup = state.GetComponentLookup<BounceDamage>(true);
        _unitLookup = state.GetComponentLookup<Unit>(true);
        _enemyTypeLookup = state.GetComponentLookup<EnemyUnitType>(true);
        _allyTypeLookup = state.GetComponentLookup<AllyUnitType>(true);
        _damageLookup = state.GetBufferLookup<DamageBufferElement>();
        _feedbackLookup = state.GetBufferLookup<HitFeedbackBufferElement>();

        _groundLayerBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground);
        _pickUpsLayerBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.PickUps);

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
        _unitLookup.Update(ref state);
        _enemyTypeLookup.Update(ref state);
        _allyTypeLookup.Update(ref state);
        _damageLookup.Update(ref state);
        _feedbackLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        var bounce = SystemAPI.TryGetSingleton<BounceConfig>(out var bounceCfg) ? bounceCfg : BounceConfig.Default;

        // Per-unit-type incoming-damage scale lives in the targeting blob; absent (test scenes) → faction fallback.
        var profilesBlob = SystemAPI.TryGetSingleton<TargetProfiles>(out var profiles)
            ? profiles.Blob
            : default;

        var minVel = bounce.FallbackMinVelocity;
        var maxVel = bounce.FallbackMaxVelocity;
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
            UnitLookup = _unitLookup,
            EnemyTypeLookup = _enemyTypeLookup,
            AllyTypeLookup = _allyTypeLookup,
            ProfilesBlob = profilesBlob,
            DamageLookup = _damageLookup,
            FeedbackLookup = _feedbackLookup,
            GroundLayerBit = _groundLayerBit,
            PickUpsLayerBit = _pickUpsLayerBit,
            MinVelocity = minVel,
            MaxVelocity = maxVel,
            CurveSamples = curveSamples,
            Bounce = bounce,
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
    [ReadOnly] public ComponentLookup<Unit> UnitLookup;
    [ReadOnly] public ComponentLookup<EnemyUnitType> EnemyTypeLookup;
    [ReadOnly] public ComponentLookup<AllyUnitType> AllyTypeLookup;
    [ReadOnly] public BlobAssetReference<TargetProfilesBlob> ProfilesBlob;
    public BufferLookup<DamageBufferElement> DamageLookup;
    public BufferLookup<HitFeedbackBufferElement> FeedbackLookup;
    public EntityCommandBuffer Ecb;
    public uint GroundLayerBit;
    public uint PickUpsLayerBit;
    public float MinVelocity;
    public float MaxVelocity;
    public FixedList512Bytes<float> CurveSamples;
    public BounceConfig Bounce;

    public void Execute(CollisionEvent collisionEvent)
    {
        var entityA = collisionEvent.EntityA;
        var entityB = collisionEvent.EntityB;

        var aHasInAir = InAirLookup.HasComponent(entityA);
        var bHasInAir = InAirLookup.HasComponent(entityB);
        var aInAir = aHasInAir && InAirLookup.IsComponentEnabled(entityA);
        var bInAir = bHasInAir && InAirLookup.IsComponentEnabled(entityB);

        if (!aInAir && !bInAir) return;

        // Pickups never bounce thrown units and never count as landings — let physics resolve naturally.
        if (IsPickUp(entityA) || IsPickUp(entityB)) return;

        // Normal points B→A
        var normalBtoA = collisionEvent.Normal;

        // Both actively flying → bounce off each other
        if (aInAir && bInAir)
        {
            DoBounce(entityA, normalBtoA);
            DoBounce(entityB, -normalBtoA);
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
            DoBounce(thrownEntity, normal);
    }

    private void Landed(Entity entity)
    {
        Ecb.SetComponentEnabled<InAir>(entity, false);

        if (!VelocityLookup.TryGetComponent(entity, out var velocity)) return;
        if (!BounceDamageLookup.TryGetComponent(entity, out var bounceDmg)) return;

        var velocityPower = ComputeVelocityPower(velocity.Linear);
        var finalDamage = bounceDmg.BaseDamage * velocityPower
                          + velocityPower * bounceDmg.BounceCount * bounceDmg.BounceDamageMultiplier;

        finalDamage *= IncomingDamageScale(entity);

        if (DamageLookup.HasBuffer(entity))
            DamageLookup[entity].Add(new DamageBufferElement { Value = finalDamage });

        if (FeedbackLookup.HasBuffer(entity))
            FeedbackLookup[entity].Add(new HitFeedbackBufferElement
            {
                HitDirection = math.normalizesafe(-velocity.Linear, new float3(0f, 1f, 0f)),
            });

        // Log.Battle.D($"{entity} has landed. Velocity: {math.length(velocity.Linear)}, VelocityPower: {velocityPower}, Bounced: {bounceDmg.BounceCount}, Damage: {finalDamage}");
        bounceDmg.BounceCount = 0;
        Ecb.SetComponent(entity, bounceDmg);
    }

    private void DoBounce(Entity entity, float3 normal)
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

        var dmg = Bounce.BounceDamageFactor * bounceDmg.BaseDamage * velocityPower;
        dmg *= IncomingDamageScale(entity);
        if (DamageLookup.HasBuffer(entity))
            DamageLookup[entity].Add(new DamageBufferElement { Value = dmg });

        if (FeedbackLookup.HasBuffer(entity))
            FeedbackLookup[entity].Add(new HitFeedbackBufferElement { HitDirection = normal });

        bounceDmg.BounceCount++;
        // Log.Battle.D($"{entity} has bounced. Velocity: {math.length(velocity.Linear)}, VelocityPower: {velocityPower}, Bounced: {bounceDmg.BounceCount}, Damage: {dmg}");
        Ecb.SetComponent(entity, bounceDmg);
    }

    private void SoftLand(Entity flyingEntity, Entity groundedEntity)
    {
        if (!VelocityLookup.TryGetComponent(flyingEntity, out var flyingVelocity)) return;
        if (!BounceDamageLookup.TryGetComponent(flyingEntity, out var bounceDmg)) return;

        var originalLinear = flyingVelocity.Linear;
        var flyingSpeed    = math.length(originalLinear);
        if (flyingSpeed < Bounce.SoftLandMinSpeed) return;

        var velocityPower = ComputeVelocityPower(originalLinear);
        var finalDamage = bounceDmg.BaseDamage * velocityPower
                          + velocityPower * bounceDmg.BounceCount * bounceDmg.BounceDamageMultiplier;

        var flyingDamage   = finalDamage * Bounce.SoftLandFlyingShare;
        var groundedDamage = finalDamage * Bounce.SoftLandGroundedShare;
        flyingDamage   *= IncomingDamageScale(flyingEntity);
        groundedDamage *= IncomingDamageScale(groundedEntity);

        if (DamageLookup.HasBuffer(flyingEntity))
            DamageLookup[flyingEntity].Add(new DamageBufferElement { Value = flyingDamage });

        if (DamageLookup.HasBuffer(groundedEntity))
            DamageLookup[groundedEntity].Add(new DamageBufferElement { Value = groundedDamage });

        var horizontalImpactDir = math.normalizesafe(new float3(originalLinear.x, 0f, originalLinear.z), float3.zero);

        if (FeedbackLookup.HasBuffer(flyingEntity))
            FeedbackLookup[flyingEntity].Add(new HitFeedbackBufferElement
            {
                HitDirection = math.normalizesafe(-originalLinear, new float3(0f, 1f, 0f)),
            });

        if (FeedbackLookup.HasBuffer(groundedEntity))
            FeedbackLookup[groundedEntity].Add(new HitFeedbackBufferElement { HitDirection = horizontalImpactDir });

        flyingVelocity.Linear = -originalLinear * Bounce.ReboundElasticity;
        Ecb.SetComponent(flyingEntity, flyingVelocity);

        bounceDmg.BounceCount++;
        Ecb.SetComponent(flyingEntity, bounceDmg);

        var knockMagnitude = flyingSpeed * Bounce.KnockMagnitudeFactor;
        var horizontalDir  = math.normalizesafe(new float3(originalLinear.x, 0f, originalLinear.z));

        if (VelocityLookup.TryGetComponent(groundedEntity, out var groundedVelocity))
        {
            groundedVelocity.Linear += horizontalDir * knockMagnitude + new float3(0f, knockMagnitude * Bounce.KnockUpwardFactor, 0f);
            Ecb.SetComponent(groundedEntity, groundedVelocity);
            Ecb.SetComponentEnabled<InAir>(groundedEntity, true);
        }
    }

    // Per-unit-type incoming-damage multiplier (ally 0.25, enemy 1.0 by default), keyed via the targeting blob.
    // When the blob is absent (test scenes without bootstrap) falls back to the old faction-only rule.
    private float IncomingDamageScale(Entity entity)
    {
        if (!UnitLookup.TryGetComponent(entity, out var unit)) return 1f;

        if (!ProfilesBlob.IsCreated)
            return unit.faction == Faction.Ally ? 0.25f : 1f;

        ref var blob = ref ProfilesBlob.Value;
        switch (unit.faction)
        {
            case Faction.Ally:
                if (AllyTypeLookup.TryGetComponent(entity, out var allyType)
                    && (int)allyType.Value < blob.AllyProfiles.Length)
                    return blob.AllyProfiles[(int)allyType.Value].IncomingDamageScale;
                return 0.25f;
            case Faction.Enemy:
                if (EnemyTypeLookup.TryGetComponent(entity, out var enemyType)
                    && (int)enemyType.Value < blob.EnemyProfiles.Length)
                    return blob.EnemyProfiles[(int)enemyType.Value].IncomingDamageScale;
                return 1f;
            default:
                return 1f;
        }
    }

    private bool IsPickUp(Entity entity)
        => ColliderLookup.TryGetComponent(entity, out var collider)
           && (collider.Value.Value.GetCollisionFilter().BelongsTo & PickUpsLayerBit) != 0;

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
