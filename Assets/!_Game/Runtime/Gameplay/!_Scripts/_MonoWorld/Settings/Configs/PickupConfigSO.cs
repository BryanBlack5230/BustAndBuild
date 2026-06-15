using UnityEngine;

/// <summary>
/// Pickup spawner tuning. Read by <c>PickupSpawnerAuthoring</c>'s baker at subscene-bake time
/// (edit-time re-bake). Referenced from the <c>ConfigHub</c> as the single editing surface; an individual
/// spawner can still override via <c>PickupSpawnerAuthoring.overrides</c>. Defaults match the old authoring values.
/// The prefab→CurrencyType mappings stay on the authoring (asset wiring), not here.
/// </summary>
[CreateAssetMenu(fileName = "PickupConfig", menuName = "Game/Config/Pickup Config")]
public sealed class PickupConfigSO : ScriptableObject
{
    public PickupTuning Tuning = PickupTuning.Default;
}
