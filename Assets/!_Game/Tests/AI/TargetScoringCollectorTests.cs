using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using BarkingBird.Runtime.Gameplay.AI;

namespace BarkingBird.Tests
{
    // TargetScoringCollector folds the broadphase point-distance hits into one best-score pick. These tests
    // drive AddHit directly with fabricated DistanceHits (DistanceHit.Distance == Fraction, the absolute
    // surface distance) + real ComponentLookups from a throwaway World — no physics query needed. They pin
    // the targeting DECISION (who wins), not the query plumbing (covered by the integration test).
    public class TargetScoringCollectorTests
    {
        private const float Radius = 20f;
        private World _world;
        private LookupSource _lookups;
        private NativeParallelHashMap<Entity, Entity> _snapshot;

        [SetUp]
        public void SetUp()
        {
            _world = new World("ScorerTest");
            _lookups = _world.GetOrCreateSystemManaged<LookupSource>();
            _snapshot = new NativeParallelHashMap<Entity, Entity>(8, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            if (_snapshot.IsCreated) _snapshot.Dispose();
            if (_world is { IsCreated: true }) _world.Dispose();
        }

        // The query returns the unit's own collider at distance 0 — it must never target itself.
        [Test]
        public void AddHit_SelfEntity_IgnoredAndNoTarget()
        {
            var self = NewUnit(Faction.Ally);
            var collector = NewCollector(self, Faction.Ally);

            var accepted = collector.AddHit(Hit(self, 0f, new float3(0f, 0f, 0f)));

            Assert.That(accepted, Is.False);
            Assert.That(collector.BestType, Is.EqualTo(TargetType.None));
        }

        [Test]
        public void AddHit_HostileUnit_SelectedAsUnit()
        {
            var self = NewUnit(Faction.Ally);
            var enemy = NewUnit(Faction.Enemy);
            var collector = NewCollector(self, Faction.Ally);

            var accepted = collector.AddHit(Hit(enemy, 3f, new float3(0f, 0f, 3f)));

            Assert.That(accepted, Is.True);
            Assert.That(collector.BestEntity, Is.EqualTo(enemy));
            Assert.That(collector.BestType, Is.EqualTo(TargetType.Unit));
        }

        // Same-faction units score on WeightAlly; zero ally weight must reject them.
        [Test]
        public void AddHit_AllyUnitWithZeroAllyWeight_Rejected()
        {
            var self = NewUnit(Faction.Ally);
            var friend = NewUnit(Faction.Ally);
            var collector = NewCollector(self, Faction.Ally, weightAlly: 0f);

            var accepted = collector.AddHit(Hit(friend, 3f, new float3(0f, 0f, 3f)));

            Assert.That(accepted, Is.False);
            Assert.That(collector.BestType, Is.EqualTo(TargetType.None));
        }

        [Test]
        public void AddHit_Wall_SelectedAsWall()
        {
            var self = NewUnit(Faction.Ally);
            var wall = NewWall();
            var collector = NewCollector(self, Faction.Ally);

            var accepted = collector.AddHit(Hit(wall, 4f, new float3(0f, 0f, 4f)));

            Assert.That(accepted, Is.True);
            Assert.That(collector.BestEntity, Is.EqualTo(wall));
            Assert.That(collector.BestType, Is.EqualTo(TargetType.Wall));
        }

        // Once the castle is breached, walls stop being valid targets (units pour through the gap instead).
        [Test]
        public void AddHit_WallWhenCastleBreached_Rejected()
        {
            var self = NewUnit(Faction.Ally);
            var wall = NewWall();
            var collector = NewCollector(self, Faction.Ally, castleBreached: true);

            var accepted = collector.AddHit(Hit(wall, 4f, new float3(0f, 0f, 4f)));

            Assert.That(accepted, Is.False);
            Assert.That(collector.BestType, Is.EqualTo(TargetType.None));
        }

        // Layers can deliver wall-child / arena / debris colliders that are neither Unit nor WallSection.
        [Test]
        public void AddHit_EntityThatIsNeitherUnitNorWall_Rejected()
        {
            var self = NewUnit(Faction.Ally);
            var debris = _world.EntityManager.CreateEntity(); // no Unit, no WallSection
            var collector = NewCollector(self, Faction.Ally);

            var accepted = collector.AddHit(Hit(debris, 3f, new float3(0f, 0f, 3f)));

            Assert.That(accepted, Is.False);
            Assert.That(collector.BestType, Is.EqualTo(TargetType.None));
        }

        // Surface distance drives the prefer-closer weighting: the nearer of two equal-weight hostiles wins.
        [Test]
        public void AddHit_CloserSurface_OutscoresFarther()
        {
            var self = NewUnit(Faction.Ally);
            var near = NewUnit(Faction.Enemy);
            var far = NewUnit(Faction.Enemy);
            var collector = NewCollector(self, Faction.Ally, losBonus: 0f);

            collector.AddHit(Hit(far, 10f, new float3(0f, 0f, 10f)));
            collector.AddHit(Hit(near, 2f, new float3(0f, 0f, 2f)));

            Assert.That(collector.BestEntity, Is.EqualTo(near));
        }

        // A hostile inside the view cone gets the line-of-sight bonus and beats an equidistant one behind.
        [Test]
        public void AddHit_WithinViewAngle_BeatsEquidistantBehind()
        {
            var self = NewUnit(Faction.Ally);
            var front = NewUnit(Faction.Enemy);
            var behind = NewUnit(Faction.Enemy);
            var collector = NewCollector(self, Faction.Ally, losBonus: 5f);

            collector.AddHit(Hit(behind, 5f, new float3(0f, 0f, -5f)));
            collector.AddHit(Hit(front, 5f, new float3(0f, 0f, 5f)));

            Assert.That(collector.BestEntity, Is.EqualTo(front));
        }

        // AggroBonus: a hostile already targeting me outscores an equidistant hostile that isn't.
        [Test]
        public void AddHit_HostileTargetingMe_GetsAggroBonus()
        {
            var self = NewUnit(Faction.Ally);
            var aggressor = NewUnit(Faction.Enemy);
            var bystander = NewUnit(Faction.Enemy);
            _snapshot.Add(aggressor, self); // aggressor's current target is me
            var collector = NewCollector(self, Faction.Ally, losBonus: 0f, aggroBonus: 7f);

            collector.AddHit(Hit(bystander, 5f, new float3(3f, 0f, 4f)));
            collector.AddHit(Hit(aggressor, 5f, new float3(0f, 0f, 5f)));

            Assert.That(collector.BestEntity, Is.EqualTo(aggressor));
        }

        // --- helpers ---

        private Entity NewUnit(Faction f)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponentData(e, new Unit { faction = f });
            return e;
        }

        private Entity NewWall()
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponentData(e, new WallSection());
            return e;
        }

        private static DistanceHit Hit(Entity e, float distance, float3 position) =>
            new DistanceHit { Entity = e, Fraction = distance, Position = position };

        private TargetScoringCollector NewCollector(
            Entity self, Faction myFaction,
            float weightEnemy = 10f, float weightAlly = 5f, float weightWall = 3f,
            float distanceWeight = 4f, float losBonus = 2f, float aggroBonus = 0f,
            bool castleBreached = false) =>
            new TargetScoringCollector(Radius)
            {
                Self = self,
                MyFaction = myFaction,
                MyPos = float3.zero,
                MyForward = new float3(0f, 0f, 1f),
                WeightEnemy = weightEnemy,
                WeightAlly = weightAlly,
                WeightWall = weightWall,
                DistanceWeight = distanceWeight,
                LineOfSightBonus = losBonus,
                AggroBonus = aggroBonus,
                ViewAngleCos = math.cos(math.radians(45f)), // 90° FOV
                DetectionRadiusSq = Radius * Radius,
                CastleIsBreached = castleBreached,
                UnitLookup = _lookups.GetLookup<Unit>(),
                WallLookup = _lookups.GetLookup<WallSection>(),
                TargetSnapshot = _snapshot,
            };
    }

    // EntityManager.GetComponentLookup<T> is internal to Unity.Entities, so the test assembly can't call it.
    // The supported way to obtain a ComponentLookup outside a job is via a system's SystemState — so this
    // do-nothing system exists only to hand the collector its read-only lookups.
    partial class LookupSource : SystemBase
    {
        protected override void OnUpdate() { }

        public ComponentLookup<T> GetLookup<T>() where T : unmanaged, IComponentData =>
            GetComponentLookup<T>(true);
    }
}
