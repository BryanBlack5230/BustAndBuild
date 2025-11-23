using UnityEngine;

namespace Game.Feature.Input
{
    public class CursorMovementCalculations : IGameUpdateListener, IGameResumeListener
    {
        public Vector2 force;
        private MousePositionProvider _mousePositionProvider;
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
            force = (_mousePositionProvider.mousePosition - _lastMousePosition) / deltaTime;
            _lastMousePosition = _mousePositionProvider.mousePosition;
        }
    }
}