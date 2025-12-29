using UnityEngine;

namespace GameManagement
{
    public sealed class GameManager
    {
        private GameLoopManager _gameLoopManager;
        private GameManagerUIController _uiController;

        public GameManager(GameLoopManager gameLoopManager, GameManagerUIController uiController)
        {
            _gameLoopManager = gameLoopManager;
            _uiController = uiController;
			
            _uiController.SubscribeButtons(StartGame, PauseGame, ResumeGame);
        }

        public async void StartGame()
        {
            _uiController.OnStart();

            var countdown = new Countdown(3);
            await countdown.StartCountdownAsync();

            _gameLoopManager.StartGame();
        }

        public void PauseGame()
        {
            Debug.Log("Game paused!");
            _gameLoopManager.PauseGame();

            _uiController.OnPause();
        }

        public void ResumeGame()
        {
            Debug.Log("Game resumed!");
            _gameLoopManager.ResumeGame();

            _uiController.OnResume();
        }

        public void FinishGame()
        {
            Debug.Log("Game over!");
            _gameLoopManager.FinishGame();
        }
    }
}