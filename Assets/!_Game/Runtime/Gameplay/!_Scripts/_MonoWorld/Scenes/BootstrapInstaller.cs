using System;
using Reflex.Core;
using UnityEngine;
using UnityEngine.Serialization;

using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Save;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public class BootstrapInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private GameLoopManager _gameLoopManager = null!;
        [SerializeField] private GameManagerUIController _gameManagerUIController = null!;
        [SerializeField] private BootstrapFlow _bootstrapFlow = null!;
        [FormerlySerializedAs("_prototypeConfigSetter")]
        [SerializeField] private ConfigHub _configHub = null!;
        [FormerlySerializedAs("_throwSettingsSetter")]
        [SerializeField] private ThrowDebugTracker _throwDebugTracker = null!;

        public void InstallBindings(ContainerBuilder builder)
        {
            InstallGameLoop(builder);
            builder.AddSingleton(_configHub, typeof(ConfigHub));
            InstallHubConfigs(builder);
            builder.AddSingleton(typeof(BlobContainer), typeof(BlobContainer), typeof(IDisposable));
            builder.AddSingleton(_bootstrapFlow, typeof(BootstrapFlow));
            builder.AddSingleton(_throwDebugTracker, typeof(ThrowDebugTracker), typeof(IGameListener));
            builder.AddSingleton(typeof(ActiveSlot), typeof(ActiveSlot));
            builder.AddSingleton(typeof(DummySaveSystem), typeof(ISaveSystem));
        }

        // Plain SO configs live on the hub (single editing surface) but are bound as their own types so
        // descendant-scope consumers depend on exactly what they need, not the whole hub. Each gets a
        // null-guard so an unassigned hub field fails fast at install instead of NRE'ing deep in a scene.
        private void InstallHubConfigs(ContainerBuilder builder)
        {
            if (_configHub.CameraConfig == null)
                throw new InvalidOperationException("ConfigHub.CameraConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.CameraConfig, typeof(CameraConfigSO));

            if (_configHub.PowerHitConfig == null)
                throw new InvalidOperationException("ConfigHub.PowerHitConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.PowerHitConfig, typeof(PowerHitConfigSO));

            if (_configHub.ThrowConfig == null)
                throw new InvalidOperationException("ConfigHub.ThrowConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.ThrowConfig, typeof(ThrowConfigSO));

            if (_configHub.DaylightConfig == null)
                throw new InvalidOperationException("ConfigHub.DaylightConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.DaylightConfig, typeof(DaylightConfigSO));

            if (_configHub.TrajectoryPredictor == null)
                throw new InvalidOperationException("ConfigHub.TrajectoryPredictor is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.TrajectoryPredictor, typeof(TrajectoryPredictorSettings));

            if (_configHub.PickupMagnetConfig == null)
                throw new InvalidOperationException("ConfigHub.PickupMagnetConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.PickupMagnetConfig, typeof(PickupMagnetConfigSO));

            if (_configHub.BeaconCoreConfig == null)
                throw new InvalidOperationException("ConfigHub.BeaconCoreConfig is not assigned in the Bootstrap scene.");
            builder.AddSingleton(_configHub.BeaconCoreConfig, typeof(BeaconCoreConfigSO));
        }

        private void InstallGameLoop(ContainerBuilder builder)
        {
            builder.AddSingleton(_gameLoopManager, typeof(GameLoopManager));
            builder.AddSingleton(_gameManagerUIController, typeof(GameManagerUIController));
            builder.AddSingleton(typeof(DotsGameLoopBridge), typeof(DotsGameLoopBridge), typeof(IGameListener), typeof(IDisposable));
            builder.AddSingleton(typeof(GameManager), typeof(GameManager)).NonLazy<GameManager>();
        }
    }
}