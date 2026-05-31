using System;
using UnityEngine.InputSystem;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public class ScrollController : IDisposable
    {
        private readonly InputActions _inputActions;
        
        public ScrollController(InputManager inputManager)
        {
            _inputActions = inputManager.Actions;
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
            EventManager.Input.SceneChangeRequest?.Invoke(false);
        }

        private void OnScrollDown(InputAction.CallbackContext obj)
        {
            EventManager.Input.SceneChangeRequest?.Invoke(true);
        }
    }
}