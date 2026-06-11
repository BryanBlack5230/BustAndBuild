using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// Raised by <see cref="Wallet"/> whenever any currency total changes. Subscribers
    /// filter by <see cref="Type"/>. Replaces the old per-currency PearlsChangedEvent.
    /// </summary>
    public readonly struct CurrencyChangedEvent : IEvent
    {
        public readonly CurrencyType Type;
        public readonly int NewTotal;
        public readonly int Delta;

        public CurrencyChangedEvent(CurrencyType type, int newTotal, int delta)
        {
            Type = type;
            NewTotal = newTotal;
            Delta = delta;
        }
    }
}
