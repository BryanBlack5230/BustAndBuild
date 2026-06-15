#nullable enable
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public static class PhysicsUtility
    {
        internal static float2 GetEntityHalfExtentsXY(Entity entity, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PhysicsCollider>(entity)) return new float2(0.5f, 0.5f);
            var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            var collider       = entityManager.GetComponentData<PhysicsCollider>(entity);
            var aabb           = collider.Value.Value.CalculateAabb(new RigidTransform(localTransform.Rotation, float3.zero));
            return new float2((aabb.Max.x - aabb.Min.x) * 0.5f, (aabb.Max.y - aabb.Min.y) * 0.5f);
        }

        /// <summary>
        /// Local-space (unrotated) XY half-extents from a <see cref="PhysicsCollider"/>'s AABB. Burst-friendly —
        /// takes the component by value so it can be read from a job via a ComponentLookup. Returns
        /// <paramref name="fallback"/> when the collider blob is missing/invalid.
        /// </summary>
        internal static float2 GetColliderHalfExtentsXY(in PhysicsCollider collider, float2 fallback)
        {
            if (!collider.IsValid) return fallback;
            var aabb = collider.Value.Value.CalculateAabb();
            return new float2((aabb.Max.x - aabb.Min.x) * 0.5f, (aabb.Max.y - aabb.Min.y) * 0.5f);
        }

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

        internal static NativeList<RigidBody> CollectHitBodies(
            in PhysicsWorldSingleton physicsWorld,
            Aabb entityAabb,
            CollisionFilter filter,
            Entity selfEntity)
        {
            var results = new NativeList<RigidBody>(Allocator.Temp);
            var hits = new NativeList<int>(Allocator.Temp);
            try
            {
                physicsWorld.OverlapAabb(new OverlapAabbInput { Aabb = entityAabb, Filter = filter }, ref hits);
                for (var i = 0; i < hits.Length; i++)
                {
                    var bodyIdx = hits[i];
                    if (bodyIdx < 0 || bodyIdx >= physicsWorld.CollisionWorld.Bodies.Length) continue;
                    var body = physicsWorld.CollisionWorld.Bodies[bodyIdx];
                    if (body.Entity == selfEntity || !body.Collider.IsCreated) continue;
                    results.Add(body);
                }
            }
            finally
            {
                hits.Dispose();
            }
            return results;
        }

        internal static bool AabbsOverlapXY(Aabb a, Aabb b)
            => a.Min.x < b.Max.x && a.Max.x > b.Min.x &&
               a.Min.y < b.Max.y && a.Max.y > b.Min.y;
    }
}
