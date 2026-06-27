using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Gameplay.AI;

namespace BarkingBird.Tests
{
    // ObstacleDangerCollector folds broadphase obstacle hits into the 8-bin ContextMap danger field. The
    // danger magnitudes are flagged for in-play re-tuning (DangerWeight/Curve/radii), so these tests assert
    // INVARIANTS that survive re-tuning — directional smear, front/back range asymmetry, skip-walls, monotonic
    // by distance, max-fold — never exact danger numbers. (Reuses LookupSource from TargetScoringCollectorTests.)
    public class ObstacleDangerCollectorTests
    {
        private World _world;
        private LookupSource _lookups;

        [SetUp]
        public void SetUp()
        {
            _world = new World("ObstacleTest");
            _lookups = _world.GetOrCreateSystemManaged<LookupSource>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world is { IsCreated: true }) _world.Dispose();
        }

        // The unit's own collider can come back at distance 0 — it must never scare itself.
        [Test]
        public void AddHit_SelfEntity_IgnoredNoDanger()
        {
            var self = NewObstacle();
            var collector = NewCollector(self);

            var accepted = collector.AddHit(Hit(self, 0f, float3.zero));

            Assert.That(accepted, Is.False);
            Assert.That(collector.NumHits, Is.EqualTo(0));
            Assert.That(MaxBin(collector.Danger), Is.EqualTo(0f));
        }

        // A unit deliberately heading for a wall must NOT steer away from it (WallSection root colliders).
        [Test]
        public void AddHit_SkipWalls_RejectsWallSection()
        {
            var self = NewObstacle();
            var wall = NewWith(new WallSection());
            var collector = NewCollector(self, skipWalls: true);

            var accepted = collector.AddHit(Hit(wall, 2f, new float3(0f, 0f, 2f)));

            Assert.That(accepted, Is.False);
        }

        // ...and the same for WallReference detail child colliders (the collector checks both lookups).
        [Test]
        public void AddHit_SkipWalls_RejectsWallReference()
        {
            var self = NewObstacle();
            var wallChild = NewWith(new WallReference());
            var collector = NewCollector(self, skipWalls: true);

            var accepted = collector.AddHit(Hit(wallChild, 2f, new float3(0f, 0f, 2f)));

            Assert.That(accepted, Is.False);
        }

        // When NOT targeting walls, a wall is a normal obstacle and contributes danger.
        [Test]
        public void AddHit_NotSkippingWalls_WallContributesDanger()
        {
            var self = NewObstacle();
            var wall = NewWith(new WallSection());
            var collector = NewCollector(self, skipWalls: false);

            var accepted = collector.AddHit(Hit(wall, 2f, new float3(0f, 0f, 2f)));

            Assert.That(accepted, Is.True);
            Assert.That(MaxBin(collector.Danger), Is.GreaterThan(0f));
        }

        // Front hits scan SurroundRadius + VisionDistance; an equidistant SIDE hit (beyond SurroundRadius) is
        // out of range. Same distance, opposite outcomes → proves the front-vision asymmetry.
        [Test]
        public void AddHit_FrontReachesIntoVision_SideLimitedToSurround()
        {
            var self = NewObstacle();
            var frontHit = NewObstacle();
            var sideHit = NewObstacle();
            // SurroundRadius 3, VisionDistance 5 → front range 8, side range 3. Distance 5 splits them.
            var front = NewCollector(self, surroundRadius: 3f, visionDistance: 5f);
            var side = NewCollector(self, surroundRadius: 3f, visionDistance: 5f);

            var frontAccepted = front.AddHit(Hit(frontHit, 5f, new float3(0f, 0f, 5f)));
            var sideAccepted = side.AddHit(Hit(sideHit, 5f, new float3(5f, 0f, 0f)));

            Assert.That(frontAccepted, Is.True, "front hit within vision range should register");
            Assert.That(sideAccepted, Is.False, "side hit beyond SurroundRadius should be rejected");
        }

        // Even a front hit past (SurroundRadius + VisionDistance) is rejected.
        [Test]
        public void AddHit_BeyondMaxRange_Rejected()
        {
            var self = NewObstacle();
            var obstacle = NewObstacle();
            var collector = NewCollector(self, surroundRadius: 3f, visionDistance: 5f);

            var accepted = collector.AddHit(Hit(obstacle, 10f, new float3(0f, 0f, 10f)));

            Assert.That(accepted, Is.False);
        }

        // Closer obstacle ⇒ more danger in its bin (monotonic), regardless of the curve/weight chosen.
        [Test]
        public void AddHit_CloserObstacle_ProducesMoreDanger()
        {
            var self = NewObstacle();
            var nearObstacle = NewObstacle();
            var farObstacle = NewObstacle();
            var nearC = NewCollector(self, surroundRadius: 5f);
            var farC = NewCollector(self, surroundRadius: 5f);

            nearC.AddHit(Hit(nearObstacle, 1f, new float3(0f, 0f, 1f)));
            farC.AddHit(Hit(farObstacle, 4f, new float3(0f, 0f, 4f)));

            Assert.That(nearC.Danger.N, Is.GreaterThan(farC.Danger.N));
        }

        // Danger is smeared toward the hit direction: a hit due-North raises N and leaves the opposite bin (S)
        // at zero (dot(N, S) < 0 ⇒ clamped to 0).
        [Test]
        public void AddHit_SmearsTowardHitDirection_OppositeBinZero()
        {
            var self = NewObstacle();
            var obstacle = NewObstacle();
            var collector = NewCollector(self, surroundRadius: 5f);

            collector.AddHit(Hit(obstacle, 2f, new float3(0f, 0f, 2f))); // +Z == N (bin 0)

            Assert.That(collector.Danger.N, Is.GreaterThan(0f));
            Assert.That(collector.Danger.S, Is.EqualTo(0f));
        }

        // Two hits feeding the same bin keep the MAX, not the sum — adding a weaker far hit behind a near one
        // doesn't raise the bin.
        [Test]
        public void AddHit_MultipleHitsSameBin_KeepsMaxNotSum()
        {
            var self = NewObstacle();
            var nearObstacle = NewObstacle();
            var farObstacle = NewObstacle();
            var nearOnly = NewCollector(self, surroundRadius: 5f);
            var nearThenFar = NewCollector(self, surroundRadius: 5f);

            nearOnly.AddHit(Hit(nearObstacle, 1f, new float3(0f, 0f, 1f)));

            nearThenFar.AddHit(Hit(nearObstacle, 1f, new float3(0f, 0f, 1f)));
            nearThenFar.AddHit(Hit(farObstacle, 4f, new float3(0f, 0f, 4f)));

            Assert.That(nearThenFar.Danger.N, Is.EqualTo(nearOnly.Danger.N).Within(1e-5f));
        }

        // --- helpers ---

        private Entity NewObstacle() => _world.EntityManager.CreateEntity();

        private Entity NewWith<T>(T data) where T : unmanaged, IComponentData
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponentData(e, data);
            return e;
        }

        private static DistanceHit Hit(Entity e, float distance, float3 position) =>
            new DistanceHit { Entity = e, Fraction = distance, Position = position };

        private static float MaxBin(ContextMap map)
        {
            var m = 0f;
            for (var k = 0; k < 8; k++) m = math.max(m, map[k]);
            return m;
        }

        private ObstacleDangerCollector NewCollector(
            Entity self, float surroundRadius = 5f, float visionDistance = 5f,
            float dangerWeight = 1f, Curve curve = Curve.Linear, bool skipWalls = false) =>
            new ObstacleDangerCollector(surroundRadius + visionDistance)
            {
                Self = self,
                MyPos = float3.zero,
                MyForward = new float3(0f, 0f, 1f),
                SurroundRadius = surroundRadius,
                VisionDistance = visionDistance,
                DangerWeight = dangerWeight,
                DangerCurve = curve,
                SkipWalls = skipWalls,
                WallLookup = _lookups.GetLookup<WallSection>(),
                WallReferenceLookup = _lookups.GetLookup<WallReference>(),
            };
    }
}
