using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Currency;

public class PickupSpawnerAuthoring : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField, Tooltip("One prefab per resource type. Resources without a prefab assigned simply won't drop.")]
    private List<PickupPrefabMapping> prefabs = new();

    [Header("Tuning (from the ConfigHub)")]
    [SerializeField, Tooltip("Pickup tuning. Editing it re-bakes the subscene. Leave unassigned to fall back to PickupTuning.Default.")]
    private PickupConfigSO config;

    [SerializeField, Tooltip("Optional per-instance overrides layered on top of the config's tuning.")]
    private PickupTuningOverrides overrides;

    public class Baker : Baker<PickupSpawnerAuthoring>
    {
        public override void Bake(PickupSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            var typeCount = Enum.GetValues(typeof(CurrencyType)).Length;

            // Baked prefab references MUST live in a DynamicBuffer: Unity's baking step remaps Entity
            // ids to their runtime values for buffer elements, but does NOT traverse Entity fields
            // nested inside a FixedList on an IComponentData (those keep bake-time ids and are invalid
            // at Instantiate). Indexed by (int)CurrencyType; unassigned slots stay Entity.Null.
            var prefabRefs = AddBuffer<PickupPrefabRef>(entity);
            for (var i = 0; i < typeCount; i++)
                prefabRefs.Add(new PickupPrefabRef { Prefab = Entity.Null, Scale = 1f });

            foreach (var mapping in authoring.prefabs)
            {
                if (mapping.prefab == null) continue;

                if (mapping.prefab.GetComponent<PickupAuthoring>() == null)
                {
                    Debug.LogError(
                        $"[PickupSpawnerAuthoring] Prefab '{mapping.prefab.name}' mapped to {mapping.type} is missing a PickupAuthoring component. " +
                        "It will not spawn until a PickupAuthoring is attached.",
                        authoring);
                    continue;
                }

                var idx = (int)mapping.type;
                if (idx < 0 || idx >= typeCount) continue;

                var prefabEntity = GetEntity(mapping.prefab, TransformUsageFlags.Dynamic);
                // LocalTransform only supports uniform scale, so the X axis is representative.
                var scale = mapping.prefab.transform.localScale.x;
                prefabRefs[idx] = new PickupPrefabRef { Prefab = prefabEntity, Scale = scale };
            }

            if (authoring.config != null) DependsOn(authoring.config);
            var t = authoring.overrides.Apply(authoring.config != null ? authoring.config.Tuning : PickupTuning.Default);

            var blinkStart = Mathf.Max(0f, t.Lifetime * t.BlinkPercent);

            AddComponent(entity, new PickupSettings
            {
                PickupRadius = t.PickupRadius,
                Lifetime = t.Lifetime,
                BlinkStart = blinkStart,
                BlinkInterval = t.BlinkInterval,
                Scatter = t.Scatter,
                SpawnHeight = t.SpawnHeight,
                FloatAmplitude = t.FloatAmplitude,
                FloatPeriod = Mathf.Max(0.01f, t.FloatPeriod),
                RestSpeedThreshold = Mathf.Max(0f, t.RestSpeedThreshold),
                RestDuration = Mathf.Max(0f, t.RestDuration),
            });
        }
    }
}

[Serializable]
public struct PickupPrefabMapping
{
    public CurrencyType type;
    public GameObject prefab;
}

// Baked, remap-safe prefab reference. One buffer element per CurrencyType (indexed by enum value).
public struct PickupPrefabRef : IBufferElementData
{
    public Entity Prefab;
    public float Scale;
}

// Runtime-only value type that copies the (already-remapped) buffer into a Burst-job-friendly
// FixedList so it can be captured by value. NOT a baked component. FixedList512Bytes holds ~42
// entries — ample headroom for the enum.
public struct PickupPrefabMap
{
    public FixedList512Bytes<PickupPrefabRef> Entries;
}

public struct PickupSettings : IComponentData
{
    public float PickupRadius;
    public float Lifetime;
    public float BlinkStart;
    public float BlinkInterval;
    public float Scatter;
    public float SpawnHeight;
    public float FloatAmplitude;
    public float FloatPeriod;
    public float RestSpeedThreshold;
    public float RestDuration;
}
