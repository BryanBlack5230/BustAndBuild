using System;

using BarkingBird.Runtime.Gameplay.Beacon;
using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Gameplay.Daylight;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Save;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.GameWorld
{
    /// <summary>
    /// World-scoped persistence coordinator. It does NOT own the <see cref="Wallet"/> — the
    /// World scope owns that as standalone domain state. This service is injected with the
    /// wallet, loads the active slot on construction and hydrates the wallet from it, then
    /// marks itself dirty on every <see cref="CurrencyChangedEvent"/> and flushes the wallet
    /// back at safe points — day end and scene unload (Dispose, fired when Reflex disposes the
    /// World container).
    ///
    /// As more world state lands (day number, built structures, wall HP) this service grows
    /// to snapshot those into <see cref="WorldSaveData"/> too.
    ///
    /// TODO (needs real ISaveSystem): flush on OnApplicationPause/Quit. It is a no-op against
    /// the in-memory DummySaveSystem, so it lands with file-backed persistence.
    /// </summary>
    public sealed class WorldSaveService : IDisposable
    {
        private readonly ISaveSystem _saveSystem;
        private readonly WorldSaveData _data;
        private readonly Wallet _wallet;
        private readonly BeaconCoreState _beaconCoreState;

        private IDisposable _currencyChangedToken;
        private IDisposable _dayEndedToken;
        private bool _dirty;

        public WorldSaveService(ISaveSystem saveSystem, ActiveSlot activeSlot, Wallet wallet, BeaconCoreState beaconCoreState)
        {
            _saveSystem = saveSystem;
            _wallet = wallet;
            _beaconCoreState = beaconCoreState;
            _data = saveSystem.Load(activeSlot.WorldId);
            _wallet.Hydrate(_data.Currencies);

            _beaconCoreState.FreeCountdownRemaining = _data.BeaconCoreFreeCountdown;
            _beaconCoreState.Initialized = _data.BeaconCoreInitialized;

            _currencyChangedToken = EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _dayEndedToken = EventBus.Subscribe<DayEndedEvent>(OnDayEnded);

            Log.World.D($"WorldSaveService hydrated wallet from slot '{_data.WorldId}'");
        }

        private void OnCurrencyChanged(in CurrencyChangedEvent evt) => _dirty = true;

        private void OnDayEnded(in DayEndedEvent evt) { _dirty = true; Flush(); }

        private void Flush()
        {
            if (!_dirty) return;

            _data.Currencies = _wallet.Snapshot();
            _data.BeaconCoreFreeCountdown = _beaconCoreState.FreeCountdownRemaining;
            _data.BeaconCoreInitialized = _beaconCoreState.Initialized;
            _data.LastPlayedUtc = DateTime.UtcNow;
            _saveSystem.Save(_data);
            _dirty = false;

            Log.World.D($"WorldSaveService flushed slot '{_data.WorldId}'");
        }

        public void Dispose()
        {
            Flush();
            _currencyChangedToken?.Dispose();
            _dayEndedToken?.Dispose();
        }
    }
}
