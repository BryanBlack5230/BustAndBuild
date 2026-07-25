#nullable enable

using Cysharp.Threading.Tasks;
using Reflex.Attributes;
using Reflex.Core;
using Unity.Entities;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.SceneWorkflow;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public sealed class BootstrapFlow : MonoBehaviour, ISceneFlow
    {
        private readonly UniTaskCompletionSource _initCompleted = new();

        private LoadingService _loadingService = null!;
        private CursorSetter _cursorSetter = null!;
        private Container _bootSceneContainer = null!;
        private DotsGameLoopBridge _dotsGameLoopBridge = null!;
        private ThrowDebugTracker _throwDebugTracker = null!;
        private BlobContainer _blobContainer = null!;

        UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

        [Inject]
        private void Construct(Container container, LoadingService loadingService, CursorSetter cursorSetter, DotsGameLoopBridge dotsGameLoopBridge, ThrowDebugTracker throwDebugTracker, BlobContainer blobContainer)
        {
            SceneScope.OnSceneContainerBuilding += OverrideParent;
            _bootSceneContainer = container;
            _loadingService = loadingService;
            _cursorSetter = cursorSetter;
            _dotsGameLoopBridge = dotsGameLoopBridge;
            _throwDebugTracker = throwDebugTracker;
            _blobContainer = blobContainer;
        }

        private async void Start()
        {
            Log.Boot.D("BootstrapFlow.Start()");
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _dotsGameLoopBridge.Initialize(entityManager);
            _throwDebugTracker.Initialize(entityManager);

            await _loadingService.BeginLoading(_cursorSetter);

            _blobContainer.Initialize(entityManager);
            _initCompleted.TrySetResult();
        }

        private void OverrideParent(UnityEngine.SceneManagement.Scene scene, ContainerBuilder builder)
        {
            SceneScope.OnSceneContainerBuilding -= OverrideParent;
            builder.SetParent(_bootSceneContainer);
        }
    }
}