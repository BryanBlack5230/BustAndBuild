using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

// Computes each Structure's world-space AABB once and caches it in TargetBounds. Self-maintaining: a
// wall/beacon spawned later is picked up on the next tick, and WithNone<TargetBounds> keeps it a no-op
// afterwards. Structures don't move post-placement, so a one-shot world AABB is correct (if that ever
// changes, add a dirty path). Template: WallSectionInitSystem.
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct StructureBoundsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (collider, localToWorld, entity) in
                 SystemAPI.Query<RefRO<PhysicsCollider>, RefRO<LocalToWorld>>()
                     .WithAll<Structure>()
                     .WithNone<TargetBounds>()
                     .WithEntityAccess())
        {
            var ltw = localToWorld.ValueRO;
            // CalculateAabb() with no transform returns the collider's LOCAL-space box; pass the world
            // RigidTransform to get a world AABB (see BattleCoordinatorSystem.SetBases / unity-physics-gotchas).
            var worldAabb = collider.ValueRO.Value.Value.CalculateAabb(new RigidTransform(ltw.Rotation, ltw.Position));
            ecb.AddComponent(entity, new TargetBounds { World = worldAabb });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
