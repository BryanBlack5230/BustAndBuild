using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public class PowerHitController : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameUpdateListener, IDisposable
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
            _collisionFilter = CreateCollisionFilter();
        }
        
        public void OnStartGame() => Register();
        public void OnPause() => Unregister();
        public void OnResume() => Register();
        public void Dispose() => Unregister();

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
            Log.Battle.D("PowerHitController: OnCanceled");
            _isActive = false;
            _activeDuration = 0f;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            Log.Battle.D("PowerHitController: OnClick");
            _isActive = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_isActive) return;
            
            _activeDuration += deltaTime;
            if (_activeDuration >= _powerHitSettings.duration) Cancel();
            if (!_isActive) return;
            
            Log.Battle.D("PowerHitController: Active");
            //Not implemented yet
        }

        private CollisionFilter CreateCollisionFilter()
        {
            return new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = (uint)LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Grabbable),
                GroupIndex = 0,
            };
        }
    }
}