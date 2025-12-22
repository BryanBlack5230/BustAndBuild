using Game.Configs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct FindTargetJob : IJobEntity
{
    [ReadOnly] public BlobAssetReference<FindTargetConfigBlob> ConfigBlob;
    
    // Global Lists
    [ReadOnly] public NativeArray<Entity> AllyEntities;
    [ReadOnly] public NativeArray<Entity> WallEntities;
    [ReadOnly] public NativeArray<LocalTransform> WallTransforms;


    // Lookups
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    [ReadOnly]
    [NativeDisableContainerSafetyRestriction]
    public ComponentLookup<Target> TargetLookup;

    [ReadOnly] public Entity BeaconEntity;
    [ReadOnly] public bool CastleBreached;
    [ReadOnly] public float DeltaTime;

    private void Execute(Entity entity, ref FindTarget findTarget, ref Target target, in LocalTransform transform)
    {
        findTarget.timer -= DeltaTime;
        if (findTarget.timer > 0f) return;
        
        ref var settings = ref ConfigBlob.Value;
        findTarget.timer = settings.defaultCheckInterval;
        
        Debug.Log($"[FINDTARGET][{entity}] Settings blob: [{ConfigBlob}], value: [{ConfigBlob.Value}], settings: [{settings}]");
        Debug.Log($"[FINDTARGET][{entity}] Test settings value base priority: [{settings.beaconBasePriority}], default range: [{settings.defaultRange}], behind angle threshold: [{settings.behindAngleThreshold}]");

        var bestPriority = float.MinValue;
        TargetCandidate bestCandidate = default;
        var foundTarget = false;

        var myPos = transform.Position;
        var myForward = transform.Forward();
        
        var myPosXZ = new float3(myPos.x, 0, myPos.z);
        
        Debug.Log($"[FINDTARGET][{entity}] executed for entity {entity}");
        
        var beaconExists = BeaconEntity != Entity.Null;
        if (beaconExists)
        {
            var beaconPos = TransformLookup[BeaconEntity].Position;
            var distToBeacon = math.distance(myPosXZ, new float3(beaconPos.x, 0, beaconPos.z));
            
            var priority =  settings.beaconBasePriority;
            if (distToBeacon <= settings.closeRangeThreshold) priority += settings.closeRangeBonus;
            
            Debug.Log($"[FINDTARGET][{entity}] found beacon, priority [{priority}], distance [{distToBeacon}], close to it: [{distToBeacon <= settings.closeRangeThreshold}]");
            
            bestCandidate = new TargetCandidate
            {
                entity = BeaconEntity,
                type = TargetType.Beacon,
                priority = priority,
                distance = distToBeacon
            };
            foundTarget = true;
            bestPriority = priority;
        }

        var foundAlly = false;
        Debug.Log($"[FINDTARGET][{entity}] checking allies, there are [{AllyEntities.Length}] allies found");
        foreach (var allyEntity in AllyEntities)
        {
            var allyPos = TransformLookup[allyEntity].Position;
            var allyPosXZ = new float3(allyPos.x, 0, allyPos.z);
            var distToAlly = math.distance(myPosXZ, allyPosXZ);

            Debug.Log($"[FINDTARGET][{entity}] ally [{allyEntity}] at distance [{distToAlly}], close enough : [{distToAlly <= settings.defaultRange}]");
            if (distToAlly > settings.defaultRange) continue;

            var dirToAlly = math.normalize(allyPosXZ - myPosXZ);
            var forwardXZ = math.normalize(new float3(myForward.x, 0, myForward.z));
            var dot = math.dot(forwardXZ, dirToAlly);
            var angle = math.degrees(math.acos(math.clamp(dot, -1f, 1f)));
            var isBehind = angle > settings.behindAngleThreshold;

            var isTargetingMe = false;
            if (TargetLookup.HasComponent(allyEntity))
            {
                var allyTarget = TargetLookup[allyEntity];
                if (allyTarget.targetEntity == entity) isTargetingMe = true;
            }

            Debug.Log($"[FINDTARGET][{entity}] ally [{allyEntity}] at distance [{distToAlly}], is behind : [{isBehind}], is targeting me: [{isTargetingMe}]");
            if (isBehind && !isTargetingMe) continue;
            foundAlly = true;

            var distanceWeight = 1f - (distToAlly / settings.defaultRange);
            var priority = settings.allyBasePriority + (settings.distanceWeight * distanceWeight * 10f);
            if (distToAlly <= settings.closeRangeThreshold) priority += settings.closeRangeBonus;
            if (isTargetingMe) priority += settings.aggressionWeight;

            Debug.Log($"[FINDTARGET][{entity}] priority [{priority}], distance [{distToAlly}]");
            if (!(priority > bestPriority)) continue;

            bestPriority = priority;
            bestCandidate = new TargetCandidate
            {
                entity = allyEntity,
                type = TargetType.Ally,
                priority = priority,
                distance = distToAlly
            };
            foundTarget = true;
        }

        var targetingBeacon = bestCandidate.type == TargetType.Beacon;

        if (targetingBeacon && beaconExists && !CastleBreached)
        {
            var beaconPos = TransformLookup[BeaconEntity].Position;
            var beaconPosXZ = new float3(beaconPos.x, 0, beaconPos.z);
            var distMeBeacon = math.distance(myPosXZ, beaconPosXZ);

            var closestWallEntity = Entity.Null;
            var closestWallDist = float.MaxValue;

            for (var w = 0; w < WallEntities.Length; w++)
            {
                var wallPos = WallTransforms[w].Position;
                var wallPosXZ = new float3(wallPos.x, 0, wallPos.z);
                var distMeWall = math.distance(myPosXZ, wallPosXZ);
                var distWallBeacon = math.distance(wallPosXZ, beaconPosXZ);

                var wallBypassDelta = distMeBeacon - distWallBeacon;
                var isBypassed = wallBypassDelta < -settings.wallBypassCheckRadius;

                if (isBypassed) continue;
                if (distMeWall > settings.wallDetectionRadius) continue;
                
                if (distMeWall < closestWallDist)
                {
                    closestWallDist = distMeWall;
                    closestWallEntity = WallEntities[w];
                }
            }

            if (closestWallEntity != Entity.Null)
            {
                var priority = settings.wallBasePriority;
                if (closestWallDist <= settings.closeRangeThreshold)
                    priority += settings.closeRangeBonus;
                
                bestCandidate = new TargetCandidate
                {
                    entity = closestWallEntity,
                    type = TargetType.Wall,
                    priority = priority,
                    distance = closestWallDist
                };
                foundTarget = true;
            }
        }
        
        if (!foundAlly && CastleBreached && beaconExists)
        {
            var beaconPos = TransformLookup[BeaconEntity].Position;
            var beaconPosXZ = new float3(beaconPos.x, 0, beaconPos.z);
            var distToBeacon = math.distance(myPosXZ, beaconPosXZ);
            
            bestCandidate = new TargetCandidate
            {
                entity = BeaconEntity,
                type = TargetType.Beacon,
                priority = settings.beaconBasePriority + 50f,
                distance = distToBeacon
            };
            foundTarget = true;
        }
        
        if (foundTarget)
        {
            target.targetEntity = bestCandidate.entity;
            target.targetType = bestCandidate.type;
            target.targetPriority = bestCandidate.priority;
            findTarget.noTargetInRange = false;
        }
        else
        {
            target.targetEntity = Entity.Null;
            target.targetType = TargetType.None;
            target.targetPriority = 0;
            findTarget.noTargetInRange = true;
        }
    }
}