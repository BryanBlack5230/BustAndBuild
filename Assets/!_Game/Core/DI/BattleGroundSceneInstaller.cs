using System;
using Game.Feature.Camera;
using Game.Feature.Input;
using Reflex.Core;
using UnityEngine;

public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private BattleSceneData battleSceneData;
    [SerializeField] private BattleGroundSceneFlow _battleGroundSceneFlow;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.OnContainerBuilt += PreGameManagementForceResolve;
        builder.AddSingleton(battleSceneData, typeof(BattleSceneData));
        InstallInputs(builder);

        builder.AddSingleton(_battleGroundSceneFlow, typeof(BattleGroundSceneFlow));

        return;
        void PreGameManagementForceResolve(Container container)
        {
            builder.OnContainerBuilt -= PreGameManagementForceResolve;

            // container.Resolve<CameraMovement>();
        }
    }

    private void InstallInputs(ContainerBuilder builder)
    {
        builder.AddSingleton(typeof(MousePositionProvider), typeof(MousePositionProvider));
        builder.AddSingleton(typeof(CursorMovementCalculations), typeof(CursorMovementCalculations), typeof(IGameListener));
        
        builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener));
        builder.AddSingleton(typeof(InteractController), typeof(InteractController), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(PowerHitController), typeof(PowerHitController), typeof(IGameListener), typeof(IDisposable));
        
        builder.AddSingleton(typeof(CameraMovement), typeof(CameraMovement), typeof(IGameListener), typeof(IDisposable));
    }
}
