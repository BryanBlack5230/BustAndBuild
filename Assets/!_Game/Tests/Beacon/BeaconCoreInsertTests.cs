using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Placement;
using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Tests
{
    // The insert decision (ADR-0006/0007) end to end: a Core settled in the socket is claimed by the
    // PlacementController, which invokes the BeaconCoreController's place handler. That handler is the paid action
    // — afford the cost -> hide the Core + StartDayCommand; can't afford -> no-op so the Core lands to be
    // re-grabbed; free window -> start the Day without spending. Driven through the real PlacementController seam
    // rather than calling the private handler, so it also pins the controllers' wiring.
    //
    // Integration-level (the placement scan needs a live world): both controllers take their EntityManager via
    // Initialize(em), so each test builds a throwaway World and hands it in — no process-global default-world swap.
    public class BeaconCoreInsertTests
    {
        private World _world;
        private PlacementController _placement;
        private BeaconCoreController _controller;
        private GameObject _sceneDataGo;
        private BeaconCoreConfigSO _config;

        private int _dayStarted;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BeaconCoreInsertTest");

            _dayStarted = 0;
            _config = ScriptableObject.CreateInstance<BeaconCoreConfigSO>(); // defaults: cost 100, free 300s, thr 3, radius 2
            _placement = new PlacementController();
            _placement.Initialize(_world.EntityManager);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _controller = null;
            _placement?.Dispose();
            _placement = null;
            if (_world is { IsCreated: true }) _world.Dispose();
            if (_sceneDataGo != null) Object.DestroyImmediate(_sceneDataGo);
            _sceneDataGo = null;
            if (_config != null) Object.DestroyImmediate(_config);
            _config = null;
        }

        [Test]
        public void Insert_WithEnoughPearls_SpendsCostHidesCoreAndStartsDay()
        {
            var wallet = new Wallet();
            wallet.Add(CurrencyType.Pearls, 250);
            var core = ArrangeInsert(wallet, new BeaconCoreState()); // fresh state -> timer 300s -> cost 100

            _placement.OnUpdate(Dt);

            Assert.That(wallet.Get(CurrencyType.Pearls), Is.EqualTo(150), "cost not spent");
            Assert.That(_world.EntityManager.HasComponent<Disabled>(core), Is.True, "Core not hidden");
            Assert.That(_dayStarted, Is.EqualTo(1), "Day not started");
        }

        [Test]
        public void Insert_CannotAffordCost_NoOpsAndCoreRemains()
        {
            var wallet = new Wallet(); // empty -> can't pay the 100 cost
            var core = ArrangeInsert(wallet, new BeaconCoreState());

            _placement.OnUpdate(Dt);

            Assert.That(wallet.Get(CurrencyType.Pearls), Is.EqualTo(0), "wallet touched");
            Assert.That(_world.EntityManager.HasComponent<Disabled>(core), Is.False, "Core hidden despite no pay");
            Assert.That(_dayStarted, Is.EqualTo(0), "Day started despite no pay");
        }

        [Test]
        public void Insert_FreeWindow_StartsDayWithoutSpending()
        {
            var wallet = new Wallet(); // empty, but the free timer has elapsed -> cost 0
            var state = new BeaconCoreState { Initialized = true, FreeCountdownRemaining = 0f };
            var core = ArrangeInsert(wallet, state);

            _placement.OnUpdate(Dt);

            Assert.That(_world.EntityManager.HasComponent<Disabled>(core), Is.True, "Core not hidden");
            Assert.That(_dayStarted, Is.EqualTo(1), "Day not started on the free insert");
        }

        // --- helpers ---

        private const float Dt = 1f / 60f;

        // Wires the controller (registers the socket place) and spawns a Core settled in it. Returns the Core.
        private Entity ArrangeInsert(Wallet wallet, BeaconCoreState state)
        {
            var dispatcher = new CommandDispatcher();
            dispatcher.Register<StartDayCommand>(_ => _dayStarted++);

            _controller = new BeaconCoreController(_config, wallet, dispatcher, state, _placement, CreateSceneData());
            _controller.Initialize(_world.EntityManager);

            var em = _world.EntityManager;
            var core = em.CreateEntity();
            em.AddComponentData(core, new BeaconCore());
            em.AddComponentData(core, new Claimable());
            em.AddComponentData(core, new InAir());                                 // enabled = still flying
            em.AddComponentData(core, new PhysicsVelocity { Linear = float3.zero }); // slow -> settled
            em.AddComponentData(core, LocalTransform.FromPosition(float3.zero));     // at the socket
            return core;
        }

        private BattleSceneData CreateSceneData()
        {
            _sceneDataGo = new GameObject("BattleSceneData");
            var data = _sceneDataGo.AddComponent<BattleSceneData>();
            var socketGo = new GameObject("Socket");
            socketGo.transform.SetParent(_sceneDataGo.transform);
            socketGo.transform.position = Vector3.zero;
            data.beaconCoreSocket = socketGo.transform;
            return data;
        }
    }
}
