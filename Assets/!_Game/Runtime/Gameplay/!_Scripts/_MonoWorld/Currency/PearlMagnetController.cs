using PrimeTween;
using Reflex.Attributes;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public sealed class PearlMagnetController : MonoBehaviour
    {
        [SerializeField] private PearlsUIView pearlsUIView;
        [SerializeField] private RectTransform flyingPearlPrefab;
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField, Min(0.05f)] private float magnetDuration = 0.4f;
        [SerializeField] private Ease magnetEase = Ease.InCubic;

        private WorldCurrency _currency;
        private UnityEngine.Camera _camera;

        [Inject]
        private void Construct(WorldCurrency currency)
        {
            _currency = currency;
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
            if (flyingPearlPrefab == null || canvasRoot == null || pearlsUIView == null || pearlsUIView.MagnetTarget == null) return;

            if (_camera == null) _camera = CoreHelper.MainCamera;
            if (_camera == null) return;

            var screenPos = _camera.WorldToScreenPoint(evt.Position);
            if (screenPos.z < 0f) return;

            var flying = Instantiate(flyingPearlPrefab, canvasRoot);
            flying.position = screenPos;

            var value = Mathf.RoundToInt(evt.Value);
            var target = pearlsUIView.MagnetTarget;

            Tween.Position(flying, target.position, magnetDuration, magnetEase)
                .OnComplete(() =>
                {
                    if (_currency != null) _currency.Add(value);
                    if (flying != null) Destroy(flying.gameObject);
                });
        }
    }
}
