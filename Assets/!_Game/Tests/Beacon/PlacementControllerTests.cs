using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using BarkingBird.Runtime.Gameplay.Placement;

namespace BarkingBird.Tests
{
    // The settle-check's behavioural half (ADR-0007): PlacementController scans Claimable+InAir entities and
    // claims one into a registered AssignablePlace when it settles (slow + in radius). The load-bearing rule is
    // the per-settle debounce — a Core that can't afford the insert lingers InAir near the socket, and the
    // handler must NOT re-run every frame; it fires once, then again only after the entity leaves the window.
    //
    // Integration-level (the EntityQuery needs a live world): the controller takes its EntityManager via
    // Initialize(em), so each test builds a throwaway World and hands it in — no process-global default-world swap.
    public class PlacementControllerTests
    {
        private static readonly float3 Socket = float3.zero;
        private const float Radius = 2f;
        private const float VelThreshold = 3f;

        private World _world;
        private PlacementController _controller;

        private int _claimCount;
        private Entity _lastClaimed;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PlacementControllerTest");

            _claimCount = 0;
            _lastClaimed = Entity.Null;

            _controller = new PlacementController();
            _controller.Initialize(_world.EntityManager);
            _controller.Register(new AssignablePlace(Socket, Radius, VelThreshold, OnClaimed));
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _controller = null;
            if (_world is { IsCreated: true }) _world.Dispose();
        }

        private void OnClaimed(Entity e)
        {
            _claimCount++;
            _lastClaimed = e;
        }

        // Tracer: a Claimable resting in the socket window is claimed exactly once on the first tick.
        [Test]
        public void OnUpdate_SettledClaimable_FiresHandlerOnce()
        {
            var e = CreateClaimable(Socket, float3.zero);

            Tick();

            Assert.That(_claimCount, Is.EqualTo(1));
            Assert.That(_lastClaimed, Is.EqualTo(e));
        }

        // Debounce holds: an entity that stays in the window does not re-fire on subsequent ticks.
        [Test]
        public void OnUpdate_StaysSettledAcrossTicks_FiresOnce()
        {
            CreateClaimable(Socket, float3.zero);

            Tick();
            Tick();
            Tick();

            Assert.That(_claimCount, Is.EqualTo(1));
        }

        // Debounce resets when the entity leaves via speed (still InAir, still in radius, but too fast), then
        // settles again — the can't-afford "land and re-grab" retry path.
        [Test]
        public void OnUpdate_ReentersAfterSpeedingUp_FiresAgain()
        {
            var e = CreateClaimable(Socket, float3.zero);

            Tick();                                                  // claimed (1)
            SetVelocity(e, new float3(VelThreshold + 5f, 0f, 0f));   // too fast -> out of window
            Tick();                                                  // still 1, debounce cleared
            SetVelocity(e, float3.zero);                             // settled again
            Tick();                                                  // re-claimed (2)

            Assert.That(_claimCount, Is.EqualTo(2));
        }

        // Debounce resets when the entity drifts out of the radius, then returns.
        [Test]
        public void OnUpdate_ReentersAfterLeavingRadius_FiresAgain()
        {
            var e = CreateClaimable(Socket, float3.zero);

            Tick();                                          // claimed (1)
            SetPosition(e, new float3(Radius + 5f, 0f, 0f)); // out of radius
            Tick();                                          // still 1, debounce cleared
            SetPosition(e, Socket);                          // back in radius
            Tick();                                          // re-claimed (2)

            Assert.That(_claimCount, Is.EqualTo(2));
        }

        // Debounce resets when InAir is disabled (the entity drops out of the query entirely — it landed/was
        // re-grabbed), then re-enabled.
        [Test]
        public void OnUpdate_ReentersAfterInAirDisabled_FiresAgain()
        {
            var e = CreateClaimable(Socket, float3.zero);

            Tick();                                              // claimed (1)
            _world.EntityManager.SetComponentEnabled<InAir>(e, false);
            Tick();                                              // not in query, debounce cleared
            _world.EntityManager.SetComponentEnabled<InAir>(e, true);
            Tick();                                              // re-claimed (2)

            Assert.That(_claimCount, Is.EqualTo(2));
        }

        // A Claimable that is InAir but moving too fast is never claimed (predicate gate wired to live velocity).
        [Test]
        public void OnUpdate_ClaimableTooFast_DoesNotFire()
        {
            CreateClaimable(Socket, new float3(VelThreshold + 5f, 0f, 0f));

            Tick();

            Assert.That(_claimCount, Is.EqualTo(0));
        }

        // --- helpers ---

        private Entity CreateClaimable(float3 position, float3 velocity)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new Claimable());
            em.AddComponentData(e, new InAir());                 // AddComponent defaults to ENABLED = "still flying"
            em.AddComponentData(e, new PhysicsVelocity { Linear = velocity });
            em.AddComponentData(e, LocalTransform.FromPosition(position));
            return e;
        }

        private void SetVelocity(Entity e, float3 v) =>
            _world.EntityManager.SetComponentData(e, new PhysicsVelocity { Linear = v });

        private void SetPosition(Entity e, float3 p) =>
            _world.EntityManager.SetComponentData(e, LocalTransform.FromPosition(p));

        private void Tick() => _controller.OnUpdate(1f / 60f);
    }
}
