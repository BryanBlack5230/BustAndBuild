#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Feature.Input;
using Game.SceneWorkflow;
using GameEngine.Utils.Logging;
using GameManagement;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

public sealed class WorldFlow : MonoBehaviour, ISceneFlow
{
    private readonly UniTaskCompletionSource _initCompleted = new();

    private GameLoopManager _gameLoopManager = null!;
    private ScrollController _scrollController = null!;
    private IEnumerable<IGameListener>? _listeners;
    private Container _worldSceneContainer = null!;

    UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

    [Inject]
    private void Construct(Container container, GameLoopManager gameLoopManager, ScrollController scrollController, IEnumerable<IGameListener> listeners)
    {
        SceneScope.OnSceneContainerBuilding += OverrideParent;
        _worldSceneContainer = container;
        _gameLoopManager = gameLoopManager;
        _scrollController = scrollController;
        _listeners = listeners;
    }

    private void Start()
    {
        Log.World.D("WorldFlow.Start()");
        _scrollController.Initialize();

        if (_listeners != null)
            _gameLoopManager.AddListeners(_listeners);

        _initCompleted.TrySetResult();
    }

    private void OnDestroy()
    {
        if (_listeners != null)
            _gameLoopManager.RemoveListeners(_listeners);

        Log.World.D("WorldFlow.Destroyed()");
    }

    private void OverrideParent(UnityEngine.SceneManagement.Scene scene, ContainerBuilder builder)
    {
        SceneScope.OnSceneContainerBuilding -= OverrideParent;
        builder.SetParent(_worldSceneContainer);
    }
}
