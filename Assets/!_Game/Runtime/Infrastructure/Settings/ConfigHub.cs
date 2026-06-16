using System;
using System.Collections.Generic;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Gameplay.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    /// <summary>
    /// Single editing surface for runtime balance. Lives on the Bootstrap scene and is the source of
    /// truth for per-unit-type targeting profiles (SO assets) and the flat tuning groups.
    /// <see cref="BlobContainer"/> bakes everything here into ECS at bootstrap; the Rebake button
    /// re-bakes live in play mode.
    /// </summary>
    public class ConfigHub : MonoBehaviour
    {
        [Title("Units")]
        [Tooltip("One asset per EnemyType. Order does not matter — the baker keys each profile by its Type.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public List<EnemyUnitProfile> EnemyProfiles = new();

        [Tooltip("One asset per AllyType. Order does not matter — the baker keys each profile by its Type.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public List<AllyUnitProfile> AllyProfiles = new();

        public BounceConfig Bounce = BounceConfig.Default;

        [Title("Hit Feedback")]
        [Tooltip("On-hit flash juice. Read by HitFeedbackAuthoring's baker at bake time (editing re-bakes the subscene); point this at the same asset the unit prefabs use.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public FlashProfileSO FlashProfile;

        [Tooltip("On-hit squash-and-stretch juice. Baked into the prefab via HitFeedbackAuthoring.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public SquashProfileSO SquashProfile;

        [Tooltip("On-hit knockback impulse + stun. Baked into the prefab via HitFeedbackAuthoring.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public PushProfileSO PushProfile;

        public BattleBrainConfig Brain = BattleBrainConfig.Default;
        public SteeringConfig Steering = SteeringConfig.Default;

        [Tooltip("Camera drag/border tuning. Bound in DI at bootstrap and injected into BattleCameraMovement.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public CameraConfigSO CameraConfig;

        [Tooltip("Power-hit tuning. Bound in DI at bootstrap and injected into PowerHitController.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public PowerHitConfigSO PowerHitConfig;

        [Tooltip("Throw + throw-physics tuning. Baked into ThrowVelocitySettings by BlobContainer; gravity applied by ThrowDebugTracker.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public ThrowConfigSO ThrowConfig;

        [Tooltip("Day/night cycle tuning. Bound in DI at bootstrap and injected into DayNightCycle.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public DaylightConfigSO DaylightConfig;

        [Title("Trajectory Preview")]
        [Tooltip("Throw-arc preview tuning. Bound in DI at bootstrap and injected into ThrowTrajectoryPredictor.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public TrajectoryPredictorSettings TrajectoryPredictor;

        [Title("Structures")]
        [Tooltip("Beacon stats. Read by BeaconAuthoring's baker at bake time (editing it re-bakes the subscene).")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public BeaconConfigSO BeaconConfig;

        [Tooltip("Wall-section stats. Read by WallSectionAuthoring's baker at bake time (editing it re-bakes the subscene).")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public WallSectionConfigSO WallSectionConfig;

        [Title("Pickups")]
        [Tooltip("Pickup spawner tuning. Read by PickupSpawnerAuthoring's baker at bake time (editing it re-bakes the subscene).")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public PickupConfigSO PickupConfig;

        [Tooltip("Pickup magnet (HUD fly-to-counter) tuning. Bound in DI at bootstrap and injected into PickupMagnetController.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        public PickupMagnetConfigSO PickupMagnetConfig;

        private BlobContainer _blobContainer;

        [Inject]
        private void Construct(BlobContainer blobContainer) => _blobContainer = blobContainer;

        [PropertySpace(SpaceBefore = 12)]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        [DisableInEditorMode]
        [Tooltip("Re-bake every config into the running ECS world. Play mode only.")]
        private void Rebake()
        {
            if (_blobContainer == null)
            {
                Log.Boot.W("[ConfigHub] Rebake unavailable — BlobContainer was not injected.");
                return;
            }

            _blobContainer.Initialize();
            Log.Boot.D("[ConfigHub] Rebaked config into ECS.");
        }

        private void OnValidate()
        {
            ValidateProfiles(EnemyProfiles, Enum.GetValues(typeof(EnemyType)).Length, "Enemy");
            ValidateProfiles(AllyProfiles, Enum.GetValues(typeof(AllyType)).Length, "Ally");

            if (CameraConfig == null) Log.Boot.W("[ConfigHub] Camera config is unassigned; BattleCameraMovement will fail to bind.");
            if (PowerHitConfig == null) Log.Boot.W("[ConfigHub] PowerHit config is unassigned; PowerHitController will fail to bind.");
            if (ThrowConfig == null) Log.Boot.W("[ConfigHub] Throw config is unassigned; throw tuning + gravity will fail to bind.");
            if (DaylightConfig == null) Log.Boot.W("[ConfigHub] Daylight config is unassigned; DayNightCycle will fail to bind.");
            if (BeaconConfig == null) Log.Boot.W("[ConfigHub] Beacon config is unassigned; beacons bake with the default health.");
            if (WallSectionConfig == null) Log.Boot.W("[ConfigHub] Wall section config is unassigned; walls bake with the default health.");
            if (PickupConfig == null) Log.Boot.W("[ConfigHub] Pickup config is unassigned; the pickup spawner bakes with the default tuning.");

            if (FlashProfile == null) Log.Boot.W("[ConfigHub] Flash profile is unassigned on the hub editing surface.");
            if (SquashProfile == null) Log.Boot.W("[ConfigHub] Squash profile is unassigned on the hub editing surface.");
            if (PushProfile == null) Log.Boot.W("[ConfigHub] Push profile is unassigned on the hub editing surface.");
            if (TrajectoryPredictor == null) Log.Boot.W("[ConfigHub] Trajectory predictor settings unassigned; ThrowTrajectoryPredictor will fail to bind.");
            if (PickupMagnetConfig == null) Log.Boot.W("[ConfigHub] Pickup magnet config is unassigned; PickupMagnetController will fail to bind.");
        }

        private static void ValidateProfiles<T>(List<T> profiles, int expectedCount, string label)
            where T : UnityEngine.Object, IUnitProfile
        {
            if (profiles == null) return;

            var seen = new HashSet<int>();
            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (profile == null)
                {
                    Log.Boot.W($"[ConfigHub] {label} profile at index {i} is empty.");
                    continue;
                }

                if (!seen.Add(profile.TypeValue))
                    Log.Boot.W($"[ConfigHub] {label} profiles list has a duplicate type '{profile.TypeLabel}'.");
            }

            if (seen.Count < expectedCount)
                Log.Boot.W($"[ConfigHub] {label} profiles cover {seen.Count}/{expectedCount} types; missing types fall back to slot 0.");
        }
    }
}
