using System;
using UnityEngine;

/// <summary>
/// Designer-facing pickup spawner tunables. Authored on <see cref="PickupConfigSO"/> and read by the
/// <c>PickupSpawnerAuthoring</c> baker at subscene-bake time (NOT a runtime blob). <see cref="Default"/>
/// is the behaviour-preserving fallback when the spawner has no config assigned; it mirrors the old
/// authoring field values. The prefab→CurrencyType mappings stay on the authoring (asset wiring).
/// </summary>
[Serializable]
public struct PickupTuning
{
    [Header("Lifetime")]
    [Tooltip("Total seconds a pickup lives before disappearing.")]
    public float Lifetime;
    [Tooltip("Percent of lifetime that the pickup will blink for.")]
    [Range(0f, 1f)] public float BlinkPercent;
    [Tooltip("Toggle interval while blinking.")]
    public float BlinkInterval;

    [Header("Pickup")]
    [Tooltip("World-space distance from cursor that triggers pickup.")]
    public float PickupRadius;

    [Header("Spawn")]
    [Tooltip("XZ scatter radius around the source position when spawning.")]
    public float Scatter;
    [Tooltip("Y position offset added to source spawn position.")]
    public float SpawnHeight;

    [Header("Float")]
    [Tooltip("Vertical bob amplitude in meters once settled.")]
    public float FloatAmplitude;
    [Tooltip("Seconds per full bob cycle.")]
    public float FloatPeriod;
    [Tooltip("Linear speed below which a disturbed pickup starts settling.")]
    public float RestSpeedThreshold;
    [Tooltip("Seconds the pickup must stay below rest speed before re-entering float state.")]
    public float RestDuration;

    public static PickupTuning Default => new PickupTuning
    {
        Lifetime = 10f,
        BlinkPercent = 0.15f,
        BlinkInterval = 0.15f,
        PickupRadius = 1.5f,
        Scatter = 0.6f,
        SpawnHeight = 0.5f,
        FloatAmplitude = 0.1f,
        FloatPeriod = 2.5f,
        RestSpeedThreshold = 0.1f,
        RestDuration = 0.1f,
    };
}
