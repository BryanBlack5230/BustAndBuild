using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Input
{
    // Events
    public readonly struct ObjectGrabbedEvent : IEvent { }

    public readonly struct GroundGrabbedEvent : IEvent
    {
        public readonly bool ActuallyHolding;

        public GroundGrabbedEvent(bool actuallyHolding) => ActuallyHolding = actuallyHolding;
    }

    public readonly struct ReleaseEvent : IEvent { }
}
