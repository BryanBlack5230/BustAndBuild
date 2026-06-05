#nullable enable

using System;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    public sealed class DaylightEcsBridge : IDisposable
    {
        public DaylightEcsBridge()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayStarted(in DayStartedEvent _) => SetDayPhaseActive(true);
        private void OnDayEnded(in DayEndedEvent _) => SetDayPhaseActive(false);

        private void SetDayPhaseActive(bool isActive)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            world.GetOrCreateSystemManaged<SpawningStateSystem>().SetDesiredState(isActive);

            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<BattleCoordinator>());
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
