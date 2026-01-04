using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
// Run AFTER damage is applied so we catch deaths in the same frame
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

        new DeathJob
        {
            ECB = ecb,
            IsDeadHandle = state.GetComponentTypeHandle<IsDead>()
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct DeathJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public ComponentTypeHandle<IsDead> IsDeadHandle;

    public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref Health health, EnabledRefRW<IsDead> isDeadTag)
    {
        if (health.IsDead && !isDeadTag.ValueRO)
        {
            isDeadTag.ValueRW = true;
            return;
        }
        
        if (isDeadTag.ValueRO)
        {
            ECB.DestroyEntity(sortKey, entity);
        }
    }
}