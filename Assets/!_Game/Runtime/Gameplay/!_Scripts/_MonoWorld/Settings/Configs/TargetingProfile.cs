using System;
using UnityEngine;

/// <summary>
/// Designer-facing targeting tunables for one unit type. Authored in meters/degrees;
/// <see cref="BarkingBird.Runtime.Infrastructure.Settings.BlobContainer"/> is the ONLY place that
/// derives the squared/cosine forms consumed by the targeting job.
/// </summary>
[Serializable]
public struct TargetingProfile
{
    [Tooltip("Detection range in meters.")]
    [Min(0f)] public float DetectionRadius;

    [Tooltip("Full field-of-view angle in degrees. The line-of-sight bonus applies inside half this angle.")]
    [Range(0f, 360f)] public float ViewAngleDegrees;

    [Tooltip("Seconds between target re-evaluations for this unit type.")]
    [Min(0f)] public float CheckInterval;

    [Header("Category weights (> 0 pursue, 0 ignores the category)")]
    public float WeightEnemy;
    public float WeightAlly;
    public float WeightWall;
    public float WeightBeacon;

    [Header("Modifiers")]
    [Tooltip("Scales the 'prefer closer' bias: (1 - distSq/detectionRadiusSq) * this.")]
    public float DistanceWeight;
    [Tooltip("Score bonus when the candidate is already targeting me.")]
    public float AggroBonus;
    [Tooltip("Score bonus when the candidate sits inside the view cone.")]
    public float LineOfSightBonus;
}

/// <summary>
/// Designer-facing per-unit-type combat tunables. Geometric properties (collider half-extents) are read
/// from the real <see cref="UnityEngine.Object"/> collider at runtime, not authored here, to avoid drift.
/// </summary>
[Serializable]
public struct CombatProfile
{
    [Tooltip("Scales incoming bounce/landing damage for this unit type. 1 = full, 0.25 = quarter, 0 = immune.")]
    [Min(0f)] public float IncomingDamageScale;
}

/// <summary>
/// Lets the hub and baker treat enemy/ally profile assets uniformly while each keeps its own typed enum.
/// </summary>
public interface IUnitProfile
{
    int TypeValue { get; }
    string TypeLabel { get; }
    TargetingProfile Targeting { get; }
    CombatProfile Combat { get; }
    UnitStats Stats { get; }
}
