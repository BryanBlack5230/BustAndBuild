using BarkingBird.Runtime.Infrastructure.Utilities;
using NUnit.Framework;
using Unity.Mathematics;

namespace BarkingBird.Tests
{
    public class MathHelperTests
    {
        // Tracer bullet for the `Tests` asmdef: proves it compiles, that the Test Runner
        // discovers it, and that it can reach Runtime pure logic. MathHelper is a pure
        // static island (the cheapest possible SUT), so a red here means the harness is
        // broken, not the code. Grow real coverage from here — see
        // Claude/learnings/testing-methodology.md for the layer map and ECS one-tick pattern.
        [Test]
        public void GetForwardFromHeading_ZeroHeading_ReturnsWorldForward()
        {
            float3 forward = MathHelper.GetForwardFromHeading(0f);

            Assert.That(forward.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(forward.y, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(forward.z, Is.EqualTo(1f).Within(1e-5f));
        }
    }
}
