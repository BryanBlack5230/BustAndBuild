#nullable enable

using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class TunnelTeleporter : IDisposable
    {
        private readonly EntityManager _entityManager;
        private readonly EntityQuery _physicsWorldQuery;
        private readonly CollisionFilter _queryFilter;

        public TunnelTeleporter()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
            var groundMask = (uint)(1 << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground));
            _queryFilter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = ~groundMask, GroupIndex = 0 };
        }

        public void Dispose()
        {
            _physicsWorldQuery.Dispose();
        }

        public bool Teleport(Entity entity, float3 throwDir)
        {
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var entityAabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, localTransform.Position));
            var entityHalfExtents = (entityAabb.Max - entityAabb.Min) * 0.5f;

            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var pos = localTransform.Position;
            var maxExit = 0f;

            var bodies = PhysicsOverlapHelper.CollectHitBodies(physicsWorld, entityAabb, _queryFilter, entity);
            try
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    var bodyAabb = bodies[i].Collider.Value.CalculateAabb(bodies[i].WorldFromBody);

                    var tExit = float.MaxValue;
                    for (var axis = 0; axis < 2; axis++)
                    {
                        var d = throwDir[axis];
                        if (math.abs(d) < math.EPSILON) continue;

                        var t = d > 0
                            ? (bodyAabb.Max[axis] - pos[axis]) / d
                            : (bodyAabb.Min[axis] - pos[axis]) / d;
                        if (t > 0 && t < tExit) tExit = t;
                    }

                    if (tExit < float.MaxValue)
                    {
                        var buffer = math.dot(entityHalfExtents, math.abs(throwDir)) + 0.05f;
                        var needed = tExit + buffer;
                        if (needed > maxExit) maxExit = needed;
                    }
                }
            }
            finally
            {
                bodies.Dispose();
            }

            if (maxExit > 0f)
            {
                localTransform.Position += throwDir * maxExit;
                _entityManager.SetComponentData(entity, localTransform);
            }

            return !IsStillOverlapping(entity);
        }

        private bool IsStillOverlapping(Entity entity)
        {
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var entityAabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, localTransform.Position));

            var bodies = PhysicsOverlapHelper.CollectHitBodies(physicsWorld, entityAabb, _queryFilter, entity);
            try
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    var bodyAabb = bodies[i].Collider.Value.CalculateAabb(bodies[i].WorldFromBody);
                    if (PhysicsOverlapHelper.AabbsOverlapXY(entityAabb, bodyAabb)) return true;
                }
                return false;
            }
            finally
            {
                bodies.Dispose();
            }
        }
    }
}
