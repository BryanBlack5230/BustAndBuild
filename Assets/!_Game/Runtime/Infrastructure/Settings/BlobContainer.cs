using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Infrastructure.Settings
{
    /// <summary>
    /// Bakes the <see cref="ConfigHub"/> into ECS at bootstrap: per-unit-type targeting profiles into a
    /// blob, flat tuning groups into singleton components. <see cref="Initialize"/> is idempotent so the
    /// hub's Rebake button can re-run it live.
    /// </summary>
    public sealed class BlobContainer : IDisposable
    {
        private readonly ConfigHub _hub;

        private BlobAssetReference<TargetProfilesBlob> _profilesBlob;

        public BlobContainer(ConfigHub hub)
        {
            _hub = hub;
        }

        public void Initialize()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (_profilesBlob.IsCreated) _profilesBlob.Dispose();
            _profilesBlob = CreateProfilesBlob();

            SetSingleton(entityManager, new TargetProfiles { Blob = _profilesBlob }, "Global_Target_Profiles");
            SetSingleton(entityManager, _hub.Bounce, "Config_Bounce");
            SetSingleton(entityManager, _hub.Brain, "Config_BattleBrain");
            SetSingleton(entityManager, _hub.Steering, "Config_Steering");

            // Throw gravity is applied separately by ThrowDebugTracker (PhysicsStep loads with the battle subscene).
            if (_hub.ThrowConfig != null)
                SetSingleton(entityManager, BuildThrowVelocitySettings(_hub.ThrowConfig), "Config_ThrowVelocity");
            else
                Log.Boot.W("[BlobContainer] ThrowConfig missing on hub; ThrowVelocitySettings not baked (systems use the BounceConfig fallback).");
        }

        public void Dispose()
        {
            if (_profilesBlob.IsCreated) _profilesBlob.Dispose();
        }

        // Reuse the existing singleton if present so Rebake updates in place instead of spawning duplicates.
        private static void SetSingleton<T>(EntityManager entityManager, T data, string name)
            where T : unmanaged, IComponentData
        {
            var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());

            Entity entity;
            if (query.IsEmpty)
            {
                entity = entityManager.CreateEntity();
                entityManager.AddComponent<T>(entity);
                entityManager.SetName(entity, name);
            }
            else
            {
                entity = query.GetSingletonEntity();
            }

            entityManager.SetComponentData(entity, data);
            query.Dispose();
        }

        private BlobAssetReference<TargetProfilesBlob> CreateProfilesBlob()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TargetProfilesBlob>();

            BuildProfileArray(builder, ref root.EnemyProfiles, _hub.EnemyProfiles, Enum.GetValues(typeof(EnemyType)).Length);
            BuildProfileArray(builder, ref root.AllyProfiles, _hub.AllyProfiles, Enum.GetValues(typeof(AllyType)).Length);

            return builder.CreateBlobAssetReference<TargetProfilesBlob>(Allocator.Persistent);
        }

        // Sizes the blob array to the enum and places each profile at the slot matching its Type, so the
        // targeting job's `array[(int)enum]` lookup stays correct regardless of inspector list order.
        private static void BuildProfileArray<T>(BlobBuilder builder, ref BlobArray<TargetProfileBlob> dest, List<T> source, int slotCount)
            where T : ScriptableObject, IUnitProfile
        {
            var array = builder.Allocate(ref dest, slotCount);
            // Neutral default for unmapped types: zeroed targeting (detection radius 0 → never targets) but
            // IncomingDamageScale 1 so a missing profile takes full damage rather than turning immune (scale 0).
            for (var i = 0; i < slotCount; i++) array[i] = NeutralSlot;

            if (source == null) return;

            for (var i = 0; i < source.Count; i++)
            {
                var profile = source[i];
                if (profile == null) continue;

                var slot = profile.TypeValue;
                if (slot < 0 || slot >= slotCount) continue;

                array[slot] = ConvertToStruct(profile.Targeting, profile.Combat);
            }
        }

        private static TargetProfileBlob NeutralSlot => new TargetProfileBlob { IncomingDamageScale = 1f };

        // Bakes the throw velocity-power curve into 64 samples consumed by the bounce/landing systems.
        // Mirrors the sampling the old ThrowSettingsSetter did every frame; now baked at bootstrap + Rebake.
        private const int ThrowCurveSampleCount = 64;

        private static ThrowVelocitySettings BuildThrowVelocitySettings(ThrowConfigSO config)
        {
            var curve = config.VelocityPowerCurve;
            var samples = new FixedList512Bytes<float>();
            for (var i = 0; i < ThrowCurveSampleCount; i++)
            {
                var t = i / (float)(ThrowCurveSampleCount - 1);
                samples.Add(curve != null ? curve.Evaluate(t) : t);
            }

            return new ThrowVelocitySettings
            {
                MinVelocity = config.MinMaxVelocity.x,
                MaxVelocity = config.MinMaxVelocity.y,
                CurveSamples = samples,
            };
        }

        private static TargetProfileBlob ConvertToStruct(in TargetingProfile source, in CombatProfile combat) => new TargetProfileBlob
        {
            DetectionRadiusSq = source.DetectionRadius * source.DetectionRadius,
            ViewAngleCos = math.cos(math.radians(source.ViewAngleDegrees * 0.5f)),
            CheckInterval = source.CheckInterval,
            WeightEnemy = source.WeightEnemy,
            WeightAlly = source.WeightAlly,
            WeightWall = source.WeightWall,
            WeightBeacon = source.WeightBeacon,
            DistanceWeight = source.DistanceWeight,
            AggroBonus = source.AggroBonus,
            LineOfSightBonus = source.LineOfSightBonus,
            IncomingDamageScale = combat.IncomingDamageScale,
        };
    }

    public struct TargetProfilesBlob
    {
        // Arrays indexed by the unit-type enum value (EnemyType / AllyType).
        public BlobArray<TargetProfileBlob> EnemyProfiles;
        public BlobArray<TargetProfileBlob> AllyProfiles;
    }

    public struct TargetProfileBlob
    {
        // Derived forms, computed only in BlobContainer.ConvertToStruct.
        public float DetectionRadiusSq;   // DetectionRadius squared
        public float ViewAngleCos;        // cos(radians(ViewAngleDegrees * 0.5))
        public float CheckInterval;

        // Weights (> 0 pursue, 0 ignores the category).
        public float WeightEnemy;
        public float WeightAlly;
        public float WeightWall;
        public float WeightBeacon;

        // Modifiers.
        public float DistanceWeight;       // prefer-closer bias
        public float AggroBonus;           // candidate is targeting me
        public float LineOfSightBonus;     // candidate inside the view cone

        // Combat. Scales incoming bounce/landing damage (1 = full, 0.25 = quarter, 0 = immune).
        public float IncomingDamageScale;
    }
}
