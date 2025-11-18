using UnityEngine;

namespace GameEngine.Inputs
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Cursor Textures")]
        public Texture2D openHandCursor;
        public Texture2D holdingObjectCursor;
        public Texture2D holdingGroundCursor;

        [Header("Camera Movement Settings")]
        public float moveSpeed = 5f;
        public float upMoveDuration = 1f;
        public float hoverDuration = 1.5f;
        public float returnDuration = 2f;
        public float hoverHeightOffset = 2f;
        public float downwardOffset = 0.5f;
        public float downReturnSpeed = 0.2f;
    }
}