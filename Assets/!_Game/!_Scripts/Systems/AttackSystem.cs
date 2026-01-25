using GameManagement;
using Unity.Burst;
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

        var attackJob = new AttackJob
        {
            CooldownLookup = SystemAPI.GetComponentLookup<AttackCooldownExpirationTimestamp>(),
            DamageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(),
            ElapsedTime = elapsedTime
        };

        state.Dependency = attackJob.Schedule(state.Dependency);
    }
}

public partial struct AttackJob : IJobEntity
{
    public ComponentLookup<AttackCooldownExpirationTimestamp> CooldownLookup;
    public BufferLookup<DamageBufferElement> DamageBufferLookup;
    
    public double ElapsedTime;

    private void Execute(Entity entity, in AttackData attackData, in Target target, in BattleBrain battleBrain)
    {
        if (!battleBrain.CanAttack) return;
        if (CooldownLookup.IsComponentEnabled(entity)) return;
        if (target.TargetEntity == Entity.Null) return;
        CooldownLookup[entity] = new AttackCooldownExpirationTimestamp { Value = ElapsedTime + attackData.CooldownTime };
        
        CooldownLookup.SetComponentEnabled(entity, true);
        var damageBuffer = DamageBufferLookup[target.TargetEntity];
        damageBuffer.Add(new DamageBufferElement
        {
            Value = attackData.Damage
        });
    }
}