using System;
using Reflex.Core;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Camera;
using BarkingBird.Runtime.Gameplay.Cursor;
using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Gameplay.Input.GrabAndThrow;
using BarkingBird.Runtime.Gameplay.Placement;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private BattleSceneData battleSceneData;
        [SerializeField] private BattleGroundSceneFlow _battleGroundSceneFlow;
        [SerializeField] private BeaconCoreHintView _beaconCoreHintView;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(battleSceneData, typeof(BattleSceneData));
            InstallInputs(builder);
            InstallBeacon(builder);

            builder.AddSingleton(_battleGroundSceneFlow, typeof(BattleGroundSceneFlow));
        }
        
        private void InstallBeacon(ContainerBuilder builder)
        {
            builder.AddSingleton(typeof(PlacementController), typeof(PlacementController), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(BeaconCoreController), typeof(BeaconCoreController), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(BeaconEcsBridge), typeof(BeaconEcsBridge), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));

            if (_beaconCoreHintView != null)
                builder.AddSingleton(_beaconCoreHintView, typeof(BeaconCoreHintView), typeof(IWorldInitializable));
        }

        private void InstallInputs(ContainerBuilder builder)
        {
            builder.AddSingleton(typeof(MousePositionProvider), typeof(MousePositionProvider));
            builder.AddSingleton(typeof(CursorMovementCalculations), typeof(CursorMovementCalculations), typeof(IGameListener));
        
            builder.AddSingleton(typeof(GrabbedEntityMover), typeof(GrabbedEntityMover), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(OverlapResolver), typeof(OverlapResolver), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(OverlapEjector), typeof(OverlapEjector), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(ReleaseCoordinator), typeof(ReleaseCoordinator), typeof(IWorldInitializable));
            builder.AddSingleton(typeof(ThrowTrajectoryPredictor), typeof(ThrowTrajectoryPredictor), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(GrabbingInteractor), typeof(GrabbingInteractor), typeof(IGameListener), typeof(IWorldInitializable));
            builder.AddSingleton(typeof(InteractController), typeof(InteractController), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));
            builder.AddSingleton(typeof(PowerHitController), typeof(PowerHitController), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));

            builder.AddSingleton(typeof(BattleCameraMovement), typeof(BattleCameraMovement), typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable));

            builder.AddSingleton(typeof(BattleCameraBorderSyncBridge), typeof(BattleCameraBorderSyncBridge), typeof(IGameListener), typeof(IWorldInitializable));

            builder.AddSingleton(typeof(CursorEcsBridge), typeof(CursorEcsBridge), typeof(IGameListener), typeof(IWorldInitializable));
        }
    }
}