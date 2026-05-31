using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Camera;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class WorldSceneInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private DayNightCycle _dayNightCycle;
        [SerializeField] private WorldFlow _worldFlow;
        [SerializeField] private WorldSceneData _worldSceneData;
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(_worldFlow, typeof(WorldFlow));
            builder.AddInterfacesAndSelf(_dayNightCycle);
            builder.AddSingleton(_worldSceneData, typeof(WorldSceneData));
            builder.AddSingleton(typeof(ScrollController), typeof(ScrollController), typeof(IDisposable));
            builder.AddSingleton(typeof(WorldCameraHandler), typeof(WorldCameraHandler), typeof(IGameListener));
        }
    }
}