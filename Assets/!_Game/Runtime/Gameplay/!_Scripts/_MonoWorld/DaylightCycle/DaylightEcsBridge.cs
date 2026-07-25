#nullable enable

using System;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    public sealed class DaylightEcsBridge : IDisposable, IWorldInitializable
    {
        private EntityManager _entityManager;
        private bool _initialized;

        public DaylightEcsBridge()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _initialized = true;
        }

        private void OnDayStarted(in DayStartedEvent _) => SetDayPhaseActive(true);
        private void OnDayEnded(in DayEndedEvent _) => SetDayPhaseActive(false);

        private void SetDayPhaseActive(bool isActive)
        {
            // Events can fire before the flow hands us the world (or during teardown) — guard both.
            if (!_initialized) return;

            var world = _entityManager.World;
            if (world == null || !world.IsCreated) return;

            world.GetOrCreateSystemManaged<SpawningStateSystem>().SetDesiredState(isActive);

            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<BattleCoordinator>());
            if (query.IsEmpty) return;

            var coordinator = query.GetSingleton<BattleCoordinator>();
            coordinator.IsDayPhaseActive = isActive;
            query.SetSingleton(coordinator);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
            SetDayPhaseActive(false);
        }
    }
}
