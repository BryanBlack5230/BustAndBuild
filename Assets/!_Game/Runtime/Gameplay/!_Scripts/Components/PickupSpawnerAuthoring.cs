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

    [Header("Lifetime")]
    [SerializeField, Tooltip("Total seconds a pickup lives before disappearing.")]
    private float lifetime = 10f;
    [SerializeField, Tooltip(("Percent of lifetime that the pickup will blink for.")), Range(0f, 1f)]
    private float blinkPercent = 0.15f;
    [SerializeField, Tooltip("Toggle interval while blinking.")]
    private float blinkInterval = 0.15f;

    [Header("Pickup")]
    [SerializeField, Tooltip("World-space distance from cursor that triggers pickup.")]
    private float pickupRadius = 1.5f;

    [Header("Spawn")]
    [SerializeField, Tooltip("XZ scatter radius around the source position when spawning.")]
    private float scatter = 0.6f;
    [SerializeField, Tooltip("Y position offset added to source spawn position.")]
    private float spawnHeight = 0.5f;

    [Header("Float")]
    [SerializeField, Tooltip("Vertical bob amplitude in meters once settled.")]
    private float floatAmplitude = 0.1f;
    [SerializeField, Tooltip("Seconds per full bob cycle.")]
    private float floatPeriod = 2.5f;
    [SerializeField, Tooltip("Linear speed below which a disturbed pickup starts settling.")]
    private float restSpeedThreshold = 0.1f;
    [SerializeField, Tooltip("Seconds the pickup must stay below rest speed before re-entering float state.")]
    private float restDuration = 0.1f;

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

            var blinkStart = Mathf.Max(0f, authoring.lifetime * authoring.blinkPercent);

            AddComponent(entity, new PickupSettings
            {
                PickupRadius = authoring.pickupRadius,
                Lifetime = authoring.lifetime,
                BlinkStart = blinkStart,
                BlinkInterval = authoring.blinkInterval,
                Scatter = authoring.scatter,
                SpawnHeight = authoring.spawnHeight,
                FloatAmplitude = authoring.floatAmplitude,
                FloatPeriod = Mathf.Max(0.01f, authoring.floatPeriod),
                RestSpeedThreshold = Mathf.Max(0f, authoring.restSpeedThreshold),
                RestDuration = Mathf.Max(0f, authoring.restDuration),
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
