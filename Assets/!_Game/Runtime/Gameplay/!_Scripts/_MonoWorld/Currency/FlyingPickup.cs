using UnityEngine;
using UnityEngine.UI;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// Root of a flying-pickup HUD instance (the icon that magnets from the world to a counter).
    /// Holds its icon <see cref="Image"/> as a serialized reference so the magnet controller can
    /// recolor it per resource with no per-spawn GetComponent — Unity remaps the reference to the
    /// clone on Instantiate. Drag the child Image into the inspector slot.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FlyingPickup : MonoBehaviour
    {
        [SerializeField, Tooltip("The Image whose sprite is swapped to match the collected resource.")]
        private Image _icon;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon != null && sprite != null) _icon.sprite = sprite;
        }
    }
}
