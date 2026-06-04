using System;
using BarkingBird.Runtime.Gameplay.Input;

namespace BarkingBird.Runtime.Infrastructure
{
    public class InputManager: IDisposable
    {
        public InputActions Actions { get; }

        public InputManager()
        {
            Actions = new InputActions();
            Actions.UI.Enable();
            Actions.Gameplay.MousePosition.Enable();
        }

        public void Dispose()
        {
            Actions.Disable();
            Actions?.Dispose();
        }
    }
}