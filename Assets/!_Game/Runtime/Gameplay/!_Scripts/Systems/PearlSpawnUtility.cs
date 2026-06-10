using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using Random = Unity.Mathematics.Random;

public static class PearlSpawnUtility
{
    public static void Spawn(
        ref EntityCommandBuffer.ParallelWriter ecb,
        int sortKey,
        Entity prefab,
        in PearlSettings settings,
        float3 source,
        int count,
        float valuePerPearl,
        ref Random rand)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = rand.NextFloat(0f, math.PI2);
            var radius = math.sqrt(rand.NextFloat()) * settings.Scatter;
            var pos = new float3(source.x + math.cos(angle) * radius, source.y + settings.SpawnHeight, source.z + math.sin(angle) * radius);

            var pearl = ecb.Instantiate(sortKey, prefab);
            ecb.SetComponent(sortKey, pearl, LocalTransform.FromPosition(pos));
            ecb.SetComponent(sortKey, pearl, new Pearl { Value = valuePerPearl });
            ecb.SetComponent(sortKey, pearl, new PearlLifetime
            {
                TimeRemaining = settings.Lifetime,
                NextBlinkToggleAt = 0f,
                VisibleState = 1,
            });
            ecb.SetComponent(sortKey, pearl, new PearlFloat
            {
                Amplitude = settings.FloatAmplitude,
                Period = settings.FloatPeriod,
                PhaseOffset = rand.NextFloat(0f, math.PI2),
                RestY = pos.y,
                RestTimer = 0f,
            });
            ecb.SetComponent(sortKey, pearl, new PhysicsGravityFactor { Value = 1f });
            ecb.SetComponentEnabled<PearlSettled>(sortKey, pearl, false);
        }
    }
}
