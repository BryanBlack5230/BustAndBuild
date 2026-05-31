using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

using BarkingBird.Runtime.Gameplay.Input.GrabAndThrow;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public class InteractController : IGameStartListener, IGamePauseListener, IGameResumeListener, IDisposable
    {
        private readonly InputActions _inputActions;
        private readonly GrabbingInteractor _grabbingInteractor;
        private readonly MousePositionProvider _mousePositionProvider;
        
        private EntityManager _entityManager;
        private CollisionFilter _collisionFilter;

        public InteractController(InputManager inputManager, GrabbingInteractor grabbingInteractor, MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
            _grabbingInteractor = grabbingInteractor;
            _inputActions = inputManager.Actions;
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
            _inputActions.Gameplay.Interact.Enable();
            _inputActions.Gameplay.Interact.performed += OnClick;
            _inputActions.Gameplay.Interact.canceled += OnCanceled;
        }

        private void Unregister()
        {
            _inputActions.Gameplay.Interact.Disable();
            _inputActions.Gameplay.Interact.performed -= OnClick;
            _inputActions.Gameplay.Interact.canceled -= OnCanceled;
        }

        private void OnCanceled(InputAction.CallbackContext obj)
        {
            EventManager.Input.Release?.Invoke();
            _grabbingInteractor.Release();
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            var cameraRay = _mousePositionProvider.screenPointToRay;
            // Debug.DrawRay(cameraRay.GetPoint(0f), cameraRay.GetPoint(9999f), Color.red);
        
            var entityQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
            var collisionWorld = entityQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var raycastInput = new RaycastInput
            { // if not hitting, try 0f and 99999f for start and end
                Start = cameraRay.GetPoint(9f),
                End = cameraRay.GetPoint(40f),
                Filter = _collisionFilter
            };

            if (collisionWorld.CastRay(raycastInput, out var raycastHit))
            {
                if (_entityManager.HasComponent<Grabbed>(raycastHit.Entity))
                {
                    EventManager.Input.ObjectGrabbed?.Invoke();
                    _grabbingInteractor.Grab(raycastHit.Entity);
                }
                else
                {
                    EventManager.Input.GroundGrabbed?.Invoke(false);
                }
            }
        }

        private CollisionFilter CreateCollisionFilter()
        {
            return new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = (1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Grabbable) | 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.Ground)),
                GroupIndex = 0,
            };
        }
    }
}