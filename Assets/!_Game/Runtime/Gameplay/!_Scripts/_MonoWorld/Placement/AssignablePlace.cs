using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BarkingBird.Runtime.Gameplay.Placement
{
    /// <summary>
    /// A spot a <c>Claimable</c> entity can be dropped into to be assigned there (ADR-0007). Registered with
    /// <see cref="PlacementController"/>; when a Claimable settles inside <see cref="Radius"/> of
    /// <see cref="Position"/> moving slower than <see cref="VelThreshold"/>, <see cref="OnClaimed"/> fires with
    /// that entity.
    ///
    /// POCO by design (a <c>float3</c> position, not a <c>Transform</c>) so the seam stays ECS-liftable when
    /// unit-assignment arrives. The first consumer is the Beacon socket; the named second is assign-Units-to-buildings.
    /// </summary>
    public sealed class AssignablePlace
    {
        public float3 Position;
        public readonly float Radius;
        public readonly float VelThreshold;
        public readonly Action<Entity> OnClaimed;

        public AssignablePlace(float3 position, float radius, float velThreshold, Action<Entity> onClaimed)
        {
            Position = position;
            Radius = radius;
            VelThreshold = velThreshold;
            OnClaimed = onClaimed;
        }
    }
}
