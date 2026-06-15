using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Camera;
using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Gameplay.Input.GrabAndThrow;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private BattleSceneData battleSceneData;
        [SerializeField] private BattleGroundSceneFlow _battleGroundSceneFlow;

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
            builder.AddSingleton(typeof(ThrowTrajectoryPredictor), typeof(ThrowTrajectoryPredictor), typeof(IGameListener), typeof(IDisposable));
            builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener));
            builder.AddSingleton(typeof(InteractController), typeof(InteractController), typeof(IGameListener), typeof(IDisposable));
            builder.AddSingleton(typeof(PowerHitController), typeof(PowerHitController), typeof(IGameListener), typeof(IDisposable));
        
            builder.AddSingleton(typeof(BattleCameraMovement), typeof(BattleCameraMovement), typeof(IGameListener), typeof(IDisposable));
        
            builder.AddSingleton(typeof(BattleCameraBorderSyncBridge), typeof(BattleCameraBorderSyncBridge), typeof(IGameListener));

            builder.AddSingleton(typeof(CursorEcsBridge), typeof(CursorEcsBridge), typeof(IGameListener));
        }
    }
}