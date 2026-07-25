#nullable enable

using System;
using Unity.Entities;
using Unity.Mathematics;

using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.Placement;
using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Beacon
{
    /// <summary>
    /// Owns the player-facing side of the Beacon Core insert loop (ADR-0006 / ADR-0007): the pause-aware
    /// free-countdown timer, the current insert cost, and the Beacon socket registered as an
    /// <see cref="AssignablePlace"/>. When a settled Core is claimed there it tries to pay the cost; on success
    /// it hides the Core and starts the Day, otherwise it no-ops and the Core lands to be re-grabbed.
    ///
    /// Battle-scoped, but reads <c>Wallet</c> + <see cref="BeaconCoreState"/> from the ancestor World scope. The
    /// timer is mirrored into <see cref="BeaconCoreState"/> each frame so the World-scoped save service can
    /// snapshot it across the scope boundary. The matching ECS side (poll/drop/heal) lives in
    /// <see cref="BeaconEcsBridge"/>.
    /// </summary>
    public sealed class BeaconCoreController : IGameUpdateListener, IWorldInitializable, IDisposable
    {
        private readonly BeaconCoreConfigSO _config;
        private readonly Wallet _wallet;
        private readonly CommandDispatcher _dispatcher;
        private readonly BeaconCoreState _state;
        private readonly PlacementController _placement;
        private EntityManager _entityManager;

        private readonly AssignablePlace? _socketPlace;
        private readonly IDisposable _dayEndedToken;

        private float _freeCountdownRemaining;

        public BeaconCoreController(BeaconCoreConfigSO config, Wallet wallet, CommandDispatcher dispatcher,
            BeaconCoreState state, PlacementController placement, BattleSceneData battleSceneData)
        {
            _config = config;
            _wallet = wallet;
            _dispatcher = dispatcher;
            _state = state;
            _placement = placement;

            // Brand-new world: seed the timer as if the Core had just dropped. Otherwise resume the
            // (possibly persisted, possibly in-session) remaining from the shared World-scoped state.
            _freeCountdownRemaining = _state.Initialized ? _state.FreeCountdownRemaining : _config.FreeAfterSeconds;
            _state.Initialized = true;
            _state.FreeCountdownRemaining = _freeCountdownRemaining;

            _dayEndedToken = EventBus.Subscribe<DayEndedEvent>(OnDayEnded);

            var socket = battleSceneData != null ? battleSceneData.beaconCoreSocket : null;
            if (socket == null)
            {
                Log.Battle.W("[BeaconCoreController] BattleSceneData.beaconCoreSocket is unassigned; " +
                             "Core insertion is disabled for this scene.");
                return;
            }

            _socketPlace = new AssignablePlace(socket.position, _config.PlaceRadius, _config.InsertThreshold, OnCoreClaimed);
            _placement.Register(_socketPlace);
        }

        public void Initialize(EntityManager em) => _entityManager = em;

        /// <summary>Pearls required to insert right now: the configured cost until the free timer elapses, else 0.</summary>
        public int CurrentCost => ComputeCost(_freeCountdownRemaining, _config.InsertCost);

        /// <summary>Seconds left until inserting is free.</summary>
        public float FreeCountdownRemaining => _freeCountdownRemaining;

        public bool IsFree => _freeCountdownRemaining <= 0f;

        /// <summary>Pure cost rule (testable without a live world): full cost until the drop timer elapses, then free.</summary>
        public static int ComputeCost(float freeCountdownRemaining, int insertCost)
            => freeCountdownRemaining > 0f ? insertCost : 0;

        public void OnUpdate(float deltaTime)
        {
            if (_freeCountdownRemaining > 0f)
                _freeCountdownRemaining = math.max(0f, _freeCountdownRemaining - deltaTime);

            _state.FreeCountdownRemaining = _freeCountdownRemaining;
        }

        private void OnCoreClaimed(Entity core)
        {
            var cost = CurrentCost;
            var afforded = cost <= 0 || _wallet.TrySpend(CurrencyType.Pearls, cost);
            if (!afforded)
            {
                Log.Battle.D($"[BeaconCoreController] Insert declined — need {cost} Pearls; Core lands to retry.");
                return;
            }

            if (_entityManager.Exists(core))
                _entityManager.SetEnabled(core, false); // hide the one persistent Core; BeaconEcsBridge re-drops it on day-end

            _dispatcher.Send(new StartDayCommand());
            Log.Battle.D($"[BeaconCoreController] Core inserted (paid {cost} Pearls); Day started.");
        }

        private void OnDayEnded(in DayEndedEvent _)
        {
            // The Core was just dropped back at the socket (BeaconEcsBridge) — restart the free timer.
            _freeCountdownRemaining = _config.FreeAfterSeconds;
            _state.FreeCountdownRemaining = _freeCountdownRemaining;
        }

        public void Dispose()
        {
            _dayEndedToken.Dispose();
            if (_socketPlace != null) _placement.Unregister(_socketPlace);
        }
    }
}
