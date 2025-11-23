using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Feature.Input
{
    public class GrabbingInteractor: IGameUpdateListener
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private readonly CursorMovementCalculations _cursorMovementCalculations;
        private Entity _grabbedEntity;
        private EntityManager _entityManager;

        private PhysicsMass _originalMass;
        // private PhysicsGravityFactor _originalGravity;
        private PhysicsVelocity _originalVelocity;
        
        public GrabbingInteractor(MousePositionProvider mousePositionProvider, CursorMovementCalculations cursorMovementCalculations)
        {
            _mousePositionProvider = mousePositionProvider;
            _cursorMovementCalculations = cursorMovementCalculations;
        
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void Grab(Entity grabbedEntity)
        {
            _grabbedEntity = grabbedEntity;
            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, true);
            
            DisablePhysics(_grabbedEntity);
        }
    
        public void Release()
        {
            if (_grabbedEntity == Entity.Null) return;
            
            RestorePhysics(_grabbedEntity);
            _entityManager.SetComponentEnabled<Grabbed>(_grabbedEntity, false);
            //TODO pass _cursorMovementCalculations.force to entity
            _grabbedEntity = Entity.Null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (_grabbedEntity == Entity.Null) return;
        
            var localTransform = _entityManager.GetComponentData<LocalTransform>(_grabbedEntity);
            localTransform.Position = _mousePositionProvider.worldMousePosition(localTransform.Position);
            _entityManager.SetComponentData(_grabbedEntity, localTransform);
        }
        
        private void DisablePhysics(Entity entity)
        {
            _originalVelocity = _entityManager.GetComponentData<PhysicsVelocity>(entity);
            _originalMass     = _entityManager.GetComponentData<PhysicsMass>(entity);
            // _originalGravity  = _entityManager.GetComponentData<PhysicsGravityFactor>(entity);

            var frozenMass = _originalMass;
            frozenMass.InverseMass = 0;
            frozenMass.InverseInertia = float3.zero;

            _entityManager.SetComponentData(entity, new PhysicsVelocity());
            _entityManager.SetComponentData(entity, frozenMass);
            // _entityManager.SetComponentData(entity, new PhysicsGravityFactor { Value = 0 });
        }

        private void RestorePhysics(Entity entity)
        {
            _entityManager.SetComponentData(entity, _originalVelocity);
            _entityManager.SetComponentData(entity, _originalMass);
            // _entityManager.SetComponentData(entity, _originalGravity);
        }
    }
}