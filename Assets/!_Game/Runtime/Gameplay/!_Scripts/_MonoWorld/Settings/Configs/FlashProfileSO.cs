using UnityEngine;

[CreateAssetMenu(fileName = "FlashProfile", menuName = "Game/Hit Feedback/Flash Profile")]
public sealed class FlashProfileSO : ScriptableObject
{
    [SerializeField] private Color _whiteColor = Color.white;
    [SerializeField] private Color _redColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField, Min(0.01f)] private float _toWhiteDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float _toRedDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float _toNormalDuration = 0.5f;

    public Color WhiteColor => _whiteColor;
    public Color RedColor => _redColor;
    public float ToWhiteDuration => _toWhiteDuration;
    public float ToRedDuration => _toRedDuration;
    public float ToNormalDuration => _toNormalDuration;
}