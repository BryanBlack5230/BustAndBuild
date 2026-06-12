using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Currency;

using Random = Unity.Mathematics.Random;

public static class PickupSpawnUtility
{
    public static void Spawn(
        ref EntityCommandBuffer.ParallelWriter ecb,
        int sortKey,
        Entity prefab,
        float prefabScale,
        in PickupSettings settings,
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
            var pos = new float3(source.x + math.cos(angle) * radius, source.y + settings.SpawnHeight, source.z + math.sin(angle) * radius);

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
}
