using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.AI;

namespace BarkingBird.Tests
{
    // The reported bug (DistanceCalculationsRefactorTask Step 1): the brain measured center-to-center, so a
    // unit pressed against a big structure's surface was still "far" from its pivot and never entered attack
    // range. Fix: structures carry a cached world AABB (TargetBounds) and the brain measures to the nearest
    // SURFACE point. Units (no TargetBounds) keep center distance. These one-tick BattleBrainSystem tests pin
    // all three: the range gate, the move destination, and the units-unchanged path.
    public class BattleBrainSurfaceRangeTests
    {
        private const float AttackRange = 3f;
        private World _world;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BrainTest");
            CreateSingletons();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world is { IsCreated: true }) _world.Dispose();
        }

        // Unit at origin; structure centre is 20m away but its near face is 2m away (< AttackRange) → in range.
        // Center distance (the old bug) would be 20m → Moving. Surface distance → Attacking.
        [Test]
        public void Brain_UnitAtStructureSurface_EntersAttackRange()
        {
            var unit = CreateUnit(Faction.Ally, float3.zero);
            // AABB near face at z=2, centre at z=20.
            var target = CreateStructureTarget(center: new float3(0f, 0f, 20f),
                bounds: new Aabb { Min = new float3(-5f, -5f, 2f), Max = new float3(5f, 5f, 38f) });
            PointAt(unit, target, TargetType.Wall);

            Tick();

            Assert.That(_world.EntityManager.GetComponentData<ActionState>(unit).Value, Is.EqualTo(ActionType.Attacking));
            Assert.That(_world.EntityManager.GetComponentData<BattleBrain>(unit).CanAttack, Is.True);
        }

        // Same geometry, but the target is a UNIT (no TargetBounds) → center distance (20m) stays out of range,
        // so behaviour is unchanged: Moving, not Attacking. Guards against the fix leaking into unit-vs-unit.
        [Test]
        public void Brain_UnitTargetBeyondCenterRange_StaysMoving()
        {
            var unit = CreateUnit(Faction.Ally, float3.zero);
            var target = CreateUnitTarget(center: new float3(0f, 0f, 20f));
            PointAt(unit, target, TargetType.Unit);

            Tick();

            Assert.That(_world.EntityManager.GetComponentData<ActionState>(unit).Value, Is.EqualTo(ActionType.Moving));
            Assert.That(_world.EntityManager.GetComponentData<BattleBrain>(unit).CanAttack, Is.False);
        }

        // Out-of-range structure: the move destination must be the SURFACE point (z=10), not the centre (z=24).
        [Test]
        public void Brain_StructureOutOfRange_MovesToSurfaceNotCentre()
        {
            var unit = CreateUnit(Faction.Ally, float3.zero);
            var target = CreateStructureTarget(center: new float3(0f, 0f, 24f),
                bounds: new Aabb { Min = new float3(-5f, -5f, 10f), Max = new float3(5f, 5f, 38f) });
            PointAt(unit, target, TargetType.Wall);

            Tick();

            var dest = _world.EntityManager.GetComponentData<FinalDestination>(unit).Value;
            Assert.That(_world.EntityManager.GetComponentData<ActionState>(unit).Value, Is.EqualTo(ActionType.Moving));
            Assert.That(dest.z, Is.EqualTo(10f).Within(0.01f), "should head for the surface, not the pivot");
        }

        // --- arrange helpers ---

        private void CreateSingletons()
        {
            var em = _world.EntityManager;
            var config = em.CreateEntity();
            em.AddComponentData(config, new BattleCoordinator { IsDayPhaseActive = true, IsBattleActive = true });
            em.AddComponentData(config, new FactionBases { IsInitialized = true });
        }

        private Entity CreateUnit(Faction faction, float3 pos)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new Unit { faction = faction });
            em.AddComponentData(e, new BattleBrain());
            em.AddComponentData(e, new ActionState());
            em.AddComponentData(e, new Destination());
            em.AddComponentData(e, new FinalDestination());
            em.AddComponentData(e, new Target());
            em.AddComponentData(e, new EmotionalState { Value = Emotion.Normal });
            em.AddComponentData(e, new AttackData { AttackRange = AttackRange });
            em.AddComponentData(e, new LocalToWorld { Value = float4x4.Translate(pos) });

            em.AddComponent<SteeringEnabled>(e); // present (WithPresent gate)

            // Enableable gates the brain reads via lookups — present but DISABLED so the unit can act/attack.
            em.AddComponent<UnableToAct>(e);
            em.SetComponentEnabled<UnableToAct>(e, false);
            em.AddComponent<AttackCooldownExpirationTimestamp>(e);
            em.SetComponentEnabled<AttackCooldownExpirationTimestamp>(e, false);
            return e;
        }

        private Entity CreateStructureTarget(float3 center, Aabb bounds)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new LocalToWorld { Value = float4x4.Translate(center) });
            em.AddComponentData(e, new TargetBounds { World = bounds });
            return e;
        }

        private Entity CreateUnitTarget(float3 center)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new LocalToWorld { Value = float4x4.Translate(center) });
            return e;
        }

        private void PointAt(Entity unit, Entity target, TargetType type) =>
            _world.EntityManager.SetComponentData(unit, new Target { TargetEntity = target, Type = type });

        private void Tick()
        {
            _world.GetOrCreateSystem<BattleBrainSystem>().Update(_world.Unmanaged);
            _world.EntityManager.CompleteAllTrackedJobs();
        }
    }
}
