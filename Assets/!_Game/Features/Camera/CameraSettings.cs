using UnityEngine;

namespace Game.Feature.Camera
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Game/CameraSettings")]
    public class CameraSettings : ScriptableObject
    {
        public float moveSpeed = 5f;
        public float returnDuration = 1f;
        public AnimationCurve returnCurve;
        public float timeToHold = 1f;
        public float maxOutsideDistance = 2f;
        public AnimationCurve borderPushCurve; 
    }
}