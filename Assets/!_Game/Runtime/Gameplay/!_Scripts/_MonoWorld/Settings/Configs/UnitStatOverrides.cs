using System;

/// <summary>
/// Optional per-instance overrides layered on top of a unit's per-type <see cref="UnitStats"/>.
/// Each field defaults to "use the profile value"; tick one to author a one-off (e.g. a tougher
/// individual unit) without re-authoring the whole block. <see cref="Apply"/> merges field-by-field.
/// </summary>
[Serializable]
public struct UnitStatOverrides
{
    public OptionalFloat MoveSpeed;
    public OptionalFloat TurnSpeed;
    public OptionalFloat StoppingDistance;
    public OptionalFloat Health;
    public OptionalFloat AttackDamage;
    public OptionalFloat AttackCooldown;
    public OptionalFloat AttackRange;
    public OptionalFloat BounceBaseDamage;
    public OptionalFloat BounceMultiplier;
    public OptionalFloat BounceElasticity;
    public OptionalFloat AgentSize;
    public OptionalFloat DangerWeight;
    public OptionalFloat SurroundRadius;
    public OptionalFloat VisionDistance;
    public OptionalFloat ScanInterval;
    public OptionalLayerMask ObstacleLayer;
    public OptionalCurve ObstacleDangerCurve;

    public UnitStats Apply(in UnitStats b) => new UnitStats
    {
        MoveSpeed = MoveSpeed.Resolve(b.MoveSpeed),
        TurnSpeed = TurnSpeed.Resolve(b.TurnSpeed),
        StoppingDistance = StoppingDistance.Resolve(b.StoppingDistance),
        Health = Health.Resolve(b.Health),
        AttackDamage = AttackDamage.Resolve(b.AttackDamage),
        AttackCooldown = AttackCooldown.Resolve(b.AttackCooldown),
        AttackRange = AttackRange.Resolve(b.AttackRange),
        BounceBaseDamage = BounceBaseDamage.Resolve(b.BounceBaseDamage),
        BounceMultiplier = BounceMultiplier.Resolve(b.BounceMultiplier),
        BounceElasticity = BounceElasticity.Resolve(b.BounceElasticity),
        AgentSize = AgentSize.Resolve(b.AgentSize),
        DangerWeight = DangerWeight.Resolve(b.DangerWeight),
        SurroundRadius = SurroundRadius.Resolve(b.SurroundRadius),
        VisionDistance = VisionDistance.Resolve(b.VisionDistance),
        ScanInterval = ScanInterval.Resolve(b.ScanInterval),
        ObstacleLayer = ObstacleLayer.Resolve(b.ObstacleLayer),
        ObstacleDangerCurve = ObstacleDangerCurve.Resolve(b.ObstacleDangerCurve),
    };
}
