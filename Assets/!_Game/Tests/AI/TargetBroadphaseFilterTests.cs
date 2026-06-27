using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Tests
{
    // Integration: build a real CollisionWorld and run an actual point-distance query with the targeting
    // CollisionFilter. The collector-isolation tests bypass the query, so they can't catch a wrong filter
    // mask — and a wrong mask is exactly the bug that shipped (units live on Grabbable(8), NOT Unit(6), so
    // the original Unit|Obstacle filter returned only structures). This proves: a unit on Grabbable IS
    // discovered, a wall on Obstacle IS discovered, and the beacon on Default is NOT (it's scored separately).
    public class TargetBroadphaseFilterTests
    {
        private const float QueryRadius = 20f;

        // Mirrors TargetSearchSystem.OnCreate's _targetFilter exactly (the filter is private, so reproduced).
        private static CollisionFilter TargetFilter()
        {
            var unitBit = 1u << UnityEngine.LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Unit);
            var grabbableBit = 1u << UnityEngine.LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Grabbable);
            var obstacleBit = 1u << UnityEngine.LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Obstacle);
            return new CollisionFilter { BelongsTo = ~0u, CollidesWith = unitBit | grabbableBit | obstacleBit, GroupIndex = 0 };
        }

        private static uint LayerBit(string layer) => 1u << UnityEngine.LayerMask.NameToLayer(layer);

        [Test]
        public void TargetFilter_DiscoversGrabbableUnitAndObstacleWall_ExcludesDefaultBeacon()
        {
            using var world = new World("FilterTest");
            var em = world.EntityManager;
            var unit = em.CreateEntity();
            var beacon = em.CreateEntity();
            var wall = em.CreateEntity();

            // Units sit on Grabbable (the shipped reality), the beacon on Default, walls on Obstacle.
            var unitCollider = Box(LayerBit(RuntimeConstants.PhysicLayers.Grabbable));
            var beaconCollider = Box(LayerBit("Default"));
            var wallCollider = Box(LayerBit(RuntimeConstants.PhysicLayers.Obstacle));

            var pw = new PhysicsWorld(numStaticBodies: 3, numDynamicBodies: 0, numJoints: 0);
            try
            {
                var bodies = pw.StaticBodies;
                bodies[0] = Body(unitCollider, new float3(0f, 0f, 5f), unit);
                bodies[1] = Body(beaconCollider, new float3(5f, 0f, 0f), beacon);
                bodies[2] = Body(wallCollider, new float3(0f, 0f, 8f), wall);
                pw.CollisionWorld.BuildBroadphase(ref pw, 1f / 60f, new float3(0f, -9.81f, 0f), buildStaticTree: true);

                using var hits = new NativeList<Entity>(8, Allocator.Temp);
                var collector = new RecordingCollector(QueryRadius, hits);
                pw.CollisionWorld.CalculateDistance(
                    new PointDistanceInput { Position = float3.zero, MaxDistance = QueryRadius, Filter = TargetFilter() },
                    ref collector);

                Assert.That(Contains(hits, unit), Is.True, "unit on Grabbable layer must be discovered (the shipped bug)");
                Assert.That(Contains(hits, wall), Is.True, "wall on Obstacle layer must be discovered");
                Assert.That(Contains(hits, beacon), Is.False, "beacon on Default layer must be excluded (scored separately, D6)");
            }
            finally
            {
                pw.Dispose();
                unitCollider.Dispose();
                beaconCollider.Dispose();
                wallCollider.Dispose();
            }
        }

        // --- helpers ---

        private static BlobAssetReference<Collider> Box(uint belongsTo) =>
            BoxCollider.Create(
                new BoxGeometry { Center = float3.zero, Size = new float3(1f, 1f, 1f), Orientation = quaternion.identity, BevelRadius = 0f },
                new CollisionFilter { BelongsTo = belongsTo, CollidesWith = ~0u, GroupIndex = 0 });

        private static RigidBody Body(BlobAssetReference<Collider> collider, float3 pos, Entity e) =>
            new RigidBody
            {
                Collider = collider,
                WorldFromBody = new RigidTransform(quaternion.identity, pos),
                Entity = e,
                CustomTags = 0,
                Scale = 1f,
            };

        private static bool Contains(NativeList<Entity> list, Entity e)
        {
            for (var i = 0; i < list.Length; i++)
                if (list[i] == e) return true;
            return false;
        }
    }

    // Records every entity the query delivers; MaxFraction fixed at the query radius (never shrinks → all
    // in-range hits arrive). Used only to witness WHICH bodies the filter lets through.
    struct RecordingCollector : ICollector<DistanceHit>
    {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int NumHits { get; private set; }

        public NativeList<Entity> Hits;

        public RecordingCollector(float maxDistance, NativeList<Entity> hits) : this()
        {
            MaxFraction = maxDistance;
            Hits = hits;
        }

        public bool AddHit(DistanceHit hit)
        {
            Hits.Add(hit.Entity);
            NumHits++;
            return true;
        }
    }
}
