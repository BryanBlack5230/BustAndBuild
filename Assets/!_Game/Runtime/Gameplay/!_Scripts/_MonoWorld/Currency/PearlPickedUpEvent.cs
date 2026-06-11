using Unity.Mathematics;

namespace BarkingBird.Runtime.Infrastructure
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

    public readonly struct PearlsChangedEvent : IEvent
    {
        public readonly int NewTotal;
        public readonly int Delta;

        public PearlsChangedEvent(int newTotal, int delta)
        {
            NewTotal = newTotal;
            Delta = delta;
        }
    }
}
