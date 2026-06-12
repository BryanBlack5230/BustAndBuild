using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.AI;

using Random = Unity.Mathematics.Random;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(ApplyDamageSystem))]
[UpdateBefore(typeof(DeathSystem))]
public partial struct PickupSpawnOnDeathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // PickupSettings + the PickupPrefabRef buffer live on the same baked spawner entity.
        state.RequireForUpdate<PickupSettings>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<PickupSettings>();

        // Copy the (baking-remapped) prefab refs into a value-type map for Burst job capture.
        var prefabBuffer = SystemAPI.GetSingletonBuffer<PickupPrefabRef>(true);
        var map = default(PickupPrefabMap);
        for (var i = 0; i < prefabBuffer.Length; i++) map.Entries.Add(prefabBuffer[i]);
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new SpawnPickupsOnDeathJob
        {
            PrefabMap = map,
            Settings = settings,
            Seed = (uint)math.max(1, (int)(SystemAPI.Time.ElapsedTime * 10007)),
            ECB = ecb,
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(IsDead))]
public partial struct SpawnPickupsOnDeathJob : IJobEntity
{
    public PickupPrefabMap PrefabMap;
    public PickupSettings Settings;
    public uint Seed;
    public EntityCommandBuffer.ParallelWriter ECB;

    private void Execute(
        Entity entity,
        [EntityIndexInQuery] int sortKey,
        in LocalToWorld worldTransform,
        [ReadOnly] DynamicBuffer<ResourceDrop> drops)
    {
        var rand = Random.CreateFromIndex(Seed + (uint)entity.Index * 73856093u);

        for (var i = 0; i < drops.Length; i++)
        {
            var drop = drops[i];

            var idx = (int)drop.Type;
            if (idx < 0 || idx >= PrefabMap.Entries.Length) continue;

            var prefabEntry = PrefabMap.Entries[idx];
            if (prefabEntry.Prefab == Entity.Null) continue;

            if (rand.NextFloat() > drop.Chance) continue;

            var count = rand.NextInt(drop.MinCount, drop.MaxCount + 1);
            if (count <= 0) continue;

            PickupSpawnUtility.Spawn(ref ECB, sortKey, prefabEntry.Prefab, prefabEntry.Scale, Settings, drop.Type, worldTransform.Position, count, drop.Value, ref rand);
        }

        ECB.RemoveComponent<ResourceDrop>(sortKey, entity);
    }
}
