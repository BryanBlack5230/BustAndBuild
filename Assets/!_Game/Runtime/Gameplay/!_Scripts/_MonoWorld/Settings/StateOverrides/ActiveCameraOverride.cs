#nullable enable

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [Serializable]
    public sealed class ActiveCameraOverride : StateOverride
    {
        [SerializeField] private bool _switchUp;

        public override UniTask Apply()
        {
            EventManager.Input.SceneChangeRequest?.Invoke(_switchUp);
            return UniTask.CompletedTask;
        }
    }
}
