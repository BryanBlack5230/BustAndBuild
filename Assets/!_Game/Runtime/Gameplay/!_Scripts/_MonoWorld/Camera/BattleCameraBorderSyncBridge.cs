using Cinemachine;
using Unity.Entities;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Camera
{
    public class BattleCameraBorderSyncBridge : IGameUpdateListener, IWorldInitializable
    {
        private readonly CinemachineVirtualCamera _camera;
        
        private EntityManager _entityManager;
        private Entity _queryEntity;

        public BattleCameraBorderSyncBridge(BattleSceneData battleSceneData)
        {
            _camera = battleSceneData.sceneCamera;
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _queryEntity = _entityManager.CreateEntity(typeof(CameraFrustumData));
        }

        public void OnUpdate(float deltaTime)
        {
            var brainCamera = CoreHelper.MainCamera;
            var isLive = CinemachineCore.Instance.IsLive(_camera);
            
            _entityManager.SetComponentData(_queryEntity, new CameraFrustumData
            {
                WorldToCameraMatrix = brainCamera.worldToCameraMatrix,
                Fov = _camera.m_Lens.FieldOfView,
                Aspect = brainCamera.aspect,
                IsLive = isLive && _camera.gameObject.activeInHierarchy
            });
        }
    }
}