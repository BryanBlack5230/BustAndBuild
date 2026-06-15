using PrimeTween;
using UnityEngine;

/// <summary>
/// Tuning for the pickup magnet (the HUD icon that flies a collected resource to its counter).
/// Referenced from the <c>ConfigHub</c> as the single editing surface, bound in DI at bootstrap and
/// injected into <c>PickupMagnetController</c>. Defaults match the old inline serialized values.
/// </summary>
[CreateAssetMenu(fileName = "PickupMagnetConfig", menuName = "Game/Config/Pickup Magnet Config")]
public sealed class PickupMagnetConfigSO : ScriptableObject
{
    [SerializeField, Min(0.05f)] private float _magnetDuration = 0.4f;
    [SerializeField] private Ease _magnetEase = Ease.InCubic;

    public float MagnetDuration => _magnetDuration;
    public Ease MagnetEase => _magnetEase;
}
