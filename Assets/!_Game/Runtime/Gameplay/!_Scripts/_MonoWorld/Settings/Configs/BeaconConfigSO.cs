using UnityEngine;

/// <summary>
/// Per-type tuning for the beacon structure. Read by <c>BeaconAuthoring</c>'s baker at subscene-bake time
/// (edit-time re-bake). Referenced from the <c>ConfigHub</c> as the single editing surface; an individual
/// placement can still override via <c>BeaconAuthoring.healthOverride</c>. Default matches the old authoring value.
/// </summary>
[CreateAssetMenu(fileName = "BeaconConfig", menuName = "Game/Config/Beacon Config")]
public sealed class BeaconConfigSO : ScriptableObject
{
    [Tooltip("Beacon hit points.")]
    [SerializeField, Min(0f)] private float _health = 6000f;

    public float Health => _health;
}
