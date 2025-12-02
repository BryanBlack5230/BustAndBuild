using UnityEngine;

namespace Game.Feature.Input
{
    [CreateAssetMenu(fileName = "PowerHitSettings", menuName = "Game/PowerHitSettings")]
    public class PowerHitSettings : ScriptableObject
    {
        public float duration;
        public float powerHitForce;
        public AnimationCurve powerHitSizeCurve;
    }
}