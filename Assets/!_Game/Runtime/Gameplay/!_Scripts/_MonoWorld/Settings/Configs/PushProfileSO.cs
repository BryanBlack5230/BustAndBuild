using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PushProfile", menuName = "Game/Hit Feedback/Push Profile")]
public sealed class PushProfileSO : ScriptableObject
{
    [Title("Knockback")]
    [SerializeField, Min(0f)] private float _horizontalImpulse = 3f;
    [SerializeField, Min(0f)] private float _upKick = 1f;

    [Title("Stun")]
    [SerializeField, Min(0.01f), SuffixLabel("s", Overlay = true)] private float _stunDuration = 0.25f;

    public float HorizontalImpulse => _horizontalImpulse;
    public float UpKick => _upKick;
    public float StunDuration => _stunDuration;
}
