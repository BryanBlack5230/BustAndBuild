using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BarkingBird.Tests
{
    // StructureBoundsSystem caches each Structure's world-space AABB into TargetBounds once. The behaviour
    // that matters: it must use the WORLD transform — a unit pressed against a translated wall reads the
    // surface, not the local box centred on the origin. (See DistanceCalculationsRefactorTask §Step 1 / D3.)
    public class StructureBoundsSystemTests
    {
        private World _world;
        private BlobAssetReference<Collider> _collider;

        [SetUp]
        public void SetUp() => _world = new World("StructureBoundsTest");

        [TearDown]
        public void TearDown()
        {
            if (_collider.IsCreated) _collider.Dispose();
            if (_world is { IsCreated: true }) _world.Dispose();
        }

        // Half-extents (2,1,3); placed at world (10,0,0) → world AABB centred on the translation, NOT origin.
        [Test]
        public void StructureBoundsSystem_TranslatedStructure_CachesWorldAabbNotLocal()
        {
            var em = _world.EntityManager;
            var e = CreateStructure(em, new float3(10f, 0f, 0f), new float3(4f, 2f, 6f));

            Tick();

            Assert.That(em.HasComponent<TargetBounds>(e), Is.True, "TargetBounds not added");
            var aabb = em.GetComponentData<TargetBounds>(e).World;
            Assert.That(aabb.Center.x, Is.EqualTo(10f).Within(0.01f), "AABB used local space, not world");
            Assert.That(aabb.Min, Is.EqualTo(new float3(8f, -1f, -3f)).Using(Float3Within(0.01f)));
            Assert.That(aabb.Max, Is.EqualTo(new float3(12f, 1f, 3f)).Using(Float3Within(0.01f)));
        }

        // WithNone<TargetBounds> guard: a structure that already has bounds must be left untouched (the cache
        // is computed once; re-running must not clobber it).
        [Test]
        public void StructureBoundsSystem_AlreadyHasTargetBounds_LeavesItUntouched()
        {
            var em = _world.EntityManager;
            var e = CreateStructure(em, new float3(10f, 0f, 0f), new float3(4f, 2f, 6f));
            var sentinel = new Aabb { Min = new float3(-99f), Max = new float3(-98f) };
            em.AddComponentData(e, new TargetBounds { World = sentinel });

            Tick();

            Assert.That(em.GetComponentData<TargetBounds>(e).World.Min, Is.EqualTo(sentinel.Min).Using(Float3Within(0f)));
        }

        // Only Structure-marked entities get bounds; a plain collider must be ignored.
        [Test]
        public void StructureBoundsSystem_NonStructure_GetsNoBounds()
        {
            var em = _world.EntityManager;
            var e = CreateStructure(em, new float3(10f, 0f, 0f), new float3(4f, 2f, 6f), withStructure: false);

            Tick();

            Assert.That(em.HasComponent<TargetBounds>(e), Is.False);
        }

        // Self-maintaining + idempotent: a second tick is a no-op (the entity now has TargetBounds → WithNone
        // excludes it), and the cached value is unchanged.
        [Test]
        public void StructureBoundsSystem_SecondTick_KeepsBoundsStable()
        {
            var em = _world.EntityManager;
            var e = CreateStructure(em, new float3(10f, 0f, 0f), new float3(4f, 2f, 6f));

            Tick();
            var first = em.GetComponentData<TargetBounds>(e).World;
            Tick();
            var second = em.GetComponentData<TargetBounds>(e).World;

            Assert.That(second.Min, Is.EqualTo(first.Min).Using(Float3Within(0f)));
            Assert.That(second.Max, Is.EqualTo(first.Max).Using(Float3Within(0f)));
        }

        // --- helpers ---

        private Entity CreateStructure(EntityManager em, float3 pos, float3 size, bool withStructure = true)
        {
            _collider = BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Size = size,
                Orientation = quaternion.identity,
                BevelRadius = 0f,
            });

            var e = em.CreateEntity();
            if (withStructure) em.AddComponentData(e, new Structure());
            em.AddComponentData(e, new PhysicsCollider { Value = _collider });
            em.AddComponentData(e, new LocalToWorld { Value = float4x4.TRS(pos, quaternion.identity, 1f) });
            return e;
        }

        private void Tick()
        {
            _world.GetOrCreateSystem<StructureBoundsSystem>().Update(_world.Unmanaged);
            _world.EntityManager.CompleteAllTrackedJobs();
        }

        private static System.Collections.Generic.IEqualityComparer<float3> Float3Within(float tol) =>
            new Float3Comparer(tol);

        private sealed class Float3Comparer : System.Collections.Generic.IEqualityComparer<float3>
        {
            private readonly float _tol;
            public Float3Comparer(float tol) => _tol = tol;
            public bool Equals(float3 a, float3 b) => math.all(math.abs(a - b) <= _tol);
            public int GetHashCode(float3 v) => 0;
        }
    }
}
