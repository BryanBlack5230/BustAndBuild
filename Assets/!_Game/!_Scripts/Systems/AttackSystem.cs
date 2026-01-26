using GameManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct AttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var elapsedTime = SystemAPI.Time.ElapsedTime;
        foreach (var (expirationTimestamp, cooldownEnabled) in SystemAPI.Query<RefRO<AttackCooldownExpirationTimestamp>, EnabledRefRW<AttackCooldownExpirationTimestamp>>())
        {
            if (expirationTimestamp.ValueRO.Value > elapsedTime) continue;
            cooldownEnabled.ValueRW = false;
        }
        
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        var attackJob = new AttackJob
        {
            Ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            ElapsedTime = elapsedTime
        };

        state.Dependency = attackJob.ScheduleParallel(state.Dependency);
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
[WithAll(typeof(AttackData), typeof(Target), typeof(BattleBrain))]
public partial struct AttackJob : IJobEntity
{
    [ReadOnly] public double ElapsedTime;
    public EntityCommandBuffer.ParallelWriter Ecb;

    private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, in AttackData attackData, in Target target, in BattleBrain battleBrain,
        EnabledRefRW<AttackCooldownExpirationTimestamp> cooldownEnabled, ref AttackCooldownExpirationTimestamp cooldownTimestamp)
    {
        if (!battleBrain.CanAttack) return;
        
        if (cooldownEnabled.ValueRO) return;
        if (target.TargetEntity == Entity.Null) return;
        
        cooldownTimestamp.Value = ElapsedTime + attackData.CooldownTime;
        cooldownEnabled.ValueRW = true;
        
        Ecb.AppendToBuffer(sortKey, target.TargetEntity, new DamageBufferElement
        {
            Value = attackData.Damage
        });
    }
}