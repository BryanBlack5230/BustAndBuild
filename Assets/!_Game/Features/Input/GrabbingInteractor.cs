using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Feature.Input
{
    public class GrabbingInteractor: IGameUpdateListener
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private Entity _grabbedEntity;
        private EntityManager _entityManager;

        public GrabbingInteractor(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
        
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Debug.Log("Grabbing Interactor created");
        }

        public void Grab(Entity grabbedEntity) => _grabbedEntity = grabbedEntity;
    
        public void Release() => _grabbedEntity = Entity.Null;

        public void OnUpdate(float deltaTime)
        {
            if (_grabbedEntity == Entity.Null) return;
        
            var localTransform = _entityManager.GetComponentData<LocalTransform>(_grabbedEntity);
            var mousePosition = _mousePositionProvider.worldMousePosition(localTransform.Position);
            localTransform.Position = mousePosition;
            _entityManager.SetComponentData(_grabbedEntity, localTransform);
        }
    }
}