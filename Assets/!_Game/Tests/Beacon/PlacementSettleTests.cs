using NUnit.Framework;
using Unity.Mathematics;

using BarkingBird.Runtime.Gameplay.Placement;

namespace BarkingBird.Tests
{
    // PlacementController.IsSettledInto is the velocity+radius half of the settle-check claim (ADR-0007); the
    // InAir half is the ECS query. Pure -> tested directly. Boundaries matter: both checks are inclusive (<=).
    public class PlacementSettleTests
    {
        private static readonly float3 Place = float3.zero;
        private const float Radius = 2f;
        private const float VelThreshold = 3f;

        [Test]
        public void IsSettledInto_SlowAndInsideRadius_True()
        {
            Assert.That(PlacementController.IsSettledInto(new float3(1f, 0f, 0f), 1f, Place, Radius, VelThreshold), Is.True);
        }

        [Test]
        public void IsSettledInto_InsideRadiusButTooFast_False()
        {
            Assert.That(PlacementController.IsSettledInto(new float3(1f, 0f, 0f), 5f, Place, Radius, VelThreshold), Is.False);
        }

        [Test]
        public void IsSettledInto_SlowButOutsideRadius_False()
        {
            Assert.That(PlacementController.IsSettledInto(new float3(5f, 0f, 0f), 1f, Place, Radius, VelThreshold), Is.False);
        }

        // Boundary: speed exactly at the threshold still counts (<=).
        [Test]
        public void IsSettledInto_SpeedExactlyAtThreshold_True()
        {
            Assert.That(PlacementController.IsSettledInto(Place, VelThreshold, Place, Radius, VelThreshold), Is.True);
        }

        // Boundary: distance exactly at the radius still counts (<=).
        [Test]
        public void IsSettledInto_DistanceExactlyAtRadius_True()
        {
            Assert.That(PlacementController.IsSettledInto(new float3(Radius, 0f, 0f), 0f, Place, Radius, VelThreshold), Is.True);
        }

        [Test]
        public void IsSettledInto_JustOverThreshold_False()
        {
            Assert.That(PlacementController.IsSettledInto(Place, VelThreshold + 0.01f, Place, Radius, VelThreshold), Is.False);
        }
    }
}
