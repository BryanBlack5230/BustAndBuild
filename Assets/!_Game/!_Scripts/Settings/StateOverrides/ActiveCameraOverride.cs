#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Game.Core.Events;
using UnityEngine;

namespace Game.Configs
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
