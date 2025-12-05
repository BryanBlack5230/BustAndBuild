using System;
using Game.Configs;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Feature.Input
{
    public class PowerHitController : IGamePauseListener, IGameResumeListener, IGameUpdateListener, IDisposable
    {
        private readonly InputActions _inputActions;
        private readonly PowerHitConfig _powerHitSettings;
        private readonly MousePositionProvider _mousePositionProvider;

        private CollisionFilter _collisionFilter;
        private EntityManager _entityManager;

        private bool _isActive;
        private float _activeDuration;

        public PowerHitController(InputManager inputManager, MousePositionProvider mousePositionProvider, ConfigContainer configContainer)
        {
            _mousePositionProvider = mousePositionProvider;
            _inputActions = inputManager.Actions;
            _powerHitSettings = configContainer.Battle.PowerHitConfig;
        }

        public void Initialize()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateCollisionFilter();
            Register();
        }

        private void Register()
        {
            _inputActions.Gameplay.PowerHit.Enable();
            
            _inputActions.Gameplay.PowerHit.performed += OnClick;
            _inputActions.Gameplay.PowerHit.canceled += OnCanceled;
        }

        private void Unregister()
        {
            _inputActions.Gameplay.PowerHit.Disable();
            
            _inputActions.Gameplay.PowerHit.performed -= OnClick;
            _inputActions.Gameplay.PowerHit.canceled -= OnCanceled;
        }

        private void OnCanceled(InputAction.CallbackContext obj)
        {
            if (!_isActive) return;
            Cancel();
        }

        private void Cancel()
        {
            Debug.Log("PowerHitController: OnCanceled");
            _isActive = false;
            _activeDuration = 0f;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            Debug.Log("PowerHitController: OnClick");
            _isActive = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isActive) return;
            
            _activeDuration += deltaTime;
            if (_activeDuration >= _powerHitSettings.duration) Cancel();
            if (!_isActive) return;
            
            Debug.Log("PowerHitController: Active");
            //Not implemented yet
        }

        private void CreateCollisionFilter()
        {
            _collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = (uint)LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Grabbable),
                GroupIndex = 0,
            };
        }

        public void OnPause()
        {
            Unregister();
        }

        public void OnResume()
        {
            Register();
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}