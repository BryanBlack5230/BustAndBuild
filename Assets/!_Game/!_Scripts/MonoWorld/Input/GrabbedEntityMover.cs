using System;
using Game.Configs;
using GameEngine.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Feature.Input
{
    public sealed class GrabbedEntityMover : IDisposable
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private readonly EntityManager _entityManager;
        private readonly EntityQuery _physicsWorldQuery;
        private readonly uint _groundMask;

        private Entity _entity;
        private float _entityHalfHeight;
        private float _entityHalfWidth;

        public GrabbedEntityMover(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
            _groundMask = (uint)(1 << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground));
        }

        public void Dispose()
        {
            _physicsWorldQuery.Dispose();
        }

        public void StartMoving(Entity entity)
        {
            _entity = entity;
            GetEntityDimensions(entity);
        }

        public void StopMoving()
        {
            _entity = Entity.Null;
            _entityHalfHeight = 0f;
            _entityHalfWidth = 0f;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_entity == Entity.Null) return;
            if (!_entityManager.Exists(_entity))
            {
                StopMoving();
                return;
            }

            var localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            var targetPos = _mousePositionProvider.worldMousePosition(localTransform.Position);

            targetPos.y = math.max(targetPos.y, GetGroundY(localTransform.Position) + _entityHalfHeight);
            localTransform.Position = targetPos;

            _entityManager.SetComponentData(_entity, localTransform);
            ScreenDisplacementCheck();

            localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            localTransform.Position.y = math.max(localTransform.Position.y, GetGroundY(localTransform.Position) + _entityHalfHeight);
            _entityManager.SetComponentData(_entity, localTransform);
        }

        private void GetEntityDimensions(Entity entity)
        {
            if (!_entityManager.HasComponent<PhysicsCollider>(entity))
            {
                _entityHalfHeight = 0.5f;
                _entityHalfWidth = 0.5f;
                return;
            }
            
            var localTransform = _entityManager.GetComponentData<LocalTransform>(entity);
            var collider = _entityManager.GetComponentData<PhysicsCollider>(entity);
            var aabb = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, float3.zero));
            _entityHalfHeight = (aabb.Max.y - aabb.Min.y) * 0.5f;
            _entityHalfWidth = (aabb.Max.x - aabb.Min.x) * 0.5f;
        }

        private float GetGroundY(float3 position)
        {
            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var rayInput = new RaycastInput
            {
                Start = new float3(position.x, 100, position.z),
                End   = new float3(position.x, -100, position.z),
                Filter = new CollisionFilter
                {
                    BelongsTo    = ~0u,
                    CollidesWith = _groundMask,
                    GroupIndex   = 0
                }
            };
            return physicsWorld.CastRay(rayInput, out var hit) ? hit.Position.y : 0f;
        }

        private void ScreenDisplacementCheck()
        {
            var camera = CoreHelper.MainCamera;
            if (camera == null) return;
            
            var localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            var pos = localTransform.Position;
            var rot = localTransform.Rotation;

            float minVpX = float.MaxValue, maxVpX = float.MinValue, maxVpY = float.MinValue;
            UpdateViewportBounds(camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(-_entityHalfWidth, -_entityHalfHeight, 0f))), ref minVpX, ref maxVpX, ref maxVpY);
            UpdateViewportBounds(camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(+_entityHalfWidth, -_entityHalfHeight, 0f))), ref minVpX, ref maxVpX, ref maxVpY);
            UpdateViewportBounds(camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(-_entityHalfWidth, +_entityHalfHeight, 0f))), ref minVpX, ref maxVpX, ref maxVpY);
            UpdateViewportBounds(camera.WorldToViewportPoint(pos + math.rotate(rot, new float3(+_entityHalfWidth, +_entityHalfHeight, 0f))), ref minVpX, ref maxVpX, ref maxVpY);

            const float margin = 0.01f;
            float shiftX = 0f, shiftY = 0f;

            if (minVpX < margin)           shiftX = margin - minVpX;
            else if (maxVpX > 1f - margin) shiftX = (1f - margin) - maxVpX;
            if (maxVpY > 1f - margin)      shiftY = (1f - margin) - maxVpY;

            if (shiftX == 0f && shiftY == 0f) return;

            var centerVp = camera.WorldToViewportPoint(pos);
            var newWorld = camera.ViewportToWorldPoint(new Vector3(centerVp.x + shiftX, centerVp.y + shiftY, centerVp.z));
            localTransform.Position = new float3(newWorld.x, newWorld.y, localTransform.Position.z);
            _entityManager.SetComponentData(_entity, localTransform);
        }

        private static void UpdateViewportBounds(Vector3 vp, ref float minX, ref float maxX, ref float maxY)
        {
            if (vp.x < minX) minX = vp.x;
            if (vp.x > maxX) maxX = vp.x;
            if (vp.y > maxY) maxY = vp.y;
        }
    }
}
