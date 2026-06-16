using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Tuning for the pickup magnet (the HUD icon that flies a collected resource to its counter).
/// Referenced from the <c>ConfigHub</c> as the single editing surface, bound in DI at bootstrap and
/// injected into <c>PickupMagnetController</c>. Defaults match the old inline serialized values.
/// </summary>
[CreateAssetMenu(fileName = "PickupMagnetConfig", menuName = "Game/Config/Pickup Magnet Config")]
public sealed class PickupMagnetConfigSO : ScriptableObject
{
    [Tooltip("Seconds the collected resource takes to fly from the world to its HUD counter.")]
    [SerializeField, Min(0.05f), SuffixLabel("s", Overlay = true)] private float _magnetDuration = 0.4f;

    [Tooltip("Easing applied to the fly-to-counter tween.")]
    [SerializeField] private Ease _magnetEase = Ease.InCubic;

    public float MagnetDuration => _magnetDuration;
    public Ease MagnetEase => _magnetEase;
}
