using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Camera;
using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.GameWorld;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class WorldSceneInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private DaylightHandler _daylightHandler;
        [SerializeField] private WorldFlow _worldFlow;
        [SerializeField] private WorldSceneData _worldSceneData;
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(_worldFlow, typeof(WorldFlow));
            builder.AddSingleton(_daylightHandler, typeof(DaylightHandler));
            builder.AddSingleton(typeof(DayNightCycle), typeof(DayNightCycle), typeof(IGameListener), typeof(IDisposable));
            builder.AddSingleton(typeof(DaylightEcsBridge), typeof(DaylightEcsBridge), typeof(IDisposable)).NonLazy<DaylightEcsBridge>();
            builder.AddSingleton(_worldSceneData, typeof(WorldSceneData));
            builder.AddSingleton(typeof(ScrollController), typeof(ScrollController), typeof(IDisposable));
            builder.AddSingleton(typeof(WorldCameraHandler), typeof(WorldCameraHandler), typeof(IGameListener));
            builder.AddSingleton(typeof(Wallet), typeof(Wallet));
            builder.AddSingleton(typeof(BeaconCoreState), typeof(BeaconCoreState));
            builder.AddSingleton(typeof(WorldSaveService), typeof(WorldSaveService), typeof(IDisposable)).NonLazy<WorldSaveService>();
        }
    }
}
