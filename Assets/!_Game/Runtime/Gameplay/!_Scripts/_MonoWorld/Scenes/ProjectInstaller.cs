using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
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
}