#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs;
using GameEngine.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Feature.Input
{
    public sealed class OverlapResolver : IDisposable
    {
        private readonly EntityManager _entityManager;
        private readonly EntityQuery _physicsWorldQuery;
        private readonly CollisionFilter _nonGroundFilter;
        private readonly CollisionFilter _groundFilter;

        private const float DisplaceSpeed = 15f;
        private const float DisplaceMaxTime = 0.5f;

        public OverlapResolver()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
            var groundMask = (uint)(1 << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground));
            _groundFilter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = groundMask, GroupIndex = 0 };
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

            var bodies = PhysicsOverlapHelper.CollectHitBodies(physicsWorld, entityAabb, _nonGroundFilter, entity);
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
            var halfExtents = GetEntityHalfExtentsXY(entity, localTransform);

            var groundY = GetGroundY(localTransform.Position);
            localTransform.Position.y = math.max(localTransform.Position.y, groundY + halfExtents.y);

            var camera = CoreHelper.MainCamera;
            if (camera != null)
            {
                var pos = localTransform.Position;
                var rot = localTransform.Rotation;
                var hw = halfExtents.x;
                var hh = halfExtents.y;

                var vp0 = camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(-hw, -hh, 0f)));
                var vp1 = camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(+hw, -hh, 0f)));
                var vp2 = camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(-hw, +hh, 0f)));
                var vp3 = camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(+hw, +hh, 0f)));

                var minVpX = Mathf.Min(Mathf.Min(vp0.x, vp1.x), Mathf.Min(vp2.x, vp3.x));
                var maxVpX = Mathf.Max(Mathf.Max(vp0.x, vp1.x), Mathf.Max(vp2.x, vp3.x));
                var minVpY = Mathf.Min(Mathf.Min(vp0.y, vp1.y), Mathf.Min(vp2.y, vp3.y));
                var maxVpY = Mathf.Max(Mathf.Max(vp0.y, vp1.y), Mathf.Max(vp2.y, vp3.y));

                const float margin = 0.01f;
                float shiftX = 0f, shiftY = 0f;
                if (minVpX < margin)           shiftX = margin - minVpX;
                else if (maxVpX > 1f - margin) shiftX = (1f - margin) - maxVpX;
                if (minVpY < margin)           shiftY = margin - minVpY;
                else if (maxVpY > 1f - margin) shiftY = (1f - margin) - maxVpY;

                if (shiftX != 0f || shiftY != 0f)
                {
                    var centerVp = camera.WorldToViewportPoint(pos);
                    var newWorld = camera.ViewportToWorldPoint(new Vector3(centerVp.x + shiftX, centerVp.y + shiftY, centerVp.z));
                    localTransform.Position = new float3(newWorld.x, newWorld.y, localTransform.Position.z);
                }
            }

            groundY = GetGroundY(localTransform.Position);
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

            var bodies = PhysicsOverlapHelper.CollectHitBodies(physicsWorld, entityAabb, _nonGroundFilter, entity);
            try
            {
                var pushDir = float3.zero;
                var firstIndividualDir = float3.zero;
                var pushCount = 0;

                for (var i = 0; i < bodies.Length; i++)
                {
                    var bodyAabb = bodies[i].Collider.Value.CalculateAabb(bodies[i].WorldFromBody);
                    if (!PhysicsOverlapHelper.AabbsOverlapXY(entityAabb, bodyAabb)) continue;

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

        private float2 GetEntityHalfExtentsXY(Entity entity, LocalTransform localTransform)
        {
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var aabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, float3.zero));
            return new float2((aabb.Max.x - aabb.Min.x) * 0.5f, (aabb.Max.y - aabb.Min.y) * 0.5f);
        }

        private float GetGroundY(float3 position)
        {
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var rayInput = new RaycastInput
            {
                Start  = new float3(position.x, 100, position.z),
                End    = new float3(position.x, -100, position.z),
                Filter = _groundFilter
            };
            return physicsWorld.CastRay(rayInput, out var hit) ? hit.Position.y : 0f;
        }
    }
}
