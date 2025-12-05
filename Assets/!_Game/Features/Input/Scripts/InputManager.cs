using System;

namespace Game.Feature.Input
{
    public class InputManager: IDisposable
    {
        public InputActions Actions { get; }

        public InputManager()
        {
            Actions = new InputActions();
            // _inputActions.UI.Enable();
            Actions.Gameplay.MousePosition.Enable();
        }

        public void Dispose()
        {
            Actions.Disable();
            Actions?.Dispose();
        }
    }
}