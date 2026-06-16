using System;
using Sirenix.OdinInspector;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;

/// <summary>
/// Designer-facing per-unit-type stat block. Authored on the unit profile SOs and read by the
/// <c>EnemyAuthoring</c>/<c>AllyAuthoring</c> bakers at subscene-bake time (NOT a runtime blob — these
/// values bake into the prefab's components). <see cref="EnemyDefault"/>/<see cref="AllyDefault"/> are the
/// behaviour-preserving fallbacks when an authoring has no profile assigned (e.g. isolated test scenes);
/// they mirror the shipped EnemyTest/AllyTest prefab values, not the old C# field defaults.
/// </summary>
[Serializable]
public struct UnitStats
{
    [Title("Movement")]
    [SuffixLabel("m/s", Overlay = true)] public float MoveSpeed;
    [SuffixLabel("deg/s", Overlay = true)] public float TurnSpeed;
    [SuffixLabel("m", Overlay = true)] public float StoppingDistance;

    [Title("Combat")]
    [SuffixLabel("HP", Overlay = true)] public float Health;
    [SuffixLabel("dmg", Overlay = true)] public float AttackDamage;
    [SuffixLabel("s", Overlay = true)] public float AttackCooldown;
    [SuffixLabel("m", Overlay = true)] public float AttackRange;

    [Title("Bounce")]
    [SuffixLabel("dmg", Overlay = true)] public float BounceBaseDamage;
    [SuffixLabel("x", Overlay = true)] public float BounceMultiplier;
    [PropertyRange(0f, 1f)] public float BounceElasticity;

    [Title("Steering / obstacle avoidance")]
    [SuffixLabel("m", Overlay = true)] public float AgentSize;
    [SuffixLabel("x", Overlay = true)] public float DangerWeight;
    [SuffixLabel("m", Overlay = true)] public float SurroundRadius;
    [SuffixLabel("m", Overlay = true)] public float VisionDistance;
    [SuffixLabel("s", Overlay = true)] public float ScanInterval;
    public LayerMask ObstacleLayer;
    public Curve ObstacleDangerCurve;

    // Mirrors EnemyTest.prefab so an enemy with no profile still behaves like the shipped one.
    public static UnitStats EnemyDefault => new UnitStats
    {
        MoveSpeed = 3f,
        TurnSpeed = 13f,
        StoppingDistance = 1f,
        Health = 50f,
        AttackDamage = 10f,
        AttackCooldown = 2f,
        AttackRange = 1.5f,
        BounceBaseDamage = 5f,
        BounceMultiplier = 2f,
        BounceElasticity = 0.8f,
        AgentSize = 0.5f,
        DangerWeight = 2f,
        SurroundRadius = 12f,
        VisionDistance = 5f,
        ScanInterval = 1f,
        ObstacleLayer = 512,
        ObstacleDangerCurve = Curve.Quadratic,
    };

    // Mirrors AllyTest.prefab (slower move speed + different danger curve than the enemy).
    public static UnitStats AllyDefault => new UnitStats
    {
        MoveSpeed = 2f,
        TurnSpeed = 13f,
        StoppingDistance = 1f,
        Health = 100f,
        AttackDamage = 20f,
        AttackCooldown = 2f,
        AttackRange = 1.5f,
        BounceBaseDamage = 5f,
        BounceMultiplier = 2f,
        BounceElasticity = 0.8f,
        AgentSize = 0.5f,
        DangerWeight = 2f,
        SurroundRadius = 12f,
        VisionDistance = 5f,
        ScanInterval = 1f,
        ObstacleLayer = 512,
        ObstacleDangerCurve = Curve.Quadruple,
    };
}
