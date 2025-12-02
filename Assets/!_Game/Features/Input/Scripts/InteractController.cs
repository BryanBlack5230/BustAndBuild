using System;
using Game.Core.Events;
using Reflex.Attributes;
using Unity.Entities;
using Unity.Physics;
using UnityEngine.InputSystem;

namespace Game.Feature.Input
{
    public class InteractController : IGameStartListener, IGamePauseListener, IGameResumeListener, IDisposable
    {
        // [SerializeField] private LayerMask _layerMask;
        private EntityManager _entityManager;
        private CollisionFilter _collisionFilter;
        private InputActions _inputActions;
        private GrabbingInteractor _grabbingInteractor;
        private MousePositionProvider _mousePositionProvider;

        [Inject]
        private void Construct(InputManager inputManager, GrabbingInteractor grabbingInteractor, MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
            _grabbingInteractor = grabbingInteractor;
            _inputActions = inputManager.Actions;
            Register();
        }

        public void OnStartGame()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateCollisionFilter();
        }

        public void OnPause() => Unregister();
        public void OnResume() => Register();
        public void Dispose() => Unregister();

        private void Register()
        {
            _inputActions.Gameplay.Interact.performed += OnClick;
            _inputActions.Gameplay.Interact.canceled += OnCanceled;
        }

        private void Unregister()
        {
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
                    // camera movement implementation
                }
            }
        }

        private void CreateCollisionFilter()
        {
            // var unitLayerIndex = Mathf.RoundToInt(Mathf.Log(_layerMask, 2));

            _collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = (1u << GameData.GrabbableLayerMask) | (1u << GameData.GroundLayerMask),
                GroupIndex = 0,
            };
        }
    }
}