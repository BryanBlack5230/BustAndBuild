#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Reflex.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Settings
{
    [Serializable]
    public sealed class AddResourcesOverride : StateOverride
    {
        [Serializable]
        private struct ResourceGrant
        {
            [Tooltip("Resource type to grant.")]
            [EnumToggleButtons]
            public CurrencyType type;
            [Tooltip("Amount added to the wallet. Values <= 0 are ignored by the wallet.")]
            public int amount;
        }

        [SerializeField] private List<ResourceGrant> _grants = new();

        public override UniTask Apply()
        {
            var scene = SceneManager.GetSceneByBuildIndex(RuntimeConstants.Scenes.World);
            if (!scene.isLoaded)
                throw new InvalidOperationException(
                    $"World scene not loaded; cannot resolve {nameof(Wallet)} to grant resources.");

            var wallet = scene.GetSceneContainer().Resolve<Wallet>();

            for (var i = 0; i < _grants.Count; i++)
                wallet.Add(_grants[i].type, _grants[i].amount);

            return UniTask.CompletedTask;
        }
    }
}
