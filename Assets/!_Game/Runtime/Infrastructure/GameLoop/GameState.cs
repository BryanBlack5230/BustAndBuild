namespace BarkingBird.Runtime.Infrastructure.GameLoop
{
    /// <summary>
    /// Single source of truth for the game-loop lifecycle. Owned by <see cref="GameLoopManager"/>,
    /// broadcast via <see cref="GameStateChangedEvent"/>, and rendered by <see cref="GameManagerUIController"/>.
    /// </summary>
    public enum GameState
    {
        Unknown,
        Start,
        Finish,
        Pause,
        Resume
    }
}
