using BarkingBird.Runtime.Infrastructure.Utilities;
using NUnit.Framework;
using Unity.Mathematics;

namespace BarkingBird.Tests
{
    public class MathHelperTests
    {
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
