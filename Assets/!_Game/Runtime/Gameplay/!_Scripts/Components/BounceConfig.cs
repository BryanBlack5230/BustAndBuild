using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Flat tuning shared by the bounce/landing systems (<c>InAirCollisionSystem</c>, <c>ScreenBounceSystem</c>).
/// Authored on the ConfigHub, pushed to a singleton at bootstrap. <see cref="Default"/> mirrors the
/// pre-config literals so the systems behave identically if the hub never bakes.
/// </summary>
[Serializable]
public struct BounceConfig : IComponentData
{
    [Title("Soft landing (one unit flying onto a grounded unit)")]
    [Tooltip("Share of the impact damage dealt to the flying unit.")]
    [PropertyRange(0f, 1f)] public float SoftLandFlyingShare;
    [Tooltip("Share of the impact damage dealt to the grounded unit.")]
    [PropertyRange(0f, 1f)] public float SoftLandGroundedShare;
    [Tooltip("Minimum flying speed for a soft-land to register.")]
    [SuffixLabel("m/s", Overlay = true)] public float SoftLandMinSpeed;
    [Tooltip("Fraction of the flying speed converted into knockback on the grounded unit.")]
    [PropertyRange(0f, 1f)] public float KnockMagnitudeFactor;
    [Tooltip("Upward fraction of that knockback.")]
    [PropertyRange(0f, 1f)] public float KnockUpwardFactor;

    [Title("Bounce")]
    [Tooltip("Fraction of velocity retained when a flying unit rebounds off another flying unit.")]
    [PropertyRange(0f, 1f)] public float ReboundElasticity;
    [Tooltip("Bounce damage = this * BaseDamage * velocityPower.")]
    [SuffixLabel("x", Overlay = true)] public float BounceDamageFactor;

    [Title("Velocity-power fallback (used only when ThrowVelocitySettings is missing)")]
    [SuffixLabel("m/s", Overlay = true)] public float FallbackMinVelocity;
    [SuffixLabel("m/s", Overlay = true)] public float FallbackMaxVelocity;

    public static BounceConfig Default => new()
    {
        SoftLandFlyingShare = 0.8f,
        SoftLandGroundedShare = 0.2f,
        SoftLandMinSpeed = 2f,
        KnockMagnitudeFactor = 0.5f,
        KnockUpwardFactor = 0.5f,
        ReboundElasticity = 0.15f,
        BounceDamageFactor = 0.5f,
        FallbackMinVelocity = 5f,
        FallbackMaxVelocity = 10f,
    };
}
