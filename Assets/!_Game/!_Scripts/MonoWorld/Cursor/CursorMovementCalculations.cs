using UnityEngine;

namespace Game.Feature.Input
{
    public class CursorMovementCalculations : IGameUpdateListener, IGameResumeListener
    {
        public Vector2 velocity;
        private readonly MousePositionProvider _mousePositionProvider;
        private Vector2 _lastMousePosition = Vector2.zero;

        public CursorMovementCalculations(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
        }

        public void OnResume()
        {
            _lastMousePosition = _mousePositionProvider.mousePosition;
        }

        public void OnUpdate(float deltaTime)
        {
            velocity = (_mousePositionProvider.mousePosition - _lastMousePosition) / deltaTime;
            _lastMousePosition = _mousePositionProvider.mousePosition;
        }
    }
}