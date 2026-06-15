using System;
using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    /// <summary>
    /// Flat tuning for the brain/escape behaviours (<c>BattleBrainSystem</c>, <c>EnemyEscapeSystem</c>).
    /// Authored on the ConfigHub, pushed to a singleton at bootstrap. <see cref="Default"/> mirrors the
    /// pre-config literals.
    /// </summary>
    [Serializable]
    public struct BattleBrainConfig : IComponentData
    {
        [Tooltip("Evade engages when distSq <= attackRangeSq * this. (Temporary quick-fix knob.)")]
        public float EvadeTriggerRangeMultiplier;
        [Tooltip("How far (m) a unit retreats from its target while evading.")]
        public float EvadeRetreatDistance;
        [Tooltip("Seconds an enemy must dwell inside its base (day ended or scared) before escaping.")]
        public float EscapeDwellSeconds;

        public static BattleBrainConfig Default => new()
        {
            EvadeTriggerRangeMultiplier = 32f,
            EvadeRetreatDistance = 3f,
            EscapeDwellSeconds = 2f,
        };
    }
}
