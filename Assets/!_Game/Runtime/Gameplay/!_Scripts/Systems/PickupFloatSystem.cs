using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial struct PickupFloatSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PickupSettings>();
        state.RequireForUpdate(state.GetEntityQuery(
            ComponentType.ReadOnly<Pickup>(),
            ComponentType.ReadOnly<PickupFloat>()));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var settings = SystemAPI.GetSingleton<PickupSettings>();
        var elapsed = (float)SystemAPI.Time.ElapsedTime;
        var dt = SystemAPI.Time.DeltaTime;
        var wakeSpeedSq = settings.RestSpeedThreshold * settings.RestSpeedThreshold;

        new PickupFloatJob
        {
            Elapsed = elapsed,
            DeltaTime = dt,
            WakeSpeedSq = wakeSpeedSq,
            RestDuration = settings.RestDuration,
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithPresent(typeof(PickupSettled))]
public partial struct PickupFloatJob : IJobEntity
{
    public float Elapsed;
    public float DeltaTime;
    public float WakeSpeedSq;
    public float RestDuration;

    private void Execute(
        ref LocalTransform transform,
        ref PhysicsVelocity velocity,
        ref PhysicsGravityFactor gravity,
        ref PickupFloat floatData,
        EnabledRefRW<PickupSettled> settled)
    {
        var speedSq = math.lengthsq(velocity.Linear);

        if (settled.ValueRO)
        {
            if (speedSq > WakeSpeedSq)
            {
                settled.ValueRW = false;
                gravity.Value = 1f;
                floatData.RestTimer = 0f;
                return;
            }

            velocity.Linear = float3.zero;
            velocity.Angular = float3.zero;
            gravity.Value = 0f;

            var phase = (Elapsed + floatData.PhaseOffset) * (math.PI2 / floatData.Period);
            transform.Position.y = floatData.RestY + math.sin(phase) * floatData.Amplitude;
            return;
        }

        if (speedSq < WakeSpeedSq)
        {
            floatData.RestTimer += DeltaTime;
            if (floatData.RestTimer >= RestDuration)
            {
                floatData.RestY = transform.Position.y;
                floatData.RestTimer = 0f;
                settled.ValueRW = true;
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                gravity.Value = 0f;
            }
        }
        else
        {
            floatData.RestTimer = 0f;
        }
    }
}
