using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameManagement
{
    public class GameManagerUIController : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;

        private void Start()
        {
            Show(_startButton);
            Hide(_pauseButton);
            Hide(_resumeButton);
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

        public void OnStart()
        {
            Hide(_startButton);
            Show(_pauseButton);
        }

        public void OnPause()
        {
            Hide(_pauseButton);
            Show(_resumeButton);
        }

        public void OnResume()
        {
            Hide(_resumeButton);
            Show(_pauseButton);
        }

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