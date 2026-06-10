using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
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
            Settings = settings,
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
    public PearlSettings Settings;
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

        PearlSpawnUtility.Spawn(ref ECB, sortKey, Prefab, Settings, worldTransform.Position, count, drop.ValuePerPearl, ref rand);

        ECB.RemoveComponent<PearlDropOnDeath>(sortKey, entity);
    }
}
