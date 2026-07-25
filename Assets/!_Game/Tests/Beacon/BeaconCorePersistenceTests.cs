using NUnit.Framework;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Gameplay.GameWorld;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Save;

namespace BarkingBird.Tests
{
    // The Beacon Core free-countdown round-trips a world reload: WorldSaveService snapshots the World-scoped
    // BeaconCoreState into WorldSaveData on flush and re-hydrates a fresh holder on the next load, through the
    // same in-memory DummySaveSystem slot. This pins the save seam the holder approach depends on (the
    // controller writes the holder; the service persists it across the scope boundary).
    public class BeaconCorePersistenceTests
    {
        private DummySaveSystem _save;
        private ActiveSlot _slot;
        private WorldSaveService _service;

        [SetUp]
        public void SetUp()
        {
            _save = new DummySaveSystem();
            _slot = new ActiveSlot();
        }

        [TearDown]
        public void TearDown()
        {
            // EventBus is static and domain reload is off — always unsubscribe so a leaked handler can't fire
            // into a later test.
            _service?.Dispose();
            _service = null;
        }

        [Test]
        public void FreeCountdown_SurvivesReload()
        {
            // First session hydrates fresh, then the "controller" advances the shared holder.
            var state = new BeaconCoreState();
            _service = new WorldSaveService(_save, _slot, new Wallet(), state);
            state.Initialized = true;
            state.FreeCountdownRemaining = 123.5f;

            // Day end flushes the holder into the slot; tear down the session.
            EventBus.Raise(new DayEndedEvent());
            _service.Dispose();
            _service = null;

            // Second session against the same slot re-hydrates a fresh holder from the save.
            var reloaded = new BeaconCoreState();
            _service = new WorldSaveService(_save, _slot, new Wallet(), reloaded);

            Assert.That(reloaded.Initialized, Is.True);
            Assert.That(reloaded.FreeCountdownRemaining, Is.EqualTo(123.5f).Within(0.001f));
        }

        [Test]
        public void FreshWorld_HydratesUninitialized()
        {
            var state = new BeaconCoreState();
            _service = new WorldSaveService(_save, _slot, new Wallet(), state);

            // A brand-new slot leaves the holder uninitialized so the controller seeds the timer fresh.
            Assert.That(state.Initialized, Is.False);
            Assert.That(state.FreeCountdownRemaining, Is.EqualTo(0f));
        }
    }
}
