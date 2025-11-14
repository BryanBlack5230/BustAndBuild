using UnityEngine;

namespace GameEngine.Inputs
{
    public class CursorManager
    {
        private Texture2D _openHandCursor;
        private Texture2D _holdingObjectCursor;
        private Texture2D _holdingGroundCursor;

        public CursorManager(GameSettings settings)
        {
            _openHandCursor = settings.openHandCursor;
            _holdingObjectCursor = settings.holdingObjectCursor;
            _holdingGroundCursor = settings.holdingGroundCursor;
        }

        public void SetOpenHandCursor()
        {
            Cursor.SetCursor(_openHandCursor, Vector2.zero, CursorMode.ForceSoftware);
        }

        public void SetHoldingObjectCursor()
        {
            Cursor.SetCursor(_holdingObjectCursor, Vector2.zero, CursorMode.ForceSoftware);
        }

        public void SetHoldingGroundCursor()
        {
            Cursor.SetCursor(_holdingGroundCursor, Vector2.zero, CursorMode.ForceSoftware);
        }
    }
}

