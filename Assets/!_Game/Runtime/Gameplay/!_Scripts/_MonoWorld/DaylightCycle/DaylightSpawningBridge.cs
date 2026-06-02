#nullable enable

using System;
using Unity.Entities;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    public sealed class DaylightSpawningBridge : IDisposable
    {
        public DaylightSpawningBridge()
        {
            Log.Boot.D("DaylightSpawningBridge created");
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayStarted(in DayStartedEvent _) => SetSpawning(true);
        private void OnDayEnded(in DayEndedEvent _) => SetSpawning(false);

        private void SetSpawning(bool enabled)
        {
            Log.Battle.D($"SetSpawning: {enabled}");
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
