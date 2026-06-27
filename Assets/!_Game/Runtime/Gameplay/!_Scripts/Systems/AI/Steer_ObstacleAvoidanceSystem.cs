using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BarkingBird.Runtime.Gameplay.AI
{
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

    // Broadphase obstacle scan: one point-distance query per unit returns every Obstacle-layer surface within
    // (SurroundRadius + VisionDistance), with the exact surface distance. The collector folds each hit into the
    // 8 ContextMap directions (directional smear) — replaces the old 8 SphereCasts, smoother and no 8-way aliasing.
    [BurstCompile]
    [WithAll(typeof(SteeringContext))]
    public partial struct ObstacleOverlapJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<WallSection> WallLookup;
        [ReadOnly] public ComponentLookup<WallReference> WallReferenceLookup;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public float DeltaTime;

        private void Execute(
            Entity entity,
            ref ObstacleShadow shadow,
            in SteerBehavior_Obstacle config,
            in LocalTransform transform,
            in Target target)
        {
            shadow.Timer -= DeltaTime;
            if (shadow.Timer > 0) return;
            shadow.Timer = config.UpdateInterval;
            shadow.MyLastPos = transform.Position;

            // Front dirs scan SurroundRadius + VisionDistance, others just SurroundRadius — query the larger,
            // reject per-hit by the actual range in the collector (preserves the front-vision asymmetry).
            var maxRange = config.SurroundRadius + config.VisionDistance;

            var collector = new ObstacleDangerCollector(maxRange)
            {
                Self = entity,
                MyPos = transform.Position,
                MyForward = transform.Forward(),
                SurroundRadius = config.SurroundRadius,
                VisionDistance = config.VisionDistance,
                DangerWeight = config.DangerWeight,
                DangerCurve = config.Curve,
                // Skip walls when this unit is deliberately targeting walls (don't steer away from the thing
                // you're trying to reach). WallSection = root sections, WallReference = detail child colliders.
                SkipWalls = target.TargetEntity != Entity.Null && target.Type == TargetType.Wall,
                WallLookup = WallLookup,
                WallReferenceLookup = WallReferenceLookup,
            };

            PhysicsWorld.CollisionWorld.CalculateDistance(
                new PointDistanceInput
                {
                    Position = transform.Position,
                    MaxDistance = maxRange,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = (uint)config.ObstacleLayer.value,
                        GroupIndex = 0
                    }
                },
                ref collector);

            shadow.CachedDanger = collector.Danger;
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

    // Folds every in-range Obstacle surface from a PointDistanceInput query into an 8-bin ContextMap danger field.
    // MaxFraction is fixed at the max scan range (never shrinks) so all in-range hits are delivered; the query's
    // MaxDistance does the culling. hit.Distance is the absolute surface distance (Physics 1.4.2: for distance
    // queries DistanceHit.Distance == the metric distance, not a normalized raycast fraction).
    struct ObstacleDangerCollector : ICollector<DistanceHit>
    {
        public bool  EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int   NumHits { get; private set; }

        public Entity Self;
        public float3 MyPos, MyForward;
        public float  SurroundRadius, VisionDistance, DangerWeight;
        public Curve  DangerCurve;
        public bool   SkipWalls;

        public ComponentLookup<WallSection>   WallLookup;
        public ComponentLookup<WallReference> WallReferenceLookup;

        public ContextMap Danger; // copied into ObstacleShadow.CachedDanger after the query

        public ObstacleDangerCollector(float maxDistance) : this()
        {
            MaxFraction = maxDistance;
        }

        public bool AddHit(DistanceHit hit)
        {
            if (hit.Entity == Self) return false; // own collider returns at dist 0 (defensive — Unit layer ≠ Obstacle)

            if (SkipWalls && (WallLookup.HasComponent(hit.Entity) || WallReferenceLookup.HasComponent(hit.Entity)))
                return false;

            var dir = math.normalizesafe(hit.Position - MyPos);
            var isFront = math.dot(dir, MyForward) > 0.5f;
            var range = isFront ? SurroundRadius + VisionDistance : SurroundRadius;
            if (hit.Distance > range) return false;

            // Two-tier proximity: physical (within SurroundRadius) maxed against the weaker front-only vision zone.
            var physicalProximity = math.saturate(1f - hit.Distance / SurroundRadius);
            var visionProximity = isFront ? math.saturate(1f - hit.Distance / range) * 0.3f : 0f;
            var proximity = math.max(physicalProximity, visionProximity);

            var proximityWeight = DangerCurve switch
            {
                Curve.Linear => proximity,
                Curve.Quadratic => proximity * proximity,
                Curve.Cubic => proximity * proximity * proximity,
                Curve.Quadruple => proximity * proximity * proximity * proximity,
                Curve.Quintuple => proximity * proximity * proximity * proximity * proximity,
                _ => proximity
            };

            var danger = proximityWeight * DangerWeight;

            // Smear danger across all 8 directions by alignment with the hit; max ≈ "nearest blocker per bin".
            for (var k = 0; k < 8; k++)
            {
                var w = math.max(0f, math.dot(dir, SteeringConstants.GetDirection(k)));
                Danger[k] = math.max(Danger[k], danger * w);
            }

            NumHits++;
            return true;
        }
    }
}
