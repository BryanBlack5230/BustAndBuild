using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Tests
{
    // The ECS side of the Beacon loop (ADR-0006). Two behaviours that matter and aren't witnessed by any pure
    // unit: (1) the Beacon's IsInvulnerable rising edge sends ForceFinishDayCommand exactly once — a Mono poll so
    // it pauses with the game; (2) DayEndedEvent re-drops the one persistent Core (re-enable + reposition + zero
    // velocity + restore the un-frozen mass + clear grab flags) and heals the Beacon (Health=Max, invuln + damage
    // buffer cleared). Plus the runtime PristineMass capture (baking the mass is unreliable).
    //
    // The bridge takes its EntityManager via Initialize(em) -> build a throwaway world per test and hand it in;
    // no process-global default-world swap. Dispose the bridge in TearDown: it subscribes to the static EventBus
    // and domain reload is off, so a leaked handler would fire into a later test.
    public class BeaconEcsBridgeTests
    {
        private const float BeaconMaxHealth = 6000f;

        private World _world;
        private BeaconEcsBridge _bridge;
        private GameObject _sceneDataGo;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BeaconEcsBridgeTest");
        }

        [TearDown]
        public void TearDown()
        {
            _bridge?.Dispose();
            _bridge = null;
            if (_world is { IsCreated: true }) _world.Dispose();
            if (_sceneDataGo != null) Object.DestroyImmediate(_sceneDataGo);
            _sceneDataGo = null;
        }

        // --- invulnerability rising edge ---

        [Test]
        public void OnUpdate_InvulnerabilityRisingEdge_SendsForceFinishOnce()
        {
            var em = _world.EntityManager;
            var beacon = CreateBeacon(em, invulnerable: false);

            var sent = 0;
            var dispatcher = new CommandDispatcher();
            using var sub = dispatcher.Register<ForceFinishDayCommand>(_ => sent++);
            _bridge = new BeaconEcsBridge(dispatcher, null);
            _bridge.Initialize(_world.EntityManager);

            _bridge.OnUpdate(Dt);                                          // not invuln -> nothing
            Assert.That(sent, Is.EqualTo(0));

            em.SetComponentEnabled<IsInvulnerable>(beacon, true);
            _bridge.OnUpdate(Dt);                                          // rising edge -> send
            _bridge.OnUpdate(Dt);                                          // held high -> no re-send

            Assert.That(sent, Is.EqualTo(1));
        }

        [Test]
        public void OnUpdate_NeverInvulnerable_SendsNothing()
        {
            var em = _world.EntityManager;
            CreateBeacon(em, invulnerable: false);

            var sent = 0;
            var dispatcher = new CommandDispatcher();
            using var sub = dispatcher.Register<ForceFinishDayCommand>(_ => sent++);
            _bridge = new BeaconEcsBridge(dispatcher, null);
            _bridge.Initialize(_world.EntityManager);

            _bridge.OnUpdate(Dt);
            _bridge.OnUpdate(Dt);

            Assert.That(sent, Is.EqualTo(0));
        }

        // --- DayEnded: drop the Core ---

        [Test]
        public void DayEnded_DropsCore_ReEnablesRepositionsZeroesVelocityRestoresMassResetsFlags()
        {
            var em = _world.EntityManager;
            var socket = new float3(5f, 0f, 0f);
            var core = CreateCore(em, hidden: true);

            _bridge = new BeaconEcsBridge(new CommandDispatcher(), CreateSceneData(socket));
            _bridge.Initialize(_world.EntityManager);
            EventBus.Raise(new DayEndedEvent());

            Assert.That(em.HasComponent<Disabled>(core), Is.False, "Core not re-enabled");
            Assert.That(em.GetComponentData<LocalTransform>(core).Position,
                Is.EqualTo(socket).Using(Float3Within(0.001f)), "Core not moved to socket");
            Assert.That(math.length(em.GetComponentData<PhysicsVelocity>(core).Linear),
                Is.EqualTo(0f).Within(0.001f), "Core velocity not zeroed");
            Assert.That(em.GetComponentData<PhysicsMass>(core).InverseMass,
                Is.EqualTo(PristineInverseMass).Within(1e-4f), "Core mass not restored to pristine");
            Assert.That(em.IsComponentEnabled<Grabbed>(core), Is.False, "Grabbed not cleared");
            Assert.That(em.IsComponentEnabled<InAir>(core), Is.False, "InAir not cleared");
        }

        // --- DayEnded: heal the Beacon ---

        [Test]
        public void DayEnded_HealsBeacon_FullHealthClearsInvulnAndDamageBuffer()
        {
            var em = _world.EntityManager;
            var beacon = CreateBeacon(em, invulnerable: true, health: 1f);
            em.GetBuffer<DamageBufferElement>(beacon).Add(new DamageBufferElement { Value = 250f });
            CreateCore(em, hidden: false); // present so the drop half runs clean (no "no Core" warning)

            _bridge = new BeaconEcsBridge(new CommandDispatcher(), CreateSceneData(float3.zero));
            _bridge.Initialize(_world.EntityManager);
            EventBus.Raise(new DayEndedEvent());

            Assert.That(em.GetComponentData<Health>(beacon).Value, Is.EqualTo(BeaconMaxHealth).Within(0.001f));
            Assert.That(em.IsComponentEnabled<IsInvulnerable>(beacon), Is.False, "invuln not cleared");
            Assert.That(em.GetBuffer<DamageBufferElement>(beacon).Length, Is.EqualTo(0), "damage buffer not cleared");
        }

        // --- pristine mass capture ---

        [Test]
        public void OnUpdate_RestingCore_CapturesPristineMassOnce()
        {
            var em = _world.EntityManager;
            var core = em.CreateEntity();
            em.AddComponentData(core, new BeaconCore());
            em.AddComponentData(core, new PristineMass());                       // Captured = false
            em.AddComponentData(core, new PhysicsMass { InverseMass = PristineInverseMass });

            _bridge = new BeaconEcsBridge(new CommandDispatcher(), null);
            _bridge.Initialize(_world.EntityManager);
            _bridge.OnUpdate(Dt);

            var captured = em.GetComponentData<PristineMass>(core);
            Assert.That(captured.Captured, Is.True);
            Assert.That(captured.Value.InverseMass, Is.EqualTo(PristineInverseMass).Within(1e-4f));

            // Capture is once: a later (different) mass must not overwrite the stored pristine value.
            em.SetComponentData(core, new PhysicsMass { InverseMass = 0.9f });
            _bridge.OnUpdate(Dt);
            Assert.That(em.GetComponentData<PristineMass>(core).Value.InverseMass,
                Is.EqualTo(PristineInverseMass).Within(1e-4f));
        }

        [Test]
        public void OnUpdate_GrabFrozenCore_DoesNotCapturePristineMass()
        {
            var em = _world.EntityManager;
            var core = em.CreateEntity();
            em.AddComponentData(core, new BeaconCore());
            em.AddComponentData(core, new PristineMass());
            em.AddComponentData(core, new PhysicsMass { InverseMass = 0f }); // frozen by a grab -> not pristine

            _bridge = new BeaconEcsBridge(new CommandDispatcher(), null);
            _bridge.Initialize(_world.EntityManager);
            _bridge.OnUpdate(Dt);

            Assert.That(em.GetComponentData<PristineMass>(core).Captured, Is.False);
        }

        // --- helpers ---

        private const float Dt = 1f / 60f;
        private const float PristineInverseMass = 0.5f;

        private static Entity CreateBeacon(EntityManager em, bool invulnerable, float health = BeaconMaxHealth)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new BeaconTag());
            em.AddComponentData(e, new Health { Value = health, Max = BeaconMaxHealth });
            em.AddBuffer<DamageBufferElement>(e);
            em.AddComponent<IsInvulnerable>(e);
            em.SetComponentEnabled<IsInvulnerable>(e, invulnerable);
            return e;
        }

        private static Entity CreateCore(EntityManager em, bool hidden)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new BeaconCore());
            em.AddComponentData(e, LocalTransform.FromPosition(float3.zero));
            em.AddComponentData(e, new PhysicsVelocity { Linear = new float3(7f, 7f, 7f) });
            em.AddComponentData(e, new PhysicsMass { InverseMass = 0f }); // grab-frozen at insert time
            em.AddComponentData(e, new PristineMass { Value = new PhysicsMass { InverseMass = PristineInverseMass }, Captured = true });
            em.AddComponentData(e, new Grabbed());
            em.AddComponentData(e, new InAir());
            em.SetComponentEnabled<Grabbed>(e, true);
            em.SetComponentEnabled<InAir>(e, true);
            if (hidden) em.SetEnabled(e, false); // the Core is Disabled-tagged while the Day runs
            return e;
        }

        private BattleSceneData CreateSceneData(float3 socketPosition)
        {
            _sceneDataGo = new GameObject("BattleSceneData");
            var data = _sceneDataGo.AddComponent<BattleSceneData>();
            var socketGo = new GameObject("Socket");
            socketGo.transform.SetParent(_sceneDataGo.transform);
            socketGo.transform.position = socketPosition;
            data.beaconCoreSocket = socketGo.transform;
            return data;
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
