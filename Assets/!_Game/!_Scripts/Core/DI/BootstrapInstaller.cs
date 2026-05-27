using System;
using Game.Configs;
using GameManagement;
using Reflex.Core;
using UnityEngine;

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
        builder.AddSingleton(typeof(BlobContainer), typeof(BlobContainer));
        builder.AddSingleton(_bootstrapFlow, typeof(BootstrapFlow));
        builder.AddSingleton(_throwSettingsSetter, typeof(ThrowSettingsSetter), typeof(IGameListener));
    }

    private void InstallGameLoop(ContainerBuilder builder)
    {
        builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
        builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
        builder.AddSingleton(typeof(DotsGameLoopBridge), typeof(DotsGameLoopBridge), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(GameManager), typeof(GameManager)).NonLazy<GameManager>();
    }
}
