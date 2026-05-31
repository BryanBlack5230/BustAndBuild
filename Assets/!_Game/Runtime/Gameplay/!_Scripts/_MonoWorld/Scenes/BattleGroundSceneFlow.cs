#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Reflex.Attributes;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Camera;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.SceneWorkflow;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public sealed class BattleGroundSceneFlow : MonoBehaviour, ISceneFlow
    {
        private readonly UniTaskCompletionSource _initCompleted = new();

        private InteractController _interactController = null!;
        private PowerHitController _powerHitController = null!;
        private BattleCameraMovement _battleCameraMovement = null!;
        private BattleCameraBorderSyncBridge _battleCameraBorderSyncBridge = null!;
        private IEnumerable<IGameListener>? _listeners;
        private GameLoopManager _gameLoopManager = null!;

        UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

        [Inject]
        private void Construct(GameLoopManager gameLoopManager, InteractController interactController, PowerHitController powerHitController, BattleCameraMovement battleCameraMovement, IEnumerable<IGameListener> listeners, BattleCameraBorderSyncBridge battleCameraBorderSyncBridge)
        {
            _gameLoopManager = gameLoopManager;
            _interactController = interactController;
            _powerHitController = powerHitController;
            _battleCameraMovement = battleCameraMovement;
            _battleCameraBorderSyncBridge = battleCameraBorderSyncBridge;
            _listeners = listeners;
        }

        private void Start()
        {
            Log.Battle.D("BattlegroundFlow.Start()");
            _interactController.Initialize();
            _powerHitController.Initialize();
            _battleCameraMovement.Initialize();
            _battleCameraBorderSyncBridge.Initialize();

            if (_listeners != null)
                _gameLoopManager.AddListeners(_listeners);

            _initCompleted.TrySetResult();
        }

        private void OnDestroy()
        {
            if (_listeners != null)
                _gameLoopManager.RemoveListeners(_listeners);

            Log.Battle.D("BattlegroundFlow.Destroyed()");
        }
    }
}