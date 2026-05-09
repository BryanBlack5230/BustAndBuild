#nullable enable

using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Game.Feature.Input
{
    internal static class PhysicsOverlapHelper
    {
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
