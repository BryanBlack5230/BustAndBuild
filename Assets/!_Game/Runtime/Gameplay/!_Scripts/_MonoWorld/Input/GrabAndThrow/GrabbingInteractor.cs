using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Input.GrabAndThrow
{
    public sealed class GrabbingInteractor : IGameUpdateListener
    {
        private readonly CursorMovementCalculations _cursorMovementCalculations;
        private readonly ThrowSettingsSetter _throwSettingsSetter;
        private readonly GrabbedEntityMover _grabbedEntityMover;
        private readonly ReleaseCoordinator _releaseCoordinator;
        private readonly ThrowTrajectoryPredictor _trajectoryPredictor;

        private readonly EntityManager _entityManager;

        private Entity _grabbedEntity;
        private PhysicsMass _originalMass;

        public GrabbingInteractor(CursorMovementCalculations cursorMovementCalculations, ThrowSettingsSetter throwSettingsSetter, GrabbedEntityMover grabbedEntityMover, ReleaseCoordinator releaseCoordinator, ThrowTrajectoryPredictor trajectoryPredictor)
        {
            _cursorMovementCalculations = cursorMovementCalculations;
            _throwSettingsSetter = throwSettingsSetter;
            _grabbedEntityMover = grabbedEntityMover;
            _releaseCoordinator = releaseCoordinator;
            _trajectoryPredictor = trajectoryPredictor;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void Grab(Entity grabbedEntity)
        {
            _grabbedEntity = grabbedEntity;
            DisablePhysics(_grabbedEntity);
            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, true);
            _entityManager.SetComponentEnabled<InAir>(_grabbedEntity, false);
            _grabbedEntityMover.StartMoving(_grabbedEntity);
            _trajectoryPredictor.StartTracking(_grabbedEntity, _originalMass);
        }

        public void Release()
        {
            if (_grabbedEntity == Entity.Null) return;
            if (!_entityManager.Exists(_grabbedEntity))
            {
                _grabbedEntityMover.StopMoving();
                _trajectoryPredictor.StopTracking();
                _grabbedEntity = Entity.Null;
                return;
            }

            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, false);
            _entityManager.SetComponentEnabled<InAir>(_grabbedEntity, true);

            var vel = _cursorMovementCalculations.Velocity;
            var rawPower = vel.magnitude;
            var isFastSpeed = rawPower > _throwSettingsSetter.ThrowThreshold;
            var impulse = new float3(vel.normalized.x, vel.normalized.y, 0f) * (rawPower * _throwSettingsSetter.ThrowScale);

            _grabbedEntityMover.StopMoving();
            _trajectoryPredictor.StopTracking();
            _releaseCoordinator.HandleRelease(_grabbedEntity, impulse, isFastSpeed, _originalMass);
            _throwSettingsSetter.OnThrow(_grabbedEntity, rawPower);

            _grabbedEntity = Entity.Null;
        }

        public void OnUpdate(float deltaTime)
        {
            _grabbedEntityMover.OnUpdate(deltaTime);
        }

        private void DisablePhysics(Entity entity)
        {
            _originalMass     = _entityManager.GetComponentData<PhysicsMass>(entity);

            var frozenMass = _originalMass;
            frozenMass.InverseMass = 0;
            frozenMass.InverseInertia = float3.zero;

            _entityManager.SetComponentData(entity, new PhysicsVelocity());
            _entityManager.SetComponentData(entity, frozenMass);
        }
    }
}
