using System;
using Game.Core.Events;
using UnityEngine.InputSystem;

namespace Game.Feature.Input
{
    public class ScrollController : IDisposable
    {
        private readonly InputActions _inputActions;
        
        public ScrollController(InputManager inputManager)
        {
            _inputActions = inputManager.Actions;
            Register();
        }
        
        private void Register()
        {
            _inputActions.Gameplay.ScrollUp.performed += OnScrollUp;
            _inputActions.Gameplay.ScrollDown.performed += OnScrollDown;
        }

        public void Dispose()
        {
            _inputActions.Gameplay.ScrollUp.performed -= OnScrollUp;
            _inputActions.Gameplay.ScrollDown.performed -= OnScrollDown;
        }

        private void OnScrollUp(InputAction.CallbackContext obj)
        {
            EventManager.Input.SceneChangeRequest?.Invoke(true);
        }

        private void OnScrollDown(InputAction.CallbackContext obj)
        {
            EventManager.Input.SceneChangeRequest?.Invoke(false);
        }
    }
}