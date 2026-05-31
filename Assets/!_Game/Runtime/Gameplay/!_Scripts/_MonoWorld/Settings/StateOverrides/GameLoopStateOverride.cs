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
        public enum TargetState { Start, Pause, Finish }

        [SerializeField] private TargetState _state;

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
                case TargetState.Start:  manager.StartGame();  break;
                case TargetState.Pause:  manager.PauseGame();  break;
                case TargetState.Finish: manager.FinishGame(); break;
                default: throw new ArgumentOutOfRangeException(nameof(_state), _state, null);
            }

            return UniTask.CompletedTask;
        }
    }
}
