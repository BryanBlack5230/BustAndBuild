#nullable enable

using System;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    public sealed class DaylightSpawningBridge : IDisposable
    {
        public DaylightSpawningBridge()
        {
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayStarted(in DayStartedEvent _) => SetSpawning(true);
        private void OnDayEnded(in DayEndedEvent _) => SetSpawning(false);

        private void SetSpawning(bool enabled)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            world.GetOrCreateSystemManaged<SpawningStateSystem>().SetDesiredState(enabled);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
            SetSpawning(false);
        }
    }
}
