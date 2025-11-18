using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;


[BurstCompile]
public partial struct FindTargetSystem : ISystem
{
    private CollisionFilter _collisionFilter;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        _collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameData.UnitLayerMask,
            GroupIndex = 0,
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        LookingForTarget(ref state);
    }

    private void LookingForTarget(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSingleton.CollisionWorld;
        var distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        foreach (var (localTransform, findTarget, target) 
                 in SystemAPI.Query<
                     RefRO<LocalTransform>, 
                     RefRW<FindTarget>,
                     RefRW<Target>>())
        {
            if (!IsTimeToCheck(ref state, findTarget)) continue;
            CheckUnitsInRange(ref state, ref distanceHitList, collisionWorld, localTransform, findTarget, target);
        }
    }

    private bool IsTimeToCheck(ref SystemState state, RefRW<FindTarget> findTarget)
    {
        findTarget.ValueRW.timer -= SystemAPI.Time.DeltaTime;
        // Debug.Log($"IsTimeToCheck ? current timer: {findTarget.ValueRO.timer}, out of {findTarget.ValueRO.timerMax}");
        if (findTarget.ValueRO.timer > 0f) return false;
        findTarget.ValueRW.timer = findTarget.ValueRO.timerMax;
        return true;
    }

    private void CheckUnitsInRange(ref SystemState state, ref NativeList<DistanceHit> distanceHitList, CollisionWorld collisionWorld, RefRO<LocalTransform> localTransform,
        RefRW<FindTarget> findTarget, RefRW<Target> target)
    {
        distanceHitList.Clear();

        if (!collisionWorld.OverlapSphere(localTransform.ValueRO.Position,
                findTarget.ValueRO.range,
                ref distanceHitList,
                _collisionFilter))
        {
            findTarget.ValueRW.noTargetInRange = true;
            target.ValueRW.targetEntity = Entity.Null;
            return;
        }
            
        foreach (var distanceHit in distanceHitList)
        {
            var targetUnit = SystemAPI.GetComponent<Unit>(distanceHit.Entity);
            if (targetUnit.faction != findTarget.ValueRO.targetFaction) continue;
                
            target.ValueRW.targetEntity = distanceHit.Entity;
            findTarget.ValueRW.noTargetInRange = false;
            return;
        }
        
        findTarget.ValueRW.noTargetInRange = true;
        target.ValueRW.targetEntity = Entity.Null;
    }
}
