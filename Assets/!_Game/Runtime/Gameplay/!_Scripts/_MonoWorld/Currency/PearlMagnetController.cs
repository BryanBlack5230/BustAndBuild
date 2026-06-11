using PrimeTween;
using Reflex.Attributes;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public sealed class PearlMagnetController : MonoBehaviour
    {
        [SerializeField] private PearlsUIView _pearlsUIView;
        [SerializeField] private RectTransform _flyingPearlPrefab;
        [SerializeField] private RectTransform _canvasRoot;
        [SerializeField, Min(0.05f)] private float _magnetDuration = 0.4f;
        [SerializeField] private Ease _magnetEase = Ease.InCubic;

        private Wallet _wallet;
        private UnityEngine.Camera _camera;

        [Inject]
        private void Construct(Wallet wallet)
        {
            _wallet = wallet;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PearlPickedUpEvent>(OnPearlPickedUp);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PearlPickedUpEvent>(OnPearlPickedUp);
        }

        private void OnPearlPickedUp(in PearlPickedUpEvent evt)
        {
            if (_flyingPearlPrefab == null || _canvasRoot == null || _pearlsUIView == null || _pearlsUIView.MagnetTarget == null) return;

            if (_camera == null) _camera = CoreHelper.MainCamera;
            if (_camera == null) return;

            var screenPos = _camera.WorldToScreenPoint(evt.Position);
            if (screenPos.z < 0f) return;

            var flying = Instantiate(_flyingPearlPrefab, _canvasRoot);
            flying.position = screenPos;

            var value = Mathf.RoundToInt(evt.Value);
            var target = _pearlsUIView.MagnetTarget;

            Tween.Position(flying, target.position, _magnetDuration, _magnetEase)
                .OnComplete(() =>
                {
                    if (_wallet != null) _wallet.Add(CurrencyType.Pearls, value);
                    if (flying != null) Destroy(flying.gameObject);
                });
        }
    }
}
