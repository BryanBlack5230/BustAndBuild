using UnityEngine;

namespace Game.Feature.Input
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Cursor Textures")]
        public Texture2D openHandCursor;
        public Texture2D holdingObjectCursor;
        public Texture2D holdingGroundCursor;
        
        public Sprite openHandSprite;
        public Sprite holdingObjectSprite;
        public Sprite holdingGroundSprite;
        
        public Transform cursorDummyPrefab;
    }
}