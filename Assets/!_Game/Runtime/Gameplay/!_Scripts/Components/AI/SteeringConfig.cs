using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    /// <summary>
    /// Flat tuning for the steering resolve step (<c>Steer_ResolveSystem</c>). Authored on the ConfigHub,
    /// pushed to a singleton at bootstrap. <see cref="Default"/> mirrors the pre-config literals.
    /// </summary>
    [Serializable]
    public struct SteeringConfig : IComponentData
    {
        [Tooltip("How strongly danger repels: score = interest - danger * this.")]
        [SuffixLabel("x", Overlay = true)] public float DangerMultiplier;
        [Tooltip("Distance ahead of the unit at which the resolved destination point is placed.")]
        [SuffixLabel("m", Overlay = true)] public float LookAheadDistance;

        public static SteeringConfig Default => new()
        {
            DangerMultiplier = 1f,
            LookAheadDistance = 2f,
        };
    }
}
