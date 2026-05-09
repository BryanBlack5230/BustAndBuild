using System;
using Game.Configs;
using GameManagement;
using Reflex.Core;
using UnityEngine;

public class BootstrapInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private GameLoopManager _gameLoopManager;
    [SerializeField] private GameManagerUIController _gameManagerUIController;
    [SerializeField] private BootstrapFlow _bootstrapFlow;
    [SerializeField] private PrototypeConfigSetter _prototypeConfigSetter;
    [SerializeField] private ThrowSettingsSetter _throwSettingsSetter;
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