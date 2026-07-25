using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Beacon Core insert tuning (ADR-0006 / ADR-0007). Consumed by <c>BeaconCoreController</c> (cost + free
/// timer + placement registration). Lives on the <c>ConfigHub</c> editing surface and is bound in DI at
/// bootstrap, mirroring <c>BeaconConfigSO</c>/<c>DaylightConfigSO</c>.
/// </summary>
[CreateAssetMenu(fileName = "BeaconCoreConfig", menuName = "Game/Config/Beacon Core Config")]
public sealed class BeaconCoreConfigSO : ScriptableObject
{
    [Tooltip("Pearls charged when the Core is inserted before the free timer elapses.")]
    [SerializeField, Min(0)] private int _insertCost = 100;

    [Tooltip("Seconds since the Core was dropped after which inserting it is free (binary, not a decay). " +
             "Resets each drop.")]
    [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _freeAfterSeconds = 300f;

    [Tooltip("Max linear speed the Core may have while InAir near the socket to count as a settled insert. " +
             "Tune generously — a released Core accelerates under gravity for the first frames of fall.")]
    [SerializeField, Min(0f), SuffixLabel("m/s", Overlay = true)] private float _insertThreshold = 3f;

    [Tooltip("Radius around the socket within which a settled Core is claimed for insertion.")]
    [SerializeField, Min(0f), SuffixLabel("m", Overlay = true)] private float _placeRadius = 2f;

    public int InsertCost => _insertCost;
    public float FreeAfterSeconds => _freeAfterSeconds;
    public float InsertThreshold => _insertThreshold;
    public float PlaceRadius => _placeRadius;
}
