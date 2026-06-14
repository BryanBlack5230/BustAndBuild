using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Currency;

using Random = Unity.Mathematics.Random;

public static class PickupSpawnUtility
{
    private const float GroundProbeUp = 3f;
    private const float GroundProbeDown = 10f;

    public static void Spawn(
        ref EntityCommandBuffer.ParallelWriter ecb,
        int sortKey,
        Entity prefab,
        float prefabScale,
        in PickupSettings settings,
        in CollisionWorld collisionWorld,
        uint groundLayerBit,
        CurrencyType type,
        float3 source,
        int count,
        float value,
        ref Random rand)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = rand.NextFloat(0f, math.PI2);
            var radius = math.sqrt(rand.NextFloat()) * settings.Scatter;
            var x = source.x + math.cos(angle) * radius;
            var z = source.z + math.sin(angle) * radius;
            var pos = new float3(x, FindGroundY(in collisionWorld, groundLayerBit, x, source.y, z) + settings.SpawnHeight, z);

            var pickup = ecb.Instantiate(sortKey, prefab);
            ecb.SetComponent(sortKey, pickup, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, prefabScale));
            ecb.SetComponent(sortKey, pickup, new Pickup { Type = type, Value = value });
            ecb.SetComponent(sortKey, pickup, new PickupLifetime
            {
                TimeRemaining = settings.Lifetime,
                NextBlinkToggleAt = 0f,
                VisibleState = 1,
                BaseScale = prefabScale,
            });
            ecb.SetComponent(sortKey, pickup, new PickupFloat
            {
                Amplitude = settings.FloatAmplitude,
                Period = settings.FloatPeriod,
                PhaseOffset = rand.NextFloat(0f, math.PI2),
                RestY = pos.y,
                RestTimer = 0f,
            });
            ecb.SetComponent(sortKey, pickup, new PhysicsGravityFactor { Value = 1f });
            ecb.SetComponentEnabled<PickupSettled>(sortKey, pickup, false);
        }
    }

    // A killing blow can land while the source is still penetrating the ground (thrown units),
    // and on uneven terrain a scattered point's ground may sit above the source's Y — either way
    // a fixed source.y offset can start the pickup inside/below the ground collider, where gravity
    // takes it out of the world. Snap each pickup to the actual ground surface at its own XZ.
    private static float FindGroundY(in CollisionWorld collisionWorld, uint groundLayerBit, float x, float sourceY, float z)
    {
        var input = new RaycastInput
        {
            Start = new float3(x, sourceY + GroundProbeUp, z),
            End = new float3(x, sourceY - GroundProbeDown, z),
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = groundLayerBit,
                GroupIndex = 0,
            },
        };

        return collisionWorld.CastRay(input, out var hit) ? hit.Position.y : sourceY;
    }
}
