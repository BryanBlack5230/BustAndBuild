#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [Serializable]
    public sealed class DayCycleStateOverride : StateOverride
    {
        public enum TargetAction { StartDay, ForceFinish }

        [SerializeField] private TargetAction _action;

        public override UniTask Apply()
        {
            var dispatcher = Container.ProjectContainer.Resolve<CommandDispatcher>();

            switch (_action)
            {
                case TargetAction.StartDay:    dispatcher.Send(new StartDayCommand());       break;
                case TargetAction.ForceFinish: dispatcher.Send(new ForceFinishDayCommand()); break;
                default: throw new ArgumentOutOfRangeException(nameof(_action), _action, null);
            }

            return UniTask.CompletedTask;
        }
    }
}
