using System.Collections.Generic;
using System.Linq;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameManagement
{
    public class GameLoopManager : MonoBehaviour
    {
        [System.Serializable]
        public enum State
        {
            Unknown,
            Start,
            Finish,
            Pause,
            Resume
        }
		
        [ShowInInspector] private List<IGameListener> _listeners = new();
        [ShowInInspector] private List<IGameUpdateListener> _updateListeners = new();
        [ShowInInspector] private List<IGameFixedUpdateListener> _fixedUpdateListeners = new();
        [ShowInInspector] private List<IGameLateUpdateListener> _lateUpdateListeners = new();
		
        [ShowInInspector] private State _state = State.Unknown;
		
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

		
        private bool CanUpdate()
        {
            return _state is State.Start or State.Resume;
        }
		
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
		
        public void StartGame()
        {
            _state = State.Start;
			
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
            _state = State.Finish;
			
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
            _state = State.Pause;

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
            _state = State.Resume;
			
            foreach (var gameListener in _listeners)
            {
                if (gameListener is IGameResumeListener gameResumeListener)
                {
                    gameResumeListener.OnResume();
                }
            }
        }
    }
}