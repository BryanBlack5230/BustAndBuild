using System;
using Game.Feature.Input;
using GameManagement;
using Reflex.Core;
using UnityEngine;

public class WorldSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private DayNightCycle _dayNightCycle;
    [SerializeField] private WorldFlow _worldFlow;
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddInterfacesAndSelf(_dayNightCycle);
        builder.AddSingleton(typeof(ScrollController), typeof(ScrollController), typeof(IDisposable));
        
        builder.AddSingleton(_worldFlow, typeof(WorldFlow));
    }
}