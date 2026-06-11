using System;
using UnityEngine;
using UnityEngine.UI;

namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    public class GameManagerUIController : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;

        private IDisposable _stateSubscription;

        private void OnEnable()
        {
            _stateSubscription = EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            _stateSubscription?.Dispose();
            _stateSubscription = null;
        }

        private void Start()
        {
            Render(GameState.Unknown);
        }

        public void SubscribeButtons(Action onStart, Action onPause, Action onResume)
        {
            _startButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.RemoveAllListeners();
            _resumeButton.onClick.RemoveAllListeners();

            _startButton.onClick.AddListener(() => onStart());
            _pauseButton.onClick.AddListener(() => onPause());
            _resumeButton.onClick.AddListener(() => onResume());
        }

        /// <summary>
        /// Renders button visibility from the authoritative <see cref="GameState"/>. Driven by
        /// <see cref="GameStateChangedEvent"/>, so the UI stays in sync no matter who changed the state.
        /// </summary>
        public void Render(GameState state)
        {
            switch (state)
            {
                case GameState.Start:
                case GameState.Resume:
                    Hide(_startButton);
                    Hide(_resumeButton);
                    Show(_pauseButton);
                    break;
                case GameState.Pause:
                    Hide(_startButton);
                    Hide(_pauseButton);
                    Show(_resumeButton);
                    break;
                case GameState.Finish:
                    Hide(_pauseButton);
                    Hide(_resumeButton);
                    break;
                case GameState.Unknown:
                default:
                    Show(_startButton);
                    Hide(_pauseButton);
                    Hide(_resumeButton);
                    break;
            }
        }

        private void OnGameStateChanged(in GameStateChangedEvent evt) => Render(evt.State);

        private void Show(Button button)
        {
            button.gameObject.SetActive(true);
        }

        private void Hide(Button button)
        {
            button.gameObject.SetActive(false);
        }
    }
}
