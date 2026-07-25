using Unity.Entities;
using Unity.Mathematics;

using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public sealed class CursorEcsBridge : IGameUpdateListener, IWorldInitializable
    {
        private const float MinRayDirYAbs = 1e-4f;

        private readonly MousePositionProvider _mousePositionProvider;

        private EntityManager _entityManager;
        private Entity _singletonEntity;
        private bool _initialized;

        public CursorEcsBridge(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _singletonEntity = _entityManager.CreateEntity(typeof(CursorWorldPosition));
            _initialized = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_initialized) return;

            var ray = _mousePositionProvider.screenPointToRay;

            _entityManager.SetComponentData(_singletonEntity, new CursorWorldPosition
            {
                RayOrigin = ray.origin,
                RayDirection = ray.direction,
                IsValid = math.abs(ray.direction.y) >= MinRayDirYAbs,
            });
        }
    }
}
