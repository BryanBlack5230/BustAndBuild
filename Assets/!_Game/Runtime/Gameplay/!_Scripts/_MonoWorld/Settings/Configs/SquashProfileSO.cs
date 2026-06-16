using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SquashProfile", menuName = "Game/Hit Feedback/Squash Profile")]
public sealed class SquashProfileSO : ScriptableObject
{
    [InfoBox("On hit the unit compresses, then stretches back to normal over Duration.")]
    [SerializeField, Min(0.05f), SuffixLabel("s", Overlay = true)] private float _duration = 0.4f;
    [SerializeField, Range(0f, 0.9f)] private float _maxCompress = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _maxStretch = 0.18f;

    public float Duration => _duration;
    public float MaxCompress => _maxCompress;
    public float MaxStretch => _maxStretch;
}
