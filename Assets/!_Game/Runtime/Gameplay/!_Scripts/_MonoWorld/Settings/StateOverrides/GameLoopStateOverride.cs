#nullable enable

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [Serializable]
    public sealed class GameLoopStateOverride : StateOverride
    {
        [SerializeField] private GameState _state = GameState.Start;

        public override UniTask Apply()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<GameLoopManager>();
            if (manager == null)
            {
                Log.Boot.W($"{nameof(GameLoopStateOverride)}: {nameof(GameLoopManager)} not found. Bootstrap scene must be loaded.");
                return UniTask.CompletedTask;
            }

            switch (_state)
            {
                case GameState.Start:  manager.StartGame();  break;
                case GameState.Pause:  manager.PauseGame();  break;
                case GameState.Resume: manager.ResumeGame(); break;
                case GameState.Finish: manager.FinishGame(); break;
                case GameState.Unknown:
                default:
                    Log.Boot.W($"{nameof(GameLoopStateOverride)}: state '{_state}' is not applicable; skipping.");
                    break;
            }

            return UniTask.CompletedTask;
        }
    }
}
