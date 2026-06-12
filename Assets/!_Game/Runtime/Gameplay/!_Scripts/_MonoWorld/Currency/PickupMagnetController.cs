using System.Collections.Generic;

using PrimeTween;
using Reflex.Attributes;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// Flies collected pickups from their world position to the matching resource counter on the HUD,
    /// then credits the wallet on landing. Routes by <see cref="CurrencyType"/> using the counter views
    /// registered below. Resources without a registered counter are credited immediately (no animation).
    /// </summary>
    public sealed class PickupMagnetController : MonoBehaviour
    {
        [SerializeField] private ResourceCounterView[] _counters;
        [SerializeField] private FlyingPickup _flyingPickupPrefab;
        [SerializeField] private RectTransform _canvasRoot;
        [SerializeField, Min(0.05f)] private float _magnetDuration = 0.4f;
        [SerializeField] private Ease _magnetEase = Ease.InCubic;

        private readonly Dictionary<CurrencyType, ResourceCounterView> _byType = new();
        private Wallet _wallet;
        private UnityEngine.Camera _camera;

        [Inject]
        private void Construct(Wallet wallet)
        {
            _wallet = wallet;
        }

        private void Awake()
        {
            if (_counters == null) return;
            foreach (var counter in _counters)
                if (counter != null) _byType[counter.Type] = counter;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void OnPickupCollected(in PickupCollectedEvent evt)
        {
            var amount = Mathf.RoundToInt(evt.Value);

            if (!_byType.TryGetValue(evt.Type, out var counter) || counter == null || counter.MagnetTarget == null
                || _flyingPickupPrefab == null || _canvasRoot == null)
            {
                // No HUD slot for this resource yet — credit immediately so the pickup isn't lost.
                if (_wallet != null) _wallet.Add(evt.Type, amount);
                return;
            }

            if (_camera == null) _camera = CoreHelper.MainCamera;
            if (_camera == null) return;

            var screenPos = _camera.WorldToScreenPoint(evt.Position);
            if (screenPos.z < 0f) return;

            var flying = Instantiate(_flyingPickupPrefab, _canvasRoot);
            flying.RectTransform.position = screenPos;
            flying.SetIcon(counter.Icon);

            var type = evt.Type;
            var target = counter.MagnetTarget;

            Tween.Position(flying.RectTransform, target.position, _magnetDuration, _magnetEase)
                .OnComplete(() =>
                {
                    if (_wallet != null) _wallet.Add(type, amount);
                    if (flying != null) Destroy(flying.gameObject);
                });
        }
    }
}
