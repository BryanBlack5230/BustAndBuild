#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class OverlapResolver : IDisposable
    {
        private readonly EntityManager _entityManager;
        private readonly EntityQuery _physicsWorldQuery;
        private readonly CollisionFilter _nonGroundFilter;

        private const float DisplaceSpeed = 15f;
        private const float DisplaceMaxTime = 0.5f;

        public OverlapResolver()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
            var groundMask = (uint)(1 << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground));
            _nonGroundFilter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = ~groundMask, GroupIndex = 0 };
        }

        public void Dispose()
        {
            _physicsWorldQuery.Dispose();
        }

        public bool CheckOverlap(Entity entity)
        {
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var entityAabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, localTransform.Position));

            var bodies = PhysicsUtility.CollectHitBodies(physicsWorld, entityAabb, _nonGroundFilter, entity);
            try
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    var bodyAabb = bodies[i].Collider.Value.CalculateAabb(bodies[i].WorldFromBody);
                    if (PhysicsUtility.AabbsOverlapXY(entityAabb, bodyAabb)) return true;
                }
                return false;
            }
            finally
            {
                bodies.Dispose();
            }
        }

        public async UniTask<bool> ResolveAsync(Entity entity, float3 impulse, PhysicsMass originalMass, Action<Entity, float3, PhysicsMass> onDone, CancellationToken ct)
        {
            var timeout = DisplaceMaxTime;

            while (timeout > 0)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);

                if (!_entityManager.Exists(entity)) return false;

                var deltaTime = Time.fixedDeltaTime;
                timeout -= deltaTime;

                if (DisplaceStep(entity, deltaTime) || timeout <= 0)
                    break;
            }

            if (_entityManager.Exists(entity))
            {
                ClampToViewportAndGround(entity);
                onDone(entity, impulse, originalMass);
            }
            return true;
        }

        public void ClampToViewportAndGround(Entity entity)
        {
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var halfExtents = PhysicsUtility.GetEntityHalfExtentsXY(entity, _entityManager);
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();

            var groundY = BoundaryConstraints.GetGroundY(localTransform.Position, in physicsWorld);
            localTransform.Position.y = math.max(localTransform.Position.y, groundY + halfExtents.y);

            var camera = CoreHelper.MainCamera;
            if (camera != null)
                localTransform.Position = BoundaryConstraints.ClampToViewport(
                    localTransform.Position, localTransform.Rotation, halfExtents, camera, clampBottom: true);

            groundY = BoundaryConstraints.GetGroundY(localTransform.Position, in physicsWorld);
            localTransform.Position.y = math.max(localTransform.Position.y, groundY + halfExtents.y);

            _entityManager.SetComponentData(entity, localTransform);
        }

        private bool DisplaceStep(Entity entity, float deltaTime)
        {
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var entityAabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, localTransform.Position));
            var entityCenter = localTransform.Position;

            var bodies = PhysicsUtility.CollectHitBodies(physicsWorld, entityAabb, _nonGroundFilter, entity);
            try
            {
                var pushDir = float3.zero;
                var firstIndividualDir = float3.zero;
                var pushCount = 0;

                for (var i = 0; i < bodies.Length; i++)
                {
                    var bodyAabb = bodies[i].Collider.Value.CalculateAabb(bodies[i].WorldFromBody);
                    if (!PhysicsUtility.AabbsOverlapXY(entityAabb, bodyAabb)) continue;

                    var bodyCenter = (bodyAabb.Min + bodyAabb.Max) * 0.5f;
                    var awayDir = entityCenter - bodyCenter;
                    awayDir.z = 0f;

                    var individualDir = math.lengthsq(awayDir) > math.EPSILON ? math.normalize(awayDir) : new float3(0f, 1f, 0f);
                    if (pushCount == 0) firstIndividualDir = individualDir;
                    pushDir += individualDir;
                    pushCount++;
                }

                if (pushCount == 0) return true;

                if (math.lengthsq(pushDir) <= math.EPSILON)
                    pushDir = new float3(-firstIndividualDir.y, firstIndividualDir.x, 0f);

                pushDir.z = 0f;
                localTransform.Position += math.normalize(pushDir) * DisplaceSpeed * deltaTime;
                _entityManager.SetComponentData(entity, localTransform);
                return false;
            }
            finally
            {
                bodies.Dispose();
            }
        }

    }
}
