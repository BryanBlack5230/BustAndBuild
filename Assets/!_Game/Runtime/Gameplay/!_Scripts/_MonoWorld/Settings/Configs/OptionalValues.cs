using System;
using Sirenix.OdinInspector;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.AI;

/// <summary>
/// Per-field instance override helpers used by the unit/structure authoring scripts. When <c>Override</c>
/// is on, the baker takes <c>Value</c>; otherwise it falls back to the per-type profile SO value.
/// Rendered inline (toggle + value on one row) so a full override block stays compact in the inspector.
/// </summary>
[Serializable, InlineProperty]
public struct OptionalFloat
{
    [HorizontalGroup("opt", 16f), HideLabel, Tooltip("Override this value on this instance.")]
    public bool Override;

    [HorizontalGroup("opt"), HideLabel, EnableIf(nameof(Override))]
    public float Value;

    public float Resolve(float fallback) => Override ? Value : fallback;
}

[Serializable, InlineProperty]
public struct OptionalLayerMask
{
    [HorizontalGroup("opt", 16f), HideLabel, Tooltip("Override this value on this instance.")]
    public bool Override;

    [HorizontalGroup("opt"), HideLabel, EnableIf(nameof(Override))]
    public LayerMask Value;

    public LayerMask Resolve(LayerMask fallback) => Override ? Value : fallback;
}

[Serializable, InlineProperty]
public struct OptionalCurve
{
    [HorizontalGroup("opt", 16f), HideLabel, Tooltip("Override this value on this instance.")]
    public bool Override;

    [HorizontalGroup("opt"), HideLabel, EnableIf(nameof(Override))]
    public Curve Value;

    public Curve Resolve(Curve fallback) => Override ? Value : fallback;
}
