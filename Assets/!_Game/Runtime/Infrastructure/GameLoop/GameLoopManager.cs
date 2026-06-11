using System;
using System.Collections.Generic;
using System.Linq;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    public class GameLoopManager : MonoBehaviour
    {
        [ShowInInspector] private List<IGameListener> _listeners = new();
        [ShowInInspector] private List<IGameUpdateListener> _updateListeners = new();
        [ShowInInspector] private List<IGameFixedUpdateListener> _fixedUpdateListeners = new();
        [ShowInInspector] private List<IGameLateUpdateListener> _lateUpdateListeners = new();

        [ShowInInspector] private GameState _state = GameState.Unknown;

        /// <summary>
        /// Authoritative current lifecycle state. Single source of truth — read it instead of inferring
        /// state from UI or local flags. Changes are announced via <see cref="GameStateChangedEvent"/>.
        /// </summary>
        public GameState State => _state;

        [Inject]
        private void Construct(IEnumerable<IGameListener> gameListeners)
        {
            var collection = gameListeners.ToList();
            _listeners.AddRange(collection);
			
            foreach (var listener in collection)
            {
                RegisterListener(listener);
            }
        }

        /// <summary>
        /// Add listeners at runtime (e.g., when loading additive scenes)
        /// </summary>
        public void AddListeners(IEnumerable<IGameListener> gameListeners)
        {
            var collection = gameListeners.ToList();
            _listeners.AddRange(collection);
			
            foreach (var listener in collection)
            {
                RegisterListener(listener);
            }

            ApplyState(collection);
        }

        /// <summary>
        /// Remove listeners at runtime (e.g., when unloading additive scenes)
        /// </summary>
        public void RemoveListeners(IEnumerable<IGameListener> gameListeners)
        {
            var collection = gameListeners.ToList();
            
            foreach (var listener in collection)
            {
                _listeners.Remove(listener);
        
                switch (listener)
                {
                    case IGameUpdateListener updateListener:
                        _updateListeners.Remove(updateListener);
                        break;
                    case IGameFixedUpdateListener fixedListener:
                        _fixedUpdateListeners.Remove(fixedListener);
                        break;
                    case IGameLateUpdateListener lateListener:
                        _lateUpdateListeners.Remove(lateListener);
                        break;
                }
                
                if (listener is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        #region Lifecycle

        public void StartGame()
        {
            SetState(GameState.Start);

            foreach (var gameListener in _listeners)
            {
                if (gameListener is IGameStartListener gameStartListener)
                {
                    gameStartListener.OnStartGame();
                }
            }
        }

        public void FinishGame()
        {
            SetState(GameState.Finish);

            foreach (var gameListener in _listeners)
            {
                if (gameListener is IGameFinishListener gameFinishListener)
                {
                    gameFinishListener.OnFinishGame();
                }
            }
        }

        public void PauseGame()
        {
            SetState(GameState.Pause);

            foreach (var gameListener in _listeners)
            {
                if (gameListener is IGamePauseListener gamePauseListener)
                {
                    gamePauseListener.OnPause();
                }
            }
        }

        public void ResumeGame()
        {
            SetState(GameState.Resume);

            foreach (var gameListener in _listeners)
            {
                if (gameListener is IGameResumeListener gameResumeListener)
                {
                    gameResumeListener.OnResume();
                }
            }
        }

        /// <summary>
        /// Assigns the authoritative state and announces it. Every transition routes through here so the
        /// state value and the <see cref="GameStateChangedEvent"/> notification can never drift apart.
        /// </summary>
        private void SetState(GameState state)
        {
            _state = state;
            EventBus.Raise(new GameStateChangedEvent(state));
        }

        #endregion

        #region Updates

        private bool CanUpdate() => _state is GameState.Start or GameState.Resume;

        private void Update() 
        {
            if (!CanUpdate()) return;
			
            var deltaTime = Time.deltaTime;
			
            for (int i = 0; i < _updateListeners.Count; i++)
            {
                _updateListeners[i].OnUpdate(deltaTime);
            }
        }

        private void FixedUpdate() 
        {
            if (!CanUpdate()) return;
			
            var fixedDeltaTime = Time.fixedDeltaTime;
			
            for (int i = 0; i < _fixedUpdateListeners.Count; i++)
            {
                _fixedUpdateListeners[i].OnFixedUpdate(fixedDeltaTime);
            }
        }

        private void LateUpdate() 
        {
            if (!CanUpdate()) return;
			
            var deltaTime = Time.deltaTime;
			
            for (int i = 0; i < _lateUpdateListeners.Count; i++)
            {
                _lateUpdateListeners[i].OnLateUpdate(deltaTime);
            }
        }

        #endregion

        #region InternalHelpers

        private void RegisterListener(IGameListener gameListener)
        {
            RegisterListenerOfType(_updateListeners, gameListener);
            RegisterListenerOfType(_fixedUpdateListeners, gameListener);
            RegisterListenerOfType(_lateUpdateListeners, gameListener);
        }

        private void RegisterListenerOfType<T>(List<T> list, IGameListener gameListener) where T : class
        {
            if (gameListener is T listener)
            {
                list.Add(listener);
            }
        }

        /// <summary>
        /// When adding listeners mid-game, catch them up to current state, goes through all stages
        /// </summary>
        private void ApplyState(List<IGameListener> collection)
        {
            switch (_state)
            {
                case GameState.Start:
                    foreach (var gameListener in collection)
                    {
                        if (gameListener is IGameResumeListener gameResumeListener)
                        {
                            gameResumeListener.OnResume();
                        }
                    }
                    break;
                case GameState.Pause:
                    foreach (var gameListener in collection)
                    {
                        if (gameListener is IGameStartListener gameStartListener)
                        {
                            gameStartListener.OnStartGame();
                        }
                        if (gameListener is IGamePauseListener gamePauseListener)
                        {
                            gamePauseListener.OnPause();
                        }
                    }
                    break;
                case GameState.Resume:
                    foreach (var gameListener in collection)
                    {
                        if (gameListener is IGameStartListener gameStartListener)
                        {
                            gameStartListener.OnStartGame();
                        }
                        if (gameListener is IGamePauseListener gamePauseListener)
                        {
                            gamePauseListener.OnPause();
                        }
                        if (gameListener is IGameResumeListener gameResumeListener)
                        {
                            gameResumeListener.OnResume();
                        }
                    }
                    break;
                case GameState.Finish:
                    foreach (var gameListener in collection)
                    {
                        if (gameListener is IGameStartListener gameStartListener)
                        {
                            gameStartListener.OnStartGame();
                        }
                        if (gameListener is IGameFinishListener gameFinishListener)
                        {
                            gameFinishListener.OnFinishGame();
                        }
                    }
                    break;
            }
        }

        #endregion
    }
}