using GameEngine.Utils;
using Reflex.Attributes;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrabbingManager : MonoBehaviour
{
    [SerializeField] private LayerMask _unitLayerMask;
    private EntityManager _entityManager;
    private CollisionFilter _collisionFilter;
    private InputActions _inputActions;

    [Inject]
    private void Construct(InputManager inputManager)
    {
        _inputActions = inputManager.Actions;
        _inputActions.Gameplay.Interract.performed += OnClick;
    }
    private void Awake()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << _unitLayerMask,
            GroupIndex = 0,
        };
    }

    private void OnDestroy()
    {
        _inputActions.Gameplay.Interract.performed -= OnClick;
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        var cameraRay = CoreHelper.MainCamera.ScreenPointToRay(Input.mousePosition);
        
        var entityQuery = _entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        var collisionWorld = entityQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var raycastInput = new RaycastInput
        {
            Start = cameraRay.GetPoint(0f),
            End = cameraRay.GetPoint(9999f),
            Filter = _collisionFilter
        };

        if (collisionWorld.CastRay(raycastInput, out var raycastHit))
        {
            if (_entityManager.HasComponent<Unit>(raycastHit.Entity))
            {
                _entityManager.SetComponentEnabled<Grabbed>(raycastHit.Entity, true);
            }
        }
    }
}
