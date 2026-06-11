using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    public sealed class GameManager
    {
        private readonly GameLoopManager _gameLoopManager;
        private readonly GameManagerUIController _uiController;

        public GameManager(GameLoopManager gameLoopManager, GameManagerUIController uiController)
        {
            _gameLoopManager = gameLoopManager;
            _uiController = uiController;
			
            _uiController.SubscribeButtons(StartGame, PauseGame, ResumeGame);
        }

        public void StartGame() => StartGameAsync().Forget();

        private async UniTaskVoid StartGameAsync()
        {
            _uiController.Render(GameState.Start);

            var countdown = new Countdown(3);
            await countdown.StartCountdownAsync();

            _gameLoopManager.StartGame();
        }

        public void PauseGame()
        {
            Debug.Log("Game paused!");
            _gameLoopManager.PauseGame();
        }

        public void ResumeGame()
        {
            Debug.Log("Game resumed!");
            _gameLoopManager.ResumeGame();
        }

        public void FinishGame()
        {
            Debug.Log("Game over!");
            _gameLoopManager.FinishGame();
        }
    }
}