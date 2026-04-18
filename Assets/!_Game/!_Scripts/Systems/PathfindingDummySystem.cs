using Game.Configs;
using GameManagement;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Temporary system to imitate a working pathfinder
/// </summary>
[UpdateInGroup(typeof(GameLoopSystemGroup))]
[UpdateBefore(typeof(Steer_SeekSystem))]
public partial struct PathfindingDummySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FakePathfinderPoint>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var fakePoint = SystemAPI.GetSingleton<FakePathfinderPoint>();

        var filter = new CollisionFilter {
            BelongsTo = ~0u,
            CollidesWith = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Obstacle),
            GroupIndex = 0
        };

        foreach (var (finalDestination, pathTarget, worldTransform) in SystemAPI.Query<RefRO<FinalDestination>, RefRW<PathTarget>, RefRO<LocalToWorld>>())
        {
            var rayInput = new RaycastInput {
                Start = worldTransform.ValueRO.Position,
                End = finalDestination.ValueRO.Value,
                Filter = filter
            };

            if (physicsWorld.CastRay(rayInput, out _)) 
            {
                pathTarget.ValueRW.Value = fakePoint.GatePosition;
            }
            else 
            {
                pathTarget.ValueRW.Value = finalDestination.ValueRO.Value;
            }
        }
    }
}