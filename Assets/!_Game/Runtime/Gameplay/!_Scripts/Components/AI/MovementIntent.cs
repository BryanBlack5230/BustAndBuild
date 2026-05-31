using Unity.Entities;
using Unity.Mathematics;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public struct MovementIntent : IComponentData
    {
        public float3 targetPosition;
        public float3 avoidanceVector;
        public MovementMode mode;
    }
    
    public enum MovementMode : byte
    {
        Direct = 0,      // Move straight to target
        Combat = 1,      // Circle/strafe around target
        Retreat = 2      // Move away from danger
    }
}