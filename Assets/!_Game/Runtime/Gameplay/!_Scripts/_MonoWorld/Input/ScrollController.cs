using System;
using UnityEngine.InputSystem;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public class ScrollController : IDisposable
    {
        private readonly InputActions _inputActions;
        private readonly CommandDispatcher _dispatcher;

        public ScrollController(InputManager inputManager, CommandDispatcher dispatcher)
        {
            _inputActions = inputManager.Actions;
            _dispatcher = dispatcher;
        }

        public void Initialize()
        {
            Register();
        }

        private void Register()
        {
            _inputActions.Gameplay.ScrollUp.Enable();
            _inputActions.Gameplay.ScrollDown.Enable();

            _inputActions.Gameplay.ScrollUp.performed += OnScrollUp;
            _inputActions.Gameplay.ScrollDown.performed += OnScrollDown;
        }

        public void Dispose()
        {
            _inputActions.Gameplay.ScrollDown.Disable();
            _inputActions.Gameplay.ScrollUp.Disable();

            _inputActions.Gameplay.ScrollUp.performed -= OnScrollUp;
            _inputActions.Gameplay.ScrollDown.performed -= OnScrollDown;
        }

        private void OnScrollUp(InputAction.CallbackContext obj)
        {
            _dispatcher.Send(new ChangeSceneCommand(false));
        }

        private void OnScrollDown(InputAction.CallbackContext obj)
        {
            _dispatcher.Send(new ChangeSceneCommand(true));
        }
    }
}
