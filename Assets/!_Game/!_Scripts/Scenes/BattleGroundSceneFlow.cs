using System.Collections.Generic;
using System.Linq;
using Game.Feature.Camera;
using Game.Feature.Input;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using GameManagement;
using Reflex.Attributes;
using UnityEngine;

public class BattleGroundSceneFlow : MonoBehaviour
{
    private LoadingService _loadingService;
    private InteractController _interactController;
    private PowerHitController _powerHitController;
    private BattleCameraMovement _battleCameraMovement;
    private BattleCameraBorderSyncBridge _battleCameraBorderSyncBridge;
    private IEnumerable<IGameListener> _listeners;
    private GameLoopManager _gameLoopManager;

    [Inject]
    public void Construct(GameLoopManager gameLoopManager, LoadingService loadingService, InteractController interactController, PowerHitController powerHitController, BattleCameraMovement battleCameraMovement, IEnumerable<IGameListener> listeners, BattleCameraBorderSyncBridge battleCameraBorderSyncBridge)
    {
        _gameLoopManager = gameLoopManager;
        _loadingService = loadingService;
        _interactController = interactController;
        _powerHitController = powerHitController;
        _battleCameraMovement = battleCameraMovement;
        _battleCameraBorderSyncBridge = battleCameraBorderSyncBridge;
        _listeners = listeners;
    }

    private async void Start()
    {
        Log.Battle.D("BattlegroundFlow.Start()");
        _interactController.Initialize();
        _powerHitController.Initialize();
        _battleCameraMovement.Initialize();
        _battleCameraBorderSyncBridge.Initialize();

        if (_listeners != null && _listeners.Any())
        {
            Log.Battle.D($"BattlegroundFlow registering {_listeners.Count()} listeners");
            _gameLoopManager.AddListeners(_listeners);
        }
    }

    private void OnDestroy()
    {
        if (_listeners != null && _listeners.Any())
        {
            Log.Battle.D($"BattlegroundFlow removing {_listeners.Count()} listeners");
            _gameLoopManager.RemoveListeners(_listeners);
        }
        
        Log.Battle.D("BattlegroundFlow.Destroyed()");
    }
}