using System;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// Holds the player's currency totals for the active world. Plain POCO — knows nothing
    /// about files; persistence is owned by WorldSaveService. Amounts are stored in an int[]
    /// indexed by <see cref="CurrencyType"/>. Raises <see cref="CurrencyChangedEvent"/> on
    /// every change.
    /// </summary>
    public sealed class Wallet
    {
        private static readonly int Count = Enum.GetValues(typeof(CurrencyType)).Length;

        private readonly int[] _amounts = new int[Count];

        /// <summary>
        /// Overwrites all totals from a saved snapshot — called once by WorldSaveService when
        /// the world loads. A null or shorter array leaves the missing currencies at zero; a
        /// longer array (older save with extra trailing entries) is truncated to the current
        /// enum size. Silent: hydration is initialization, not a gameplay change, so it raises
        /// no CurrencyChangedEvent.
        /// </summary>
        public void Hydrate(int[] saved)
        {
            Array.Clear(_amounts, 0, Count);
            if (saved == null) return;
            int n = Math.Min(saved.Length, Count);
            for (int i = 0; i < n; i++)
                _amounts[i] = saved[i];
        }

        public int Get(CurrencyType type) => _amounts[(int)type];

        public void Add(CurrencyType type, int amount)
        {
            if (amount <= 0) return;
            int i = (int)type;
            _amounts[i] += amount;
            EventBus.Raise(new CurrencyChangedEvent(type, _amounts[i], amount));
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int i = (int)type;
            if (_amounts[i] < amount) return false;
            _amounts[i] -= amount;
            EventBus.Raise(new CurrencyChangedEvent(type, _amounts[i], -amount));
            return true;
        }

        /// <summary>Returns a fresh copy of all totals for persistence.</summary>
        public int[] Snapshot()
        {
            var copy = new int[Count];
            Array.Copy(_amounts, copy, Count);
            return copy;
        }
    }
}
