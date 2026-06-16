using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Day/night cycle tuning. Consumed by <c>DayNightCycle</c>. Migrated off the scene-bound
/// <c>DayNightSetting</c> MonoBehaviour onto the ConfigHub (single editing surface); bound in DI at
/// bootstrap. Defaults match the retired DayNightSetting values.
/// </summary>
[CreateAssetMenu(fileName = "DaylightConfig", menuName = "Game/Config/Daylight Config")]
public sealed class DaylightConfigSO : ScriptableObject
{
    [Tooltip("Real seconds for a full in-game day.")]
    [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _dayLength = 60f;

    [Tooltip("Real seconds for the sundown fade after the day ends or is force-finished.")]
    [SerializeField, Min(0f), SuffixLabel("s", Overlay = true)] private float _sunDownDuration = 3f;

    [Tooltip("Restart the day automatically when it completes.")]
    [SerializeField, ToggleLeft] private bool _isLooping;

    public float DayLength => _dayLength;
    public float SunDownDuration => _sunDownDuration;
    public bool IsLooping => _isLooping;
}
