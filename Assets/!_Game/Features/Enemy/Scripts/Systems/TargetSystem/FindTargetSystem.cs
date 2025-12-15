using Game.Configs;
using GameManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;


[BurstCompile]
[UpdateInGroup(typeof(GameLoopSystemGroup))]
public partial struct FindTargetSystem : ISystem
{
    private CollisionFilter _collisionFilter;
    private FindTargetConfigBlob _config;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<FindTargetConfigReference>();
        
        _collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Unit),
            GroupIndex = 0,
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _config = SystemAPI.GetSingleton<FindTargetConfigReference>().ConfigBlob.Value;
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
                     RefRW<Target>>()
                     .WithDisabled<UnableToAct>())
        {
            
            if (!IsTimeToCheck(ref state, findTarget)) continue;
            CheckUnitsInRange(ref state, ref distanceHitList, collisionWorld, localTransform, findTarget, target);
        }
        
        distanceHitList.Dispose();
    }

    private bool IsTimeToCheck(ref SystemState state, RefRW<FindTarget> findTarget)
    {
        findTarget.ValueRW.timer -= SystemAPI.Time.DeltaTime;
        // Debug.Log($"IsTimeToCheck ? current timer: {findTarget.ValueRO.timer}, out of {findTarget.ValueRO.timerMax}");
        if (findTarget.ValueRO.timer > 0f) return false;
        findTarget.ValueRW.timer = _config.defaultCheckInterval;
        return true;
    }

    private void CheckUnitsInRange(ref SystemState state, ref NativeList<DistanceHit> distanceHitList, CollisionWorld collisionWorld, RefRO<LocalTransform> localTransform,
        RefRW<FindTarget> findTarget, RefRW<Target> target)
    {
        distanceHitList.Clear();

        if (!collisionWorld.OverlapSphere(localTransform.ValueRO.Position,
                _config.defaultRange,
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
