using GameEngine.Utils;
using Reflex.Attributes;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabbingManager : MonoBehaviour
{
    [SerializeField] private LayerMask _unitLayerMask;
    private int _unitLayerIndex;
    private EntityManager _entityManager;
    private CollisionFilter _collisionFilter;
    private InputActions _inputActions;

    [Inject]
    private void Construct(InputManager inputManager)
    {
        _inputActions = inputManager.Actions;
        _inputActions.Gameplay.Interact.performed += OnClick;
        _inputActions.Gameplay.Interact.canceled += OnCanceled;
    }

    private void OnCanceled(InputAction.CallbackContext obj)
    {
        Debug.Log("OnCanceled");
    }

    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _unitLayerIndex = Mathf.RoundToInt(Mathf.Log(_unitLayerMask.value, 2));

        _collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << _unitLayerIndex,
            GroupIndex = 0,
        };
    }

    private void OnDestroy()
    {
        _inputActions.Gameplay.Interact.performed -= OnClick;
        _inputActions.Gameplay.Interact.canceled -= OnCanceled;
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Debug.Log("OnPerformed");
        var cameraRay = CoreHelper.MainCamera.ScreenPointToRay(_inputActions.Gameplay.MousePosition.ReadValue<Vector2>());
        // Debug.DrawRay(cameraRay.GetPoint(0f), cameraRay.GetPoint(9999f), Color.red);
        
        var entityQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        var collisionWorld = entityQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var raycastInput = new RaycastInput
        {
            Start = cameraRay.GetPoint(9f),
            End = cameraRay.GetPoint(40f),
            Filter = _collisionFilter
        };

        if (collisionWorld.CastRay(raycastInput, out var raycastHit))
        {
            if (_entityManager.HasComponent<Grabbed>(raycastHit.Entity))
            {
                Debug.Log($"Grabbed {raycastHit.Entity}");
                _entityManager.SetComponentEnabled<Grabbed>(raycastHit.Entity, true);
            }
        }
    }
}
