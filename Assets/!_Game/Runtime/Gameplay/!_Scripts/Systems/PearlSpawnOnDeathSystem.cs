using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.AI;

using Random = Unity.Mathematics.Random;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(ApplyDamageSystem))]
[UpdateBefore(typeof(DeathSystem))]
public partial struct PearlSpawnOnDeathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PearlSpawnPrefab>();
        state.RequireForUpdate<PearlSettings>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var prefab = SystemAPI.GetSingleton<PearlSpawnPrefab>().Prefab;
        if (prefab == Entity.Null) return;

        var settings = SystemAPI.GetSingleton<PearlSettings>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new SpawnPearlsOnDeathJob
        {
            Prefab = prefab,
            Scatter = settings.Scatter,
            SpawnHeight = settings.SpawnHeight,
            Lifetime = settings.Lifetime,
            FloatAmplitude = settings.FloatAmplitude,
            FloatPeriod = settings.FloatPeriod,
            Seed = (uint)math.max(1, (int)(SystemAPI.Time.ElapsedTime * 10007)),
            ECB = ecb,
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(IsDead))]
public partial struct SpawnPearlsOnDeathJob : IJobEntity
{
    public Entity Prefab;
    public float Scatter;
    public float SpawnHeight;
    public float Lifetime;
    public float FloatAmplitude;
    public float FloatPeriod;
    public uint Seed;
    public EntityCommandBuffer.ParallelWriter ECB;

    private void Execute(
        Entity entity,
        [EntityIndexInQuery] int sortKey,
        in LocalToWorld worldTransform,
        in PearlDropOnDeath drop)
    {
        var rand = Random.CreateFromIndex(Seed + (uint)entity.Index * 73856093u);
        var count = rand.NextInt(drop.MinCount, drop.MaxCount + 1);
        var source = worldTransform.Position;

        for (var i = 0; i < count; i++)
        {
            var angle = rand.NextFloat(0f, math.PI2);
            var radius = math.sqrt(rand.NextFloat()) * Scatter;
            var pos = new float3(source.x + math.cos(angle) * radius, source.y + SpawnHeight, source.z + math.sin(angle) * radius);

            var pearl = ECB.Instantiate(sortKey, Prefab);
            ECB.SetComponent(sortKey, pearl, LocalTransform.FromPosition(pos));
            ECB.SetComponent(sortKey, pearl, new Pearl { Value = drop.ValuePerPearl });
            ECB.SetComponent(sortKey, pearl, new PearlLifetime
            {
                TimeRemaining = Lifetime,
                NextBlinkToggleAt = 0f,
                VisibleState = 1,
            });
            ECB.SetComponent(sortKey, pearl, new PearlFloat
            {
                Amplitude = FloatAmplitude,
                Period = FloatPeriod,
                PhaseOffset = rand.NextFloat(0f, math.PI2),
                RestY = pos.y,
                RestTimer = 0f,
            });
            ECB.SetComponent(sortKey, pearl, new PhysicsGravityFactor { Value = 1f });
            ECB.SetComponentEnabled<PearlSettled>(sortKey, pearl, false);
        }

        ECB.RemoveComponent<PearlDropOnDeath>(sortKey, entity);
    }
}
