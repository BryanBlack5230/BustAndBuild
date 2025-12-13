using System;
using Game.Feature.Camera;
using Game.Feature.Input;
using GameManagement;
using Reflex.Core;
using UnityEngine;

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