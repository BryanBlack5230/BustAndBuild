using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
public partial struct ApplyDamageSystem : ISystem
{
    private EntityQuery _damageQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _damageQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Health>()
            .WithAllRW<DamageBufferElement>()
            .WithNone<IsDead>()
            .Build(ref state);

        state.RequireForUpdate(_damageQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new ApplyDamageJob();
        state.Dependency = job.ScheduleParallel(_damageQuery, state.Dependency);
    }
}

[BurstCompile]
public partial struct ApplyDamageJob : IJobEntity
{
    public void Execute(ref Health health, [WithChangeFilter] ref DynamicBuffer<DamageBufferElement> damageBuffer)
    {
        if (damageBuffer.IsEmpty) return;

        var totalDamage = 0f;
        var array = damageBuffer.AsNativeArray();
        
        for (var i = 0; i < array.Length; i++)
        {
            totalDamage += array[i].Value;
        }
            
        health.Value = math.max(health.Value - totalDamage, 0f);
        damageBuffer.Clear();
    }
}