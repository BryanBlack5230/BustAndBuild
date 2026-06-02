using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    // TODO: merge into ConfigContainer once daylight tuning stabilises.
    public class DayNightSetting : MonoBehaviour
    {
        [SerializeField] private float _dayLength = 60f;
        [SerializeField] private float _sunDownDuration = 3f;
        [SerializeField] private bool _isLooping;

        public float DayLength => _dayLength;
        public float SunDownDuration => _sunDownDuration;
        public bool IsLooping => _isLooping;
    }
}
