using UnityEngine;

/// <summary>
/// Camera drag/border tuning. Consumed by <c>BattleCameraMovement</c> and its drag/border handlers.
/// Migrated off <c>Config.json</c> in Phase 3 (AnimationCurves serialize cleanly in SOs); referenced
/// from the <c>ConfigHub</c> and bound in DI at bootstrap. Defaults match the retired JSON values.
/// </summary>
[CreateAssetMenu(fileName = "CameraConfig", menuName = "Game/Config/Camera Config")]
public sealed class CameraConfigSO : ScriptableObject
{
    [Tooltip("Drag sensitivity: world units of camera offset per pixel of mouse movement.")]
    [SerializeField, Min(0f)] private float _moveSpeed = 0.1f;

    [Tooltip("Seconds the camera takes to ease back inside the bounds after an out-of-bounds drag.")]
    [SerializeField, Min(0f)] private float _returnDuration = 1f;

    [Tooltip("Easing curve for the snap-back to bounds.")]
    [SerializeField] private AnimationCurve _returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Seconds the pointer must be held before a grab is treated as a camera drag.")]
    [SerializeField, Min(0f)] private float _timeToHold = 1f;

    [Tooltip("Distance past the border at which drag resistance reaches full strength.")]
    [SerializeField, Min(0f)] private float _maxOutsideDistance = 15f;

    [Tooltip("Maps how far past the border the camera is (0..1) to drag resistance.")]
    [SerializeField] private AnimationCurve _borderPushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float MoveSpeed => _moveSpeed;
    public float ReturnDuration => _returnDuration;
    public AnimationCurve ReturnCurve => _returnCurve;
    public float TimeToHold => _timeToHold;
    public float MaxOutsideDistance => _maxOutsideDistance;
    public AnimationCurve BorderPushCurve => _borderPushCurve;
}
