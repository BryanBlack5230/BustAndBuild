#nullable enable
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    internal static class EntityPhysicsHelper
    {
        public static float2 GetEntityHalfExtentsXY(Entity entity, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PhysicsCollider>(entity)) return new float2(0.5f, 0.5f);
            var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            var collider       = entityManager.GetComponentData<PhysicsCollider>(entity);
            var aabb           = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, float3.zero));
            return new float2((aabb.Max.x - aabb.Min.x) * 0.5f, (aabb.Max.y - aabb.Min.y) * 0.5f);
        }
    }
}
