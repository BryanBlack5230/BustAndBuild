using UnityEngine;

/// <summary>
/// Per-type tuning for a wall section. Read by <c>WallSectionAuthoring</c>'s baker at subscene-bake time
/// (edit-time re-bake). Referenced from the <c>ConfigHub</c> as the single editing surface; an individual
/// placement can still override via <c>WallSectionAuthoring.healthOverride</c> (e.g. a tougher wall).
/// Default matches the old authoring value.
/// </summary>
[CreateAssetMenu(fileName = "WallSectionConfig", menuName = "Game/Config/Wall Section Config")]
public sealed class WallSectionConfigSO : ScriptableObject
{
    [Tooltip("Wall-section hit points.")]
    [SerializeField, Min(0f)] private float _health = 200f;

    public float Health => _health;
}
