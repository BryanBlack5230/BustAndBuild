using System;

/// <summary>
/// Optional per-instance overrides layered on top of a spawner's <see cref="PickupTuning"/>. Each field
/// defaults to "use the config value"; tick one to author a one-off. <see cref="Apply"/> merges field-by-field.
/// </summary>
[Serializable]
public struct PickupTuningOverrides
{
    public OptionalFloat Lifetime;
    public OptionalFloat BlinkPercent;
    public OptionalFloat BlinkInterval;
    public OptionalFloat PickupRadius;
    public OptionalFloat Scatter;
    public OptionalFloat SpawnHeight;
    public OptionalFloat FloatAmplitude;
    public OptionalFloat FloatPeriod;
    public OptionalFloat RestSpeedThreshold;
    public OptionalFloat RestDuration;

    public PickupTuning Apply(in PickupTuning b) => new PickupTuning
    {
        Lifetime = Lifetime.Resolve(b.Lifetime),
        BlinkPercent = BlinkPercent.Resolve(b.BlinkPercent),
        BlinkInterval = BlinkInterval.Resolve(b.BlinkInterval),
        PickupRadius = PickupRadius.Resolve(b.PickupRadius),
        Scatter = Scatter.Resolve(b.Scatter),
        SpawnHeight = SpawnHeight.Resolve(b.SpawnHeight),
        FloatAmplitude = FloatAmplitude.Resolve(b.FloatAmplitude),
        FloatPeriod = FloatPeriod.Resolve(b.FloatPeriod),
        RestSpeedThreshold = RestSpeedThreshold.Resolve(b.RestSpeedThreshold),
        RestDuration = RestDuration.Resolve(b.RestDuration),
    };
}
