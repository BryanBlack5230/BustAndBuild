using Reflex.Attributes;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Feature.Input
{
    public class PowerHitController : MonoBehaviour
    {
        private InputActions _inputActions;
        private MousePositionProvider _mousePositionProvider;
        private CollisionFilter _collisionFilter;
        private EntityManager _entityManager;
        private PowerHitSettings _powerHitSettings;
        
        private bool _isActive;
        private float _activeDuration;

        [Inject]
        private void Construct(InputManager inputManager, MousePositionProvider mousePositionProvider, GameDataSetter gameData)
        {
            _mousePositionProvider = mousePositionProvider;
            _inputActions = inputManager.Actions;
            _powerHitSettings = gameData.powerHitSettings;
            Register();
        }
        
        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateCollisionFilter();
        }

        private void Register()
        {
            _inputActions.Gameplay.PowerHit.performed += OnClick;
            _inputActions.Gameplay.PowerHit.canceled += OnCanceled;
        }

        private void OnDestroy()
        {
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

        private void Update()
        {
            if (!_isActive) return;
            
            _activeDuration += Time.deltaTime;
            if (_activeDuration >= _powerHitSettings.duration) Cancel();
            if (!_isActive) return;
            
            Debug.Log("PowerHitController: Active");
            //Not implemented yet
        }

        private void CreateCollisionFilter()
        {
            // var unitLayerIndex = Mathf.RoundToInt(Mathf.Log(_layerMask, 2));

            _collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameData.GrabbableLayerMask,
                GroupIndex = 0,
            };
        }
    }
}