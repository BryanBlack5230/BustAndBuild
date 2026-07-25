using NUnit.Framework;

using BarkingBird.Runtime.Gameplay.Beacon;

namespace BarkingBird.Tests
{
    // BeaconCoreController.ComputeCost is the pure insert-cost rule (ADR-0006): full cost while the drop's
    // free-countdown is still running, then free once it elapses — binary, not a decay. Tested directly; the
    // surrounding controller needs a live world, this rule does not.
    public class BeaconCoreCostTests
    {
        private const int Cost = 100;

        [Test]
        public void ComputeCost_TimerRunning_ChargesFullCost()
        {
            Assert.That(BeaconCoreController.ComputeCost(300f, Cost), Is.EqualTo(Cost));
        }

        // Boundary: the smallest positive remaining still charges — free is strictly "timer reached zero".
        [Test]
        public void ComputeCost_JustBeforeFree_ChargesFullCost()
        {
            Assert.That(BeaconCoreController.ComputeCost(0.001f, Cost), Is.EqualTo(Cost));
        }

        // Boundary: remaining == 0 is the flip point to free.
        [Test]
        public void ComputeCost_TimerElapsed_IsFree()
        {
            Assert.That(BeaconCoreController.ComputeCost(0f, Cost), Is.EqualTo(0));
        }

        // Defensive: the timer is clamped at 0 at runtime, but the rule must still read negative as free.
        [Test]
        public void ComputeCost_NegativeRemaining_IsFree()
        {
            Assert.That(BeaconCoreController.ComputeCost(-5f, Cost), Is.EqualTo(0));
        }
    }
}
