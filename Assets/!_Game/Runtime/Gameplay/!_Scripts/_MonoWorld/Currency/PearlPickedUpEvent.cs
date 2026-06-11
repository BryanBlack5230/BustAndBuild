using Unity.Mathematics;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public readonly struct PearlPickedUpEvent : IEvent
    {
        public readonly float3 Position;
        public readonly float Value;

        public PearlPickedUpEvent(float3 position, float value)
        {
            Position = position;
            Value = value;
        }
    }
}
