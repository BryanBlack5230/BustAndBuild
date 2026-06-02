#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [Serializable]
    public sealed class ActiveCameraOverride : StateOverride
    {
        [SerializeField] private bool _switchUp;

        public override UniTask Apply()
        {
            var dispatcher = Container.ProjectContainer.Resolve<CommandDispatcher>();
            dispatcher.Send(new ChangeSceneCommand(_switchUp));
            return UniTask.CompletedTask;
        }
    }
}
