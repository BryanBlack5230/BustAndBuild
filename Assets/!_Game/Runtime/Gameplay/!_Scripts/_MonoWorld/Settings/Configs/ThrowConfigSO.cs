using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Throw + throw-physics tuning. Consumed by the grab/throw code (<c>GrabbingInteractor</c>,
/// <c>ThrowTrajectoryPredictor</c>) and baked into the <c>ThrowVelocitySettings</c> ECS singleton by
/// <c>BlobContainer</c> (curve → samples). Gravity is applied to the physics world by
/// <c>ThrowDebugTracker</c>. Migrated off the scene-bound <c>ThrowSettingsSetter</c> onto the ConfigHub.
/// Defaults match the retired Bootstrap-scene values.
/// </summary>
[CreateAssetMenu(fileName = "ThrowConfig", menuName = "Game/Config/Throw Config")]
public sealed class ThrowConfigSO : ScriptableObject
{
    [Title("Throw")]
    [Tooltip("Multiplies raw throw power into launch impulse.")]
    [SerializeField, Min(0f)] private float _throwScale = 0.1f;

    [Tooltip("Raw throw power above which a throw counts as 'fast' (tunnel handling).")]
    [SerializeField, Min(0f)] private float _throwThreshold = 100f;

    [Title("Velocity Power")]
    [Tooltip("Speed range mapped onto the velocity-power curve (x = min, y = max).")]
    [MinMaxSlider(0f, 100f, showFields: true)]
    [SerializeField] private Vector2 _minMaxVelocity = new Vector2(5f, 75f);

    [Tooltip("Maps normalised speed (0..1 across the min/max range) to velocity power.")]
    [SerializeField] private AnimationCurve _velocityPowerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Title("Physics")]
    [Tooltip("Downward gravity magnitude applied to the physics world (PhysicsStep.Gravity = -y).")]
    [SerializeField, Min(0f)] private float _gravity = 30f;

    public float ThrowScale => _throwScale;
    public float ThrowThreshold => _throwThreshold;
    public Vector2 MinMaxVelocity => _minMaxVelocity;
    public AnimationCurve VelocityPowerCurve => _velocityPowerCurve;
    public float Gravity => _gravity;
}
