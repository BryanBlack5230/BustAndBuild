using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Infrastructure.GameLoop;

[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateAfter(typeof(BattleBrainSystem))]
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
            ElapsedTime = elapsedTime,
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            FeedbackLookup = SystemAPI.GetBufferLookup<HitFeedbackBufferElement>(true),
        };

        state.Dependency = attackJob.ScheduleParallel(state.Dependency);
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
[WithAll(typeof(AttackData), typeof(Target), typeof(BattleBrain))]
public partial struct AttackJob : IJobEntity
{
    [ReadOnly] public double ElapsedTime;
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly] public BufferLookup<HitFeedbackBufferElement> FeedbackLookup;
    public EntityCommandBuffer.ParallelWriter Ecb;

    private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, in AttackData attackData, in Target target, in BattleBrain battleBrain,
        in LocalTransform attackerTransform,
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

        if (FeedbackLookup.HasBuffer(target.TargetEntity) && TransformLookup.HasComponent(target.TargetEntity))
        {
            var targetPos = TransformLookup[target.TargetEntity].Position;
            var dir = math.normalizesafe(targetPos - attackerTransform.Position, new float3(0f, 0f, 1f));
            Ecb.AppendToBuffer(sortKey, target.TargetEntity, new HitFeedbackBufferElement { HitDirection = dir });
        }
    }
}