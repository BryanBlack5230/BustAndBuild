using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class GrabbedEntityMover : IDisposable
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private readonly EntityManager _entityManager;
        private readonly EntityQuery _physicsWorldQuery;

        private Entity _entity;
        private float _entityHalfHeight;
        private float _entityHalfWidth;

        public GrabbedEntityMover(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _physicsWorldQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        }

        public void Dispose()
        {
            _physicsWorldQuery.Dispose();
        }

        public void StartMoving(Entity entity)
        {
            _entity = entity;
            var halfExtents = PhysicsUtility.GetEntityHalfExtentsXY(entity, _entityManager);
            _entityHalfHeight = halfExtents.y;
            _entityHalfWidth  = halfExtents.x;
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

            var physicsWorld = _physicsWorldQuery.GetSingleton<PhysicsWorldSingleton>();
            var localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            var targetPos = _mousePositionProvider.worldMousePosition(localTransform.Position);

            targetPos.y = math.max(targetPos.y, BoundaryConstraints.GetGroundY(localTransform.Position, in physicsWorld) + _entityHalfHeight);
            localTransform.Position = targetPos;

            _entityManager.SetComponentData(_entity, localTransform);
            ScreenDisplacementCheck();

            localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            localTransform.Position.y = math.max(localTransform.Position.y, BoundaryConstraints.GetGroundY(localTransform.Position, in physicsWorld) + _entityHalfHeight);
            _entityManager.SetComponentData(_entity, localTransform);
        }

        private void ScreenDisplacementCheck()
        {
            var camera = CoreHelper.MainCamera;
            if (camera == null) return;

            var localTransform = _entityManager.GetComponentData<LocalTransform>(_entity);
            localTransform.Position = BoundaryConstraints.ClampToViewport(
                localTransform.Position, localTransform.Rotation,
                new float2(_entityHalfWidth, _entityHalfHeight), camera, clampBottom: false);
            _entityManager.SetComponentData(_entity, localTransform);
        }
    }
}
