using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.GameLoop;

[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct PearlLifetimeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PearlSettings>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<PearlSettings>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var elapsed = (float)SystemAPI.Time.ElapsedTime;

        new PearlLifetimeJob
        {
            BlinkStart = settings.BlinkStart,
            BlinkInterval = math.max(settings.BlinkInterval, 0.01f),
            Elapsed = elapsed,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ECB = ecb,
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithPresent(typeof(PearlBlinking))]
public partial struct PearlLifetimeJob : IJobEntity
{
    public float BlinkStart;
    public float BlinkInterval;
    public float Elapsed;
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ECB;

    private void Execute(
        Entity entity,
        [EntityIndexInQuery] int sortKey,
        ref PearlLifetime lifetime,
        ref LocalTransform transform,
        EnabledRefRW<PearlBlinking> blinking)
    {
        lifetime.TimeRemaining -= DeltaTime;
        if (lifetime.TimeRemaining <= 0f)
        {
            ECB.DestroyEntity(sortKey, entity);
            return;
        }

        var shouldBlink = lifetime.TimeRemaining <= BlinkStart;
        if (!shouldBlink)
        {
            if (lifetime.VisibleState == 0)
            {
                lifetime.VisibleState = 1;
                transform.Scale = math.max(transform.Scale, 0.25f);
            }
            return;
        }

        if (!blinking.ValueRO) blinking.ValueRW = true;

        if (Elapsed < lifetime.NextBlinkToggleAt) return;

        lifetime.NextBlinkToggleAt = Elapsed + BlinkInterval;
        lifetime.VisibleState = lifetime.VisibleState == 0 ? (byte)1 : (byte)0;
        transform.Scale = lifetime.VisibleState == 1 ? 0.25f : 0.1f;
    }
}
