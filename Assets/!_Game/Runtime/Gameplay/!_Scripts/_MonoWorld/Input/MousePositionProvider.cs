using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Input
{
    public class MousePositionProvider
    {
        public Vector2 mousePosition => _inputActions.Gameplay.MousePosition.ReadValue<Vector2>();
        public Vector3 mouseScreenToWorld => CoreHelper.MainCamera.ScreenToWorldPoint(mousePosition);

        public Vector3 worldMousePosition(Vector3 objectPosition)
        {
            var ray = CoreHelper.MainCamera.ScreenPointToRay(mousePosition);
            var entityZ = objectPosition.z;

            var t = (entityZ - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * t;
        }
    
        public Vector3 worldMousePosition(float zCoordinate)
        {
            var ray = screenPointToRay;
            var t = (zCoordinate - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * t;
        }
        public Ray screenPointToRay => CoreHelper.MainCamera.ScreenPointToRay(mousePosition);
    
        private readonly InputActions _inputActions;
        public MousePositionProvider(InputManager inputManager)
        {
            _inputActions = inputManager.Actions;
        }
    }
}