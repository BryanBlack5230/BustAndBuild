using Unity.Mathematics;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// Raised when the cursor collects a world pickup. Carries the resource type so the magnet
    /// controller can route it to the right counter and credit the right wallet slot. Replaces
    /// the old pearl-only PearlPickedUpEvent.
    /// </summary>
    public readonly struct PickupCollectedEvent : IEvent
    {
        public readonly CurrencyType Type;
        public readonly float3 Position;
        public readonly float Value;

        public PickupCollectedEvent(CurrencyType type, float3 position, float value)
        {
            Type = type;
            Position = position;
            Value = value;
        }
    }
}
