using UnityEngine;

/// <summary>
/// Power-hit tuning. Consumed by <c>PowerHitController</c>. Migrated off <c>Config.json</c> in Phase 3;
/// referenced from the <c>ConfigHub</c> and bound in DI at bootstrap. Defaults match the retired JSON values.
/// </summary>
[CreateAssetMenu(fileName = "PowerHitConfig", menuName = "Game/Config/Power Hit Config")]
public sealed class PowerHitConfigSO : ScriptableObject
{
    [Tooltip("How long, in seconds, a power-hit stays active after the press.")]
    [SerializeField, Min(0f)] private float _duration = 1f;

    [Tooltip("Impulse strength applied by the power-hit.")]
    [SerializeField, Min(0f)] private float _force = 2f;

    [Tooltip("Size response over the active window (0..1 normalised time).")]
    [SerializeField] private AnimationCurve _sizeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float Duration => _duration;
    public float Force => _force;
    public AnimationCurve SizeCurve => _sizeCurve;
}
