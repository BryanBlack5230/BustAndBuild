using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Runtime.Gameplay.Scenes
{
    public readonly struct ChangeSceneCommand : ICommand
    {
        public readonly bool SwitchUp;

        public ChangeSceneCommand(bool switchUp) => SwitchUp = switchUp;
    }
}
