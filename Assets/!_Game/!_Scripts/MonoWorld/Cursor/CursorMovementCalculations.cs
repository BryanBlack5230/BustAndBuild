using UnityEngine;

namespace Game.Feature.Input
{
    public class CursorMovementCalculations : IGameUpdateListener, IGameResumeListener
    {
        public Vector2 Velocity { get; private set; }
        public Vector2 Acceleration { get; private set; }

        private readonly MousePositionProvider _mousePositionProvider;
        private Vector2 _lastMousePosition = Vector2.zero;

        public CursorMovementCalculations(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
        }

        public void OnResume()
        {
            _lastMousePosition = _mousePositionProvider.mousePosition;
            Velocity = Vector2.zero;
            Acceleration = Vector2.zero;
        }

        public void OnUpdate(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            var newVelocity = (_mousePositionProvider.mousePosition - _lastMousePosition) / deltaTime;
            Acceleration = (newVelocity - Velocity) / deltaTime;
            Velocity = newVelocity;
            _lastMousePosition = _mousePositionProvider.mousePosition;
        }
    }
}