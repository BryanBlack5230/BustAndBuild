using DG.Tweening;
using UnityEngine;

namespace GameEngine.Inputs
{
    public class CameraMovement
    {
        private Transform _cameraTransform;
        private Vector3 _defaultPosition;
        private GameSettings _settings;

        private Tween _moveTween;
        private Tween _verticalTween;

        public CameraMovement(Transform cameraTransform, GameSettings settings)
        {
            _cameraTransform = cameraTransform;
            _defaultPosition = cameraTransform.position;
            _settings = settings;
        }

        public void MoveHorizontally(float direction)
        {
            _moveTween?.Kill();
            var targetPos = _cameraTransform.position + Vector3.right * direction * _settings.moveSpeed;
            _moveTween = _cameraTransform.DOMoveX(targetPos.x, 0.5f).SetEase(Ease.OutSine);
        }

        public void MoveUp()
        {
            _verticalTween?.Kill();
            var hoverPos = _defaultPosition + Vector3.up * _settings.hoverHeightOffset;

            _verticalTween = _cameraTransform.DOMoveY(hoverPos.y, _settings.upMoveDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    DOVirtual.DelayedCall(_settings.hoverDuration, () =>
                    {
                        _cameraTransform.DOMoveY(_defaultPosition.y, _settings.returnDuration).SetEase(Ease.InQuad);
                    });
                });
        }

        public void MoveDown()
        {
            _verticalTween?.Kill();
            var lowerPos = _defaultPosition - Vector3.up * _settings.downwardOffset;

            _verticalTween = _cameraTransform.DOMoveY(lowerPos.y, 0.1f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _cameraTransform.DOMoveY(_defaultPosition.y, _settings.downReturnSpeed).SetEase(Ease.InQuad);
                });
        }
    }
}