using System;
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
    [Header("Movement")]
    public float MoveSpeed;
    public float TurnSpeed;
    public float StoppingDistance;

    [Header("Combat")]
    public float Health;
    public float AttackDamage;
    public float AttackCooldown;
    public float AttackRange;

    [Header("Bounce")]
    public float BounceBaseDamage;
    public float BounceMultiplier;
    public float BounceElasticity;

    [Header("Steering / obstacle avoidance")]
    public float AgentSize;
    public float DangerWeight;
    public float SurroundRadius;
    public float VisionDistance;
    public float ScanInterval;
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
