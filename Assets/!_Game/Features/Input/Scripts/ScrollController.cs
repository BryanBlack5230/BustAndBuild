using System;
using Game.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Feature.Input
{
    public class ScrollController : IDisposable
    {
        private readonly InputActions _inputActions;
        
        public ScrollController(InputManager inputManager)
        {
            Debug.Log("Constructing Scroll Controller");
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
            Debug.Log("Scroll up called");
            EventManager.Input.SceneChangeRequest?.Invoke(true);
        }

        private void OnScrollDown(InputAction.CallbackContext obj)
        {
            Debug.Log("Scroll down called");
            EventManager.Input.SceneChangeRequest?.Invoke(false);
        }
    }
}