using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
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
        [ReadOnly] public NativeArray<Entity> GlobalEnemies;
        [ReadOnly] public NativeArray<Entity> GlobalAllies;

        [ReadOnly] public NativeArray<Entity> WallEntities;
        [ReadOnly] public NativeArray<LocalToWorld> WallTransforms;

        [ReadOnly] public Entity BeaconEntity;

        [ReadOnly] public ComponentLookup<Unit> UnitLookup;
        [ReadOnly] public ComponentLookup<EnemyUnitType> EnemyTypeLookup;
        [ReadOnly] public ComponentLookup<AllyUnitType> AllyTypeLookup;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentLookup<Target> TargetLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;

        [ReadOnly] public double ElapsedTime;
        [ReadOnly] public bool IsBattleActive;
        [ReadOnly] public bool ForceUpdate;
        [ReadOnly] public bool CastleIsBreached;

        private void Execute(Entity entity, ref Target target, in LocalTransform transform, EnabledRefRW<TargetSearchCooldownExpirationTimestamp> cooldownEnabled, ref TargetSearchCooldownExpirationTimestamp cooldownTimestamp, in LocalToWorld worldTransform)
        {
            var shouldSearch = ForceUpdate || (IsBattleActive && !cooldownEnabled.ValueRO);
            if (!shouldSearch) return;
            var faction = UnitLookup[entity].faction;
        
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

            var bestScore = float.MinValue;
            TargetCandidate bestCandidate = default;
            var myPos = transform.Position;
            var myWorldPos = worldTransform.Position;
            var myForward = transform.Forward();

            var hostiles = faction == Faction.Ally ? GlobalEnemies : GlobalAllies;
            var friends = faction == Faction.Ally ? GlobalAllies : GlobalEnemies;

            if (settings.WeightEnemy > 0) 
            {
                ProcessUnitList(entity, hostiles, myPos, myForward, ref settings, settings.WeightEnemy, isHostileList: true, ref bestScore, ref bestCandidate);
            }

            if (settings.WeightAlly > 0)
            {
                ProcessUnitList(entity, friends, myPos, myForward, ref settings, settings.WeightAlly, isHostileList: false, ref bestScore, ref bestCandidate);
            }

            if (settings.WeightWall > 0 && WallEntities.Length > 0 && !CastleIsBreached)
            {
                for (var i = 0; i < WallEntities.Length; i++)
                {
                    var wallWorldPos = WallTransforms[i].Position;
                    var distSq = math.distancesq(myWorldPos, wallWorldPos);
                    if (distSq > settings.DetectionRadiusSq) continue;

                    var distanceWeight = (1 - distSq / settings.DetectionRadiusSq) * settings.DistanceWeight;
                    var score = settings.WeightWall + distanceWeight;
                
                    var dirToTarget = math.normalize(wallWorldPos - myWorldPos);
            
                    if (math.dot(myForward, dirToTarget) >= settings.ViewAngleCos)
                    {
                        score += settings.LineOfSightBonus;
                    }
                
                    if (score < bestScore) continue;
                
                    bestScore = score;
                    bestCandidate = new TargetCandidate 
                    { 
                        Entity = WallEntities[i], 
                        Type = TargetType.Wall, 
                        DistanceSq = distSq,
                        Score = score
                    };
                }
            }
        
            if (settings.WeightBeacon > 0 && BeaconEntity != Entity.Null)
            {
                var beaconWorldPos = LocalToWorldLookup[BeaconEntity].Position;
                var distSq = math.distancesq(myWorldPos, beaconWorldPos);
                var distanceWeight = (1 - distSq / settings.DetectionRadiusSq) * settings.DistanceWeight;
                var score = settings.WeightBeacon + distanceWeight;
            
                if (score > bestScore)
                {
                    bestCandidate = new TargetCandidate { Entity = BeaconEntity, Type = TargetType.Beacon, DistanceSq = distSq, Score = score };
                }
            }
        
            if (bestCandidate.Type != TargetType.None)
            {
                target.TargetEntity = bestCandidate.Entity;
                target.Type = bestCandidate.Type;
                target.CurrentScore = bestCandidate.Score;
                target.DistanceToTarget = bestCandidate.DistanceSq;
            }
            else
            {
                target.TargetEntity = Entity.Null;
                target.Type = TargetType.None;
            }
        }

        private void ProcessUnitList(Entity me, NativeArray<Entity> list, float3 myPos, float3 myForward, ref TargetProfileBlob settings, float baseWeight, bool isHostileList, ref float bestScore, ref TargetCandidate bestCandidate)
        {
            for (var i = 0; i < list.Length; i++)
            {
                var other = list[i];
                if (!TransformLookup.HasComponent(other)) continue;

                var otherPos = TransformLookup[other].Position;
                var distSq = math.distancesq(myPos, otherPos);
                if (distSq > settings.DetectionRadiusSq) continue;
            
                var distanceWeight = (1 - distSq / settings.DetectionRadiusSq) * settings.DistanceWeight;
                var score = baseWeight + distanceWeight;

                if (isHostileList && settings.AggroBonus > 0 && TargetLookup.HasComponent(other))
                {
                    if (TargetLookup[other].TargetEntity == me)
                    {
                        score += settings.AggroBonus;
                    }
                }
            
                var dirToTarget = math.normalize(otherPos - myPos);
            
                if (math.dot(myForward, dirToTarget) >= settings.ViewAngleCos)
                {
                    score += settings.LineOfSightBonus;
                }

                if (!(score > bestScore)) continue;
            
                bestScore = score;
                bestCandidate = new TargetCandidate
                {
                    Entity = other,
                    Type = TargetType.Unit,
                    DistanceSq = distSq,
                    Score = score
                };
            }
        }
    
        struct TargetCandidate
        {
            public Entity Entity;
            public TargetType Type;
            public float DistanceSq;
            public float Score;
        }
    }
}