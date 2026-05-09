using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Game.Feature.Input
{
    public sealed class GrabbingInteractor : IGameUpdateListener
    {
        private readonly CursorMovementCalculations _cursorMovementCalculations;
        private readonly ThrowSettingsSetter _throwSettingsSetter;
        private readonly GrabbedEntityMover _grabbedEntityMover;
        private readonly ReleaseCoordinator _releaseCoordinator;

        private readonly EntityManager _entityManager;

        private Entity _grabbedEntity;
        private PhysicsMass _originalMass;

        public GrabbingInteractor(CursorMovementCalculations cursorMovementCalculations, ThrowSettingsSetter throwSettingsSetter, GrabbedEntityMover grabbedEntityMover, ReleaseCoordinator releaseCoordinator)
        {
            _cursorMovementCalculations = cursorMovementCalculations;
            _throwSettingsSetter = throwSettingsSetter;
            _grabbedEntityMover = grabbedEntityMover;
            _releaseCoordinator = releaseCoordinator;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void Grab(Entity grabbedEntity)
        {
            _grabbedEntity = grabbedEntity;
            DisablePhysics(_grabbedEntity);
            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, true);
            _entityManager.SetComponentEnabled<InAir>(_grabbedEntity, false);
            _grabbedEntityMover.StartMoving(_grabbedEntity);
        }

        public void Release()
        {
            if (_grabbedEntity == Entity.Null) return;
            if (!_entityManager.Exists(_grabbedEntity))
            {
                _grabbedEntityMover.StopMoving();
                _grabbedEntity = Entity.Null;
                return;
            }

            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, false);
            _entityManager.SetComponentEnabled<InAir>(_grabbedEntity, true);

            var vel = _cursorMovementCalculations.velocity;
            var rawPower = vel.magnitude;
            var isFastSpeed = rawPower > _throwSettingsSetter.ThrowThreshold;
            var impulse = new float3(vel.normalized.x, vel.normalized.y, 0f) * (rawPower * _throwSettingsSetter.ThrowScale);

            _grabbedEntityMover.StopMoving();
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
