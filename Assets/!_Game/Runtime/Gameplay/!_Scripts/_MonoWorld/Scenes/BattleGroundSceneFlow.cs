#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Reflex.Attributes;
using Unity.Entities;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.SceneWorkflow;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public sealed class BattleGroundSceneFlow : MonoBehaviour, ISceneFlow
    {
        private readonly UniTaskCompletionSource _initCompleted = new();

        private IEnumerable<IGameListener>? _listeners;
        private IEnumerable<IWorldInitializable>? _worldInitializables;
        private GameLoopManager _gameLoopManager = null!;

        UniTask ISceneFlow.WaitForInit() => _initCompleted.Task;

        [Inject]
        private void Construct(GameLoopManager gameLoopManager, IEnumerable<IGameListener> listeners, IEnumerable<IWorldInitializable> worldInitializables)
        {
            _gameLoopManager = gameLoopManager;
            _listeners = listeners;
            _worldInitializables = worldInitializables;
        }

        private void Start()
        {
            Log.Battle.D("BattlegroundFlow.Start()");

            if (_worldInitializables != null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                foreach (var initializable in _worldInitializables)
                    initializable.Initialize(entityManager);
            }

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