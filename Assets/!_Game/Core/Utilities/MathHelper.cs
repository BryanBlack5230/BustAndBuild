using Unity.Mathematics;

namespace GameEngine.Utils
{
    public static class MathHelper
    {
        public static float GetHeading(float3 objectPosition, float3 targetPosition)
        {
            var x = objectPosition.x - targetPosition.x;
            var y = objectPosition.z - targetPosition.z;
            return math.atan2(x, y) + math.PI;
        }
    
        public static float3 GetForwardFromHeading(float heading)
        {
            return new float3(math.sin(heading), 0, math.cos(heading));
        }
    }
}