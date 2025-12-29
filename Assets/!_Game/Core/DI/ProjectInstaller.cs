using System;
using Game.Configs;
using Game.Feature.Input;
using GameEngine.Utils;
using Reflex.Core;
using UnityEngine;

public class ProjectInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton(new InputManager(), typeof(InputManager), typeof(IDisposable));
        builder.AddSingleton(typeof(LoadingService), typeof(LoadingService));
        builder.AddSingleton(typeof(ConfigContainer), typeof(ConfigContainer));
        builder.AddSingleton(typeof(CursorSetter), typeof(CursorSetter));
        
        // if (Application.isEditor) builder.AddSingleton(typeof(DummyCursorSetter), typeof(DummyCursorSetter), typeof(IGameListener), typeof(IDisposable));
    }
}
