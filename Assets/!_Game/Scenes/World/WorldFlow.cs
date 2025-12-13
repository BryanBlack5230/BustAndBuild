using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Feature.Input;
using GameEngine.Utils;
using GameEngine.Utils.Logging;
using GameManagement;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldFlow : MonoBehaviour
{
    private GameLoopManager _gameLoopManager;
    private LoadingService _loadingService;
    private ScrollController _scrollController;
    private IEnumerable<IGameListener> _listeners;
    private Container _worldSceneContainer;

    [Inject]
    private void Construct(Container container, GameLoopManager gameLoopManager, LoadingService loadingService, ScrollController scrollController, IEnumerable<IGameListener> listeners)
    {
        SceneScope.OnSceneContainerBuilding += OverrideParent;
        _worldSceneContainer = container;

        _gameLoopManager = gameLoopManager;
        _loadingService = loadingService;
        _scrollController = scrollController;
        _listeners = listeners;
    }

    private async void Start()
    {
        Log.World.D("WorldFlow.Start()");
        _scrollController.Initialize();

        if (_listeners != null && _listeners.Any())
        {
            Log.World.D($"WorldFlow registering {_listeners.Count()} listeners");
            _gameLoopManager.AddListeners(_listeners);
        }

        SceneManager.LoadSceneAsync(RuntimeConstants.Scenes.Battle, LoadSceneMode.Additive)
            .completed += OnNextSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_listeners != null && _listeners.Any())
        {
            Log.World.D($"WorldFlow removing {_listeners.Count()} listeners");
            _gameLoopManager.RemoveListeners(_listeners);
        }
        
        Log.World.D("WorldFlow.Destroyed()");
    }
    
    private void OnNextSceneLoaded(AsyncOperation _)
    {
        SceneScope.OnSceneContainerBuilding -= OverrideParent;
    }
    
    private void OverrideParent(Scene scene, ContainerBuilder builder)
    {
        builder.SetParent(_worldSceneContainer);
    }
}