using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public static class PhysicsUtility
{
    /// <summary>
    /// Returns a random point inside the collider volume of a PhysicsCollider component.
    /// Works for any convex collider. If sampling fails, returns the collider center.
    /// </summary>
    public static float3 GetRandomPointInsideCollider(EntityManager em, Entity colliderEntity, ref Random random)
    {
        if (!em.Exists(colliderEntity) || !em.HasComponent<PhysicsCollider>(colliderEntity))
            return float3.zero;

        var collider = em.GetComponentData<PhysicsCollider>(colliderEntity);
        if (!collider.IsValid) 
            return float3.zero;

        var worldPos = float3.zero;
        var worldRot = quaternion.identity;

        if (em.HasComponent<LocalTransform>(colliderEntity))
        {
            var t = em.GetComponentData<LocalTransform>(colliderEntity);
            worldPos = t.Position;
            worldRot = t.Rotation;
        }

        ref var colliderRef = ref collider.Value.Value;
        var localAabb = colliderRef.CalculateAabb();

        var localSample = random.NextFloat3(localAabb.Min, localAabb.Max);
        var worldSample = math.transform(new RigidTransform(worldRot, worldPos), localSample);
        return worldSample;
    }
}