namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    /// <summary>
    /// Raised by <see cref="GameLoopManager"/> after every lifecycle transition, regardless of who
    /// triggered it (UI button, <c>GameLoopStateOverride</c>, or any future caller). Consumers such as
    /// <see cref="GameManagerUIController"/> react to the new <see cref="State"/> instead of being poked directly.
    /// </summary>
    public readonly struct GameStateChangedEvent : IEvent
    {
        public readonly GameState State;

        public GameStateChangedEvent(GameState state) => State = state;
    }
}
