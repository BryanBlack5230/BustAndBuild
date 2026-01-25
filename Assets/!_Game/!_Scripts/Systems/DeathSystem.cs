using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(ApplyDamageSystem))] 
public partial struct DeathSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Health>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new MarkDeathJob
        {
            ECB = ecb
        }.ScheduleParallel();

        new DestroyDeadJob
        {
            ECB = ecb
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithNone(typeof(IsDead))]
public partial struct MarkDeathJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in Health health)
    {
        if (health.Value <= 0)
        {
            ECB.SetComponentEnabled<IsDead>(sortKey, entity, true);
        }
    }
}

[BurstCompile]
[WithAll(typeof(IsDead))]
public partial struct DestroyDeadJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey)
    {
        ECB.DestroyEntity(sortKey, entity);
    }
}