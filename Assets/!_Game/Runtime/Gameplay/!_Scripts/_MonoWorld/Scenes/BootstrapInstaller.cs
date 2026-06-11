using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Save;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BootstrapInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private GameLoopManager _gameLoopManager = null!;
        [SerializeField] private GameManagerUIController _gameManagerUIController = null!;
        [SerializeField] private BootstrapFlow _bootstrapFlow = null!;
        [SerializeField] private PrototypeConfigSetter _prototypeConfigSetter = null!;
        [SerializeField] private ThrowSettingsSetter _throwSettingsSetter = null!;

        public void InstallBindings(ContainerBuilder builder)
        {
            InstallGameLoop(builder);
            builder.AddSingleton(_prototypeConfigSetter, typeof(PrototypeConfigSetter));
            builder.AddSingleton(typeof(BlobContainer), typeof(BlobContainer), typeof(IDisposable));
            builder.AddSingleton(_bootstrapFlow, typeof(BootstrapFlow));
            builder.AddSingleton(_throwSettingsSetter, typeof(ThrowSettingsSetter), typeof(IGameListener));
            builder.AddSingleton(typeof(ActiveSlot), typeof(ActiveSlot));
            builder.AddSingleton(typeof(DummySaveSystem), typeof(ISaveSystem));
        }

        private void InstallGameLoop(ContainerBuilder builder)
        {
            builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
            builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
            builder.AddSingleton(typeof(DotsGameLoopBridge), typeof(DotsGameLoopBridge), typeof(IGameListener), typeof(IDisposable));
            builder.AddSingleton(typeof(GameManager), typeof(GameManager)).NonLazy<GameManager>();
        }
    }
}