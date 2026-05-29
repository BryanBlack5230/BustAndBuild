using System;
using Game.Feature.Camera;
using Game.Feature.Input;
using Game.Settings;
using Reflex.Core;
using UnityEngine;

public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private BattleSceneData battleSceneData;
    [SerializeField] private BattleGroundSceneFlow _battleGroundSceneFlow;
    [SerializeField] private TrajectoryPredictorSettings _trajectoryPredictorSettings;
    
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton(battleSceneData, typeof(BattleSceneData));
        InstallInputs(builder);

        builder.AddSingleton(_battleGroundSceneFlow, typeof(BattleGroundSceneFlow));
    }

    private void InstallInputs(ContainerBuilder builder)
    {
        builder.AddSingleton(typeof(MousePositionProvider), typeof(MousePositionProvider));
        builder.AddSingleton(typeof(CursorMovementCalculations), typeof(CursorMovementCalculations), typeof(IGameListener));
        
        builder.AddSingleton(typeof(GrabbedEntityMover), typeof(GrabbedEntityMover), typeof(IDisposable));
        builder.AddSingleton(typeof(OverlapResolver), typeof(OverlapResolver), typeof(IDisposable));
        builder.AddSingleton(typeof(TunnelTeleporter), typeof(TunnelTeleporter), typeof(IDisposable));
        builder.AddSingleton(typeof(ReleaseCoordinator), typeof(ReleaseCoordinator));
        if (_trajectoryPredictorSettings == null)
            throw new InvalidOperationException($"{nameof(_trajectoryPredictorSettings)} is not assigned in the inspector.");
        builder.AddSingleton(_trajectoryPredictorSettings, typeof(TrajectoryPredictorSettings));
        builder.AddSingleton(typeof(ThrowTrajectoryPredictor), typeof(ThrowTrajectoryPredictor), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener));
        builder.AddSingleton(typeof(InteractController), typeof(InteractController), typeof(IGameListener), typeof(IDisposable));
        builder.AddSingleton(typeof(PowerHitController), typeof(PowerHitController), typeof(IGameListener), typeof(IDisposable));
        
        builder.AddSingleton(typeof(BattleCameraMovement), typeof(BattleCameraMovement), typeof(IGameListener), typeof(IDisposable));
        
        builder.AddSingleton(typeof(BattleCameraBorderSyncBridge), typeof(BattleCameraBorderSyncBridge), typeof(IGameListener));
    }
}
