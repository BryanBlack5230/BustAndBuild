#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;

using Unity.Entities;

using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.SceneWorkflow;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public sealed class WorldFlow : MonoBehaviour, ISceneFlow
    {
        private readonly UniTaskCompletionSource _initCompleted = new();

        private GameLoopManager _gameLoopManager = null!;
        private ScrollController _scrollController = null!;
        private DaylightEcsBridge _daylightEcsBridge = null!;
        private IEnumerable<IGameListener>? _listeners;
        private Container _worldSceneContainer = null!;

        UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

        [Inject]
        private void Construct(Container container, GameLoopManager gameLoopManager, ScrollController scrollController, DaylightEcsBridge daylightEcsBridge, IEnumerable<IGameListener> listeners)
        {
            SceneScope.OnSceneContainerBuilding += OverrideParent;
            _worldSceneContainer = container;
            _gameLoopManager = gameLoopManager;
            _scrollController = scrollController;
            _daylightEcsBridge = daylightEcsBridge;
            _listeners = listeners;
        }

        private void Start()
        {
            Log.World.D("WorldFlow.Start()");
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _scrollController.Initialize();
            _daylightEcsBridge.Initialize(entityManager);

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
}