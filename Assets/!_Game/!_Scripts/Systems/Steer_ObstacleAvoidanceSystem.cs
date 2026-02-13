using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(SteeringSystemGroup))]
public partial struct Steer_ObstacleAvoidanceSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<SteeringContext>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
        state.Dependency = new ObstacleOverlapJob
        {
            PhysicsWorld = physicsWorld.PhysicsWorld,
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new ObstacleApplyJob().ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct ObstacleOverlapJob : IJobEntity
{
    [ReadOnly] public PhysicsWorld PhysicsWorld;
    public float DeltaTime;

    private void Execute(
        ref ObstacleShadow shadow,
        in SteerBehavior_Obstacle config,
        in LocalTransform transform,
        in SteeringContext steeringContext)
    {
        shadow.Timer -= DeltaTime;
        if (shadow.Timer > 0) return;
        shadow.Timer = config.UpdateInterval;
        shadow.MyLastPos = transform.Position;

        shadow.CachedDanger = default;

        var filter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = (uint)config.ObstacleLayer.value,
            GroupIndex = 0
        };

        for (int i = 0; i < 8; i++)
        {
            var dir = SteeringConstants.Directions[i];
            var isFrontDirection = math.dot(dir, transform.Forward()) > 0.5f;
            var scanRange = isFrontDirection ? config.SurroundRadius + config.VisionDistance : config.SurroundRadius;
            var dangerWeight = isFrontDirection ? config.DangerWeight * 0.5f : config.DangerWeight;

            if (!PhysicsWorld.SphereCast(transform.Position + dir * steeringContext.AgentRadius, steeringContext.AgentRadius, dir, scanRange, out var hit, filter)) continue;
            
            var proximity = 1.0f - hit.Fraction;
            var danger = proximity * proximity * proximity * dangerWeight; // cubic falloff -the further the danger, the less we care about it 

            shadow.CachedDanger[i] = danger;
        }
    }
}

[BurstCompile]
public partial struct ObstacleApplyJob : IJobEntity
{
    private void Execute(ref SteeringContext context, in ObstacleShadow shadow, in LocalTransform transform, in SteerBehavior_Obstacle config)
    {
        for (int i = 0; i < 8; i++)
        {
            context.Danger[i] += shadow.CachedDanger[i];
        }
        // var movementSinceScan = transform.Position - shadow.MyLastPos;
        // var timeRatio = (config.UpdateInterval - shadow.Timer) / config.UpdateInterval;
        //
        // var sensitivity = 0.5f;
        //
        // for (int i = 0; i < 8; i++)
        // {
        //     var baseDanger = shadow.CachedDanger[i];
        //     if (baseDanger <= 0.001f) continue;
        //
        //     var direction = SteeringConstants.Directions[i];
        //     var alignment = math.dot(movementSinceScan, direction);
        //     var modifier = alignment * sensitivity * timeRatio;
        //
        //     var finalDanger = baseDanger + modifier;
        //     finalDanger = math.clamp(finalDanger, 0f, config.DangerWeight);
        //
        //     context.Danger[i] += finalDanger;
        // }
    }
}