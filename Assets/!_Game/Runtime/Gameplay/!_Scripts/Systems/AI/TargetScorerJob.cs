using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.AI
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(Target))]
    public partial struct TargetScorerJob : IJobEntity
    {
        [ReadOnly] public BlobAssetReference<TargetProfilesBlob> ProfilesBlob;

        // Broadphase discovery: one point-distance query per unit returns every unit/wall surface within
        // DetectionRadius, with the exact surface distance — so a unit pressed against a big wall scores as
        // adjacent instead of center-far. The beacon is NOT in this query (it's on the Default layer); it's
        // scored separately below with no range gate (D6).
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public CollisionFilter TargetFilter;

        [ReadOnly] public Entity BeaconEntity;

        [ReadOnly] public ComponentLookup<Unit> UnitLookup;
        [ReadOnly] public ComponentLookup<WallSection> WallLookup;
        [ReadOnly] public ComponentLookup<EnemyUnitType> EnemyTypeLookup;
        [ReadOnly] public ComponentLookup<AllyUnitType> AllyTypeLookup;

        [ReadOnly] public NativeParallelHashMap<Entity, Entity> TargetSnapshot;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<TargetBounds> BeaconBoundsLookup;

        [ReadOnly] public double ElapsedTime;
        [ReadOnly] public bool IsBattleActive;
        [ReadOnly] public bool ForceUpdate;
        [ReadOnly] public bool CastleIsBreached;
        [ReadOnly] public bool IsDayPhaseActive;

        private void Execute(Entity entity, ref Target target, in LocalTransform transform, EnabledRefRW<TargetSearchCooldownExpirationTimestamp> cooldownEnabled, ref TargetSearchCooldownExpirationTimestamp cooldownTimestamp, in LocalToWorld worldTransform)
        {
            var faction = UnitLookup[entity].faction;

            if (!IsDayPhaseActive && faction == Faction.Enemy)
            {
                target.TargetEntity = Entity.Null;
                target.Type = TargetType.None;
                return;
            }

            var shouldSearch = ForceUpdate || (IsBattleActive && !cooldownEnabled.ValueRO);
            if (!shouldSearch) return;

            ref var globalProfiles = ref ProfilesBlob.Value;
            ref var settings = ref globalProfiles.EnemyProfiles[0];

            switch (faction)
            {
                case Faction.Enemy:
                {
                    if (EnemyTypeLookup.HasComponent(entity))
                    {
                        var typeIndex = (int)EnemyTypeLookup[entity].Value;
                        if (typeIndex < globalProfiles.EnemyProfiles.Length)
                            settings = ref globalProfiles.EnemyProfiles[typeIndex];
                    }

                    break;
                }
                case Faction.Ally:
                {
                    if (AllyTypeLookup.HasComponent(entity))
                    {
                        var typeIndex = (int)AllyTypeLookup[entity].Value;
                        if (typeIndex < globalProfiles.AllyProfiles.Length)
                            settings = ref globalProfiles.AllyProfiles[typeIndex];
                    }

                    break;
                }
            }

            cooldownTimestamp.Value = ElapsedTime + settings.CheckInterval;
            cooldownEnabled.ValueRW = true;

            var myWorldPos = worldTransform.Position;
            var myForward = transform.Forward();

            // Units + walls via the broadphase query; the collector folds each in-range surface into a
            // best-score pick. Surface distance comes straight from the narrowphase (DistanceHit.Distance),
            // so the old center-distance gate/weighting bug dissolves with no AABB approximation here.
            var collector = new TargetScoringCollector(settings.DetectionRadius)
            {
                Self = entity,
                MyFaction = faction,
                MyPos = myWorldPos,
                MyForward = myForward,
                WeightEnemy = settings.WeightEnemy,
                WeightAlly = settings.WeightAlly,
                WeightWall = settings.WeightWall,
                DistanceWeight = settings.DistanceWeight,
                LineOfSightBonus = settings.LineOfSightBonus,
                AggroBonus = settings.AggroBonus,
                ViewAngleCos = settings.ViewAngleCos,
                DetectionRadiusSq = settings.DetectionRadiusSq,
                CastleIsBreached = CastleIsBreached,
                UnitLookup = UnitLookup,
                WallLookup = WallLookup,
                TargetSnapshot = TargetSnapshot,
            };

            PhysicsWorld.CollisionWorld.CalculateDistance(
                new PointDistanceInput { Position = myWorldPos, MaxDistance = settings.DetectionRadius, Filter = TargetFilter },
                ref collector);

            // Beacon: unconditional candidate, NO range gate (D6). Surface distance via its cached world AABB
            // (falls back to center for the one tick before StructureBoundsSystem populates TargetBounds).
            if (settings.WeightBeacon > 0 && BeaconEntity != Entity.Null)
            {
                var beaconPos = BeaconBoundsLookup.HasComponent(BeaconEntity)
                    ? BeaconBoundsLookup[BeaconEntity].World.ClosestPoint(myWorldPos)
                    : LocalToWorldLookup[BeaconEntity].Position;
                var distSq = math.distancesq(myWorldPos, beaconPos);
                var score = settings.WeightBeacon + (1 - distSq / settings.DetectionRadiusSq) * settings.DistanceWeight;

                if (score > collector.BestScore)
                {
                    collector.BestScore = score;
                    collector.BestEntity = BeaconEntity;
                    collector.BestType = TargetType.Beacon;
                    collector.BestDistSq = distSq;
                }
            }

            if (collector.BestType != TargetType.None)
            {
                target.TargetEntity = collector.BestEntity;
                target.Type = collector.BestType;
                target.CurrentScore = collector.BestScore;
                target.DistanceToTarget = collector.BestDistSq;
            }
            else
            {
                target.TargetEntity = Entity.Null;
                target.Type = TargetType.None;
            }
        }
    }

    // Folds every in-range unit/wall surface from a PointDistanceInput query into a single best-score pick.
    // MaxFraction is fixed at DetectionRadius (never shrinks) so all in-range hits are delivered; the query's
    // MaxDistance does the culling. hit.Distance is the absolute surface distance (Physics 1.4.2: for distance
    // queries DistanceHit.Distance == the metric distance, not a normalized raycast fraction).
    struct TargetScoringCollector : ICollector<DistanceHit>
    {
        public bool  EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int   NumHits { get; private set; }

        public Entity  Self;
        public Faction MyFaction;
        public float3  MyPos, MyForward;

        public float WeightEnemy, WeightAlly, WeightWall;
        public float DistanceWeight, LineOfSightBonus, AggroBonus, ViewAngleCos, DetectionRadiusSq;
        public bool  CastleIsBreached;

        public ComponentLookup<Unit>        UnitLookup;
        public ComponentLookup<WallSection> WallLookup;
        public NativeParallelHashMap<Entity, Entity> TargetSnapshot;

        public float      BestScore;
        public Entity     BestEntity;
        public TargetType BestType;
        public float      BestDistSq;

        public TargetScoringCollector(float maxDistance) : this()
        {
            MaxFraction = maxDistance;
            BestScore = float.MinValue;
        }

        public bool AddHit(DistanceHit hit)
        {
            var e = hit.Entity;
            if (e == Self) return false;                       // the query returns our own collider at dist 0

            float baseWeight;
            TargetType type;
            var isHostile = false;
            if (WallLookup.HasComponent(e))
            {
                if (CastleIsBreached || WeightWall <= 0f) return false;
                baseWeight = WeightWall;
                type = TargetType.Wall;
            }
            else if (UnitLookup.HasComponent(e))
            {
                isHostile  = UnitLookup[e].faction != MyFaction;
                baseWeight = isHostile ? WeightEnemy : WeightAlly;
                if (baseWeight <= 0f) return false;
                type = TargetType.Unit;
            }
            else return false;                                 // wall-child/arena/debris colliders — not scored

            var distSq = hit.Distance * hit.Distance;          // surface distance, straight from the query
            var score  = baseWeight + (1f - distSq / DetectionRadiusSq) * DistanceWeight;

            var dir = math.normalizesafe(hit.Position - MyPos);
            if (math.dot(MyForward, dir) >= ViewAngleCos) score += LineOfSightBonus;

            if (isHostile && AggroBonus > 0f &&
                TargetSnapshot.TryGetValue(e, out var theirTarget) && theirTarget == Self) score += AggroBonus;

            NumHits++;
            if (score > BestScore)
            {
                BestScore = score;
                BestEntity = e;
                BestType = type;
                BestDistSq = distSq;
            }
            return true;
        }
    }
}
