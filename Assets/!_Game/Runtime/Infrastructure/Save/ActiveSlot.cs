namespace BarkingBird.Runtime.Infrastructure.Save
{
    /// <summary>
    /// Records which world/slot the player picked. No world-select UI exists yet, so this
    /// defaults to a single hardcoded slot. <see cref="Select"/> is the seam the menu flow
    /// will call once it lands.
    /// </summary>
    public sealed class ActiveSlot
    {
        public const string DefaultWorldId = "world_0";

        public string WorldId { get; private set; } = DefaultWorldId;

        public void Select(string worldId) => WorldId = worldId;
    }
}
