using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.AI;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct ScreenBounceSystem : ISystem
{
    private BufferLookup<DamageBufferElement> _damageLookup;
    private ComponentLookup<Unit> _unitLookup;

    // OnCreate is not [BurstCompile] — managed calls for RequireForUpdate setup
    public void OnCreate(ref SystemState state)
    {
        _damageLookup = state.GetBufferLookup<DamageBufferElement>();
        _unitLookup = state.GetComponentLookup<Unit>(true);

        state.RequireForUpdate<CameraFrustumData>();
        state.RequireForUpdate<BattleScreenCenter>();
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<InAir>()));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<CameraFrustumData>(out var camData) || !camData.IsLive) return;
        if (!SystemAPI.TryGetSingleton<BattleScreenCenter>(out var battleCenter)) return;

        _damageLookup.Update(ref state);
        _unitLookup.Update(ref state);

        var minVel = 5f;
        var maxVel = 10f;
        var curveSamples = new FixedList512Bytes<float>();
        if (SystemAPI.TryGetSingleton<ThrowVelocitySettings>(out var velSettings))
        {
            minVel = velSettings.MinVelocity;
            maxVel = velSettings.MaxVelocity;
            curveSamples = velSettings.CurveSamples;
        }

        var worldToCam = camData.WorldToCameraMatrix;
        var camToWorld = math.inverse(worldToCam);
        var camRight   = camToWorld.c0.xyz;
        var camUp      = camToWorld.c1.xyz;
        var tanHalfFov = math.tan(math.radians(camData.Fov * 0.5f));

        const float hw = 0.25f; // here we assume that the shape of unit is a rectangle with 0.5f width and 1f height
        const float hh = 0.5f;

        foreach (var (transform, velocity, bounceDmg, _, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<BounceDamage>, EnabledRefRO<InAir>>()
                          .WithEntityAccess())
        {
            var worldPos = transform.ValueRO.Position;
            var rot = transform.ValueRO.Rotation;

            var camCenter = math.transform(worldToCam, worldPos);
            var depth = -camCenter.z;
            if (depth <= 0f) continue;

            var halfHeight = depth * tanHalfFov;
            var halfWidth  = halfHeight * camData.Aspect + battleCenter.HalfWidthOffset;
            var topLimit   = halfHeight + battleCenter.HalfHeightOffset;

            // Transform all 4 corners of the oriented rectangle to camera space
            var c0 = math.transform(worldToCam, worldPos + math.rotate(rot, new float3(-hw, -hh, 0f)));
            var c1 = math.transform(worldToCam, worldPos + math.rotate(rot, new float3(+hw, -hh, 0f)));
            var c2 = math.transform(worldToCam, worldPos + math.rotate(rot, new float3(-hw, +hh, 0f)));
            var c3 = math.transform(worldToCam, worldPos + math.rotate(rot, new float3(+hw, +hh, 0f)));

            var minCamX = math.min(math.min(c0.x, c1.x), math.min(c2.x, c3.x));
            var maxCamX = math.max(math.max(c0.x, c1.x), math.max(c2.x, c3.x));
            var maxCamY = math.max(math.max(c0.y, c1.y), math.max(c2.y, c3.y));

            var outLeft  = minCamX < -halfWidth;
            var outRight = maxCamX >  halfWidth;
            var outTop   = maxCamY >  topLimit;

            if (!outLeft && !outRight && !outTop) continue;

            var vel = velocity.ValueRO.Linear;
            var velZ = vel.z;
            var snappedCamCenter = camCenter;
            var velocityPower = ComputeVelocityPower(vel, minVel, maxVel, curveSamples);
            var bounced = false;

            if (outLeft)
            {
                if (math.dot(vel, camRight) < 0f)
                {
                    vel = math.reflect(vel, camRight);
                    bounced = true;
                }
                snappedCamCenter.x += -halfWidth - minCamX;
            }
            else if (outRight)
            {
                if (math.dot(vel, -camRight) < 0f)
                {
                    vel = math.reflect(vel, -camRight);
                    bounced = true;
                }
                snappedCamCenter.x += halfWidth - maxCamX;
            }

            if (outTop)
            {
                if (math.dot(vel, -camUp) < 0f)
                {
                    vel = math.reflect(vel, -camUp);
                    bounced = true;
                }
                snappedCamCenter.y += topLimit - maxCamY;
            }

            if (bounced)
            {
                vel *= bounceDmg.ValueRO.BounceElasticity;

                var isAlly = _unitLookup.TryGetComponent(entity, out var unit) && unit.faction == Faction.Ally;
                if (!isAlly)
                {
                    var dmg = 0.5f * bounceDmg.ValueRO.BaseDamage * velocityPower;
                    if (_damageLookup.HasBuffer(entity))
                        _damageLookup[entity].Add(new DamageBufferElement { Value = dmg });

                    bounceDmg.ValueRW.BounceCount++;
                }
            }

            vel.z = velZ; // screen bounce only affects x/y; preserve depth velocity
            velocity.ValueRW.Linear = vel;
            transform.ValueRW.Position = math.transform(camToWorld, snappedCamCenter);
        }
    }

    private static float ComputeVelocityPower(float3 velocity, float minVel, float maxVel, in FixedList512Bytes<float> curveSamples)
    {
        var t = math.saturate((math.length(velocity) - minVel) / (maxVel - minVel));
        if (curveSamples.Length < 2) return t;

        var sampleT = t * (curveSamples.Length - 1);
        var lo = (int)sampleT;
        var hi = math.min(lo + 1, curveSamples.Length - 1);
        return math.lerp(curveSamples[lo], curveSamples[hi], sampleT - lo);
    }
}
