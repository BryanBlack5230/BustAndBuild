using System;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public sealed class WorldCurrency
    {
        public int Pearls { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Pearls += amount;
            EventBus.Raise(new PearlsChangedEvent(Pearls, amount));
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Pearls < amount) return false;
            Pearls -= amount;
            EventBus.Raise(new PearlsChangedEvent(Pearls, -amount));
            return true;
        }
    }
}
