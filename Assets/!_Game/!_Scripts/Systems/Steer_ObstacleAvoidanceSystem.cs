using GameEngine.Utils.Logging;
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
            DeltaTime = SystemAPI.Time.DeltaTime,
            WallLookup = SystemAPI.GetComponentLookup<WallSection>(true),
            WallReferenceLookup = SystemAPI.GetComponentLookup<WallReference>(true),
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new ObstacleApplyJob().ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct ObstacleOverlapJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<WallSection> WallLookup;
    [ReadOnly] public ComponentLookup<WallReference> WallReferenceLookup;
    [ReadOnly] public PhysicsWorld PhysicsWorld;
    public float DeltaTime;

    private void Execute(
        ref ObstacleShadow shadow,
        in SteerBehavior_Obstacle config,
        in LocalTransform transform,
        in SteeringContext steeringContext,
        in Target target)
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

        for (var i = 0; i < 8; i++)
        {
            var dir = SteeringConstants.GetDirection(i);
            var isFrontDirection = math.dot(dir, transform.Forward()) > 0.5f;
            var scanRange = isFrontDirection ? config.SurroundRadius + config.VisionDistance : config.SurroundRadius;

            if (!PhysicsWorld.SphereCast(transform.Position + dir * steeringContext.AgentRadius, steeringContext.AgentRadius, dir, scanRange, out var hit, filter)) continue;

            if (target.TargetEntity != Entity.Null && target.Type == TargetType.Wall)
            {
                // skip obstacle, if unit is targeting walls
                var currentCheck = hit.Entity;
                var isTargetWall = WallLookup.HasComponent(currentCheck) || WallReferenceLookup.HasComponent(currentCheck);
                
                if (isTargetWall) continue;
            }
            
            var absDist = hit.Fraction * scanRange;
            var physicalProximity = math.saturate(1.0f - (absDist / config.SurroundRadius));
            var visionProximity = !isFrontDirection ? 0f : math.saturate(1.0f - (absDist / scanRange)) * 0.3f;

            var proximity = math.max(physicalProximity, visionProximity);

            var proximityWeight = config.Curve switch
            {
                Curve.Linear => proximity,
                Curve.Quadratic => proximity * proximity,
                Curve.Cubic => proximity * proximity * proximity,
                Curve.Quadruple => proximity * proximity * proximity * proximity,
                Curve.Quintuple => proximity * proximity * proximity * proximity * proximity,
                _ => proximity
            };
            
            var danger = proximityWeight * config.DangerWeight;

            shadow.CachedDanger[i] = danger;
        }
    }
}

[BurstCompile]
public partial struct ObstacleApplyJob : IJobEntity
{
    private void Execute(ref SteeringContext context, in ObstacleShadow shadow, in LocalTransform transform, in SteerBehavior_Obstacle config)
    {
        var movementSinceScan = transform.Position - shadow.MyLastPos;
        var distanceNormalization = 1.0f / config.SurroundRadius;
        
        
        for (int i = 0; i < 8; i++)
        {
            var baseDanger = shadow.CachedDanger[i];
            if (baseDanger <= 0.001f) continue;
        
            var direction = SteeringConstants.GetDirection(i);
            var alignment = math.dot(movementSinceScan, direction);
            var modifier = alignment * distanceNormalization;
        
            var finalDanger = baseDanger + modifier;
            finalDanger = math.clamp(finalDanger, 0f, config.DangerWeight);
        
            context.Danger[i] += finalDanger;
        }
    }
}