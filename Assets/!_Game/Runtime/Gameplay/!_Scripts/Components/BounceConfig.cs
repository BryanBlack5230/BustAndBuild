using System;
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
    [Header("Soft landing (one unit flying onto a grounded unit)")]
    [Tooltip("Share of the impact damage dealt to the flying unit.")]
    public float SoftLandFlyingShare;
    [Tooltip("Share of the impact damage dealt to the grounded unit.")]
    public float SoftLandGroundedShare;
    [Tooltip("Minimum flying speed for a soft-land to register.")]
    public float SoftLandMinSpeed;
    [Tooltip("Fraction of the flying speed converted into knockback on the grounded unit.")]
    public float KnockMagnitudeFactor;
    [Tooltip("Upward fraction of that knockback.")]
    public float KnockUpwardFactor;

    [Header("Bounce")]
    [Tooltip("Fraction of velocity retained when a flying unit rebounds off another flying unit.")]
    public float ReboundElasticity;
    [Tooltip("Bounce damage = this * BaseDamage * velocityPower.")]
    public float BounceDamageFactor;

    [Header("Velocity-power fallback (used only when ThrowVelocitySettings is missing)")]
    public float FallbackMinVelocity;
    public float FallbackMaxVelocity;

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
