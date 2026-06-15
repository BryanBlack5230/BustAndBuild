using System;
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
        public float DangerMultiplier;
        [Tooltip("Distance ahead of the unit at which the resolved destination point is placed.")]
        public float LookAheadDistance;

        public static SteeringConfig Default => new()
        {
            DangerMultiplier = 1f,
            LookAheadDistance = 2f,
        };
    }
}
