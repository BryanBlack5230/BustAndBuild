using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Game.Configs;
using UnityEngine;

namespace Game.Feature.Camera
{
    public class CameraBorderHandler
    {
        private readonly CinemachineTransposer _transposer;
        private readonly BorderRange _rangeX, _rangeY;
        private readonly CameraConfig _config;
        
        private CancellationTokenSource _returnCts;

        public CameraBorderHandler(CinemachineTransposer transposer, BorderRange rangeX, BorderRange rangeY, CameraConfig config)
        {
            _rangeX = rangeX;
            _rangeY = rangeY;
            _config = config;
            _transposer = transposer;
        }
        
        public void Dispose()
        {
            _returnCts?.Cancel();
            _returnCts?.Dispose();
            _returnCts = null;
        }

        public void CancelReturn()
        {
            _returnCts?.Cancel();
            _returnCts = null;
        }

        public bool IsOutsideBounds(Vector3 pos) => _rangeX.IsOutside(pos.x) || _rangeY.IsOutside(pos.y);

        public Vector2 ApplyResistanceIfNeeded(Vector2 movement)
        {
            var offset = _transposer.m_FollowOffset;

            if (!IsOutsideBounds(offset))
                return movement;

            return new Vector2(
                movement.x * CalculateResistance(offset.x, movement.x, _rangeX),
                movement.y * CalculateResistance(offset.y, movement.y, _rangeY)
            );
        }

        private float CalculateResistance(float pos, float movement, BorderRange r)
        {
            var dist = r.DistanceOutside(pos);
            if (dist <= 0) return 1f;

            var isReturning =
                (pos < r.Min && movement > 0f) ||
                (pos > r.Max && movement < 0f); 

            var t = Mathf.Clamp01(dist / _config.maxOutsideDistance);
            var resistance = _config.borderPushCurve.Evaluate(t);

            return Mathf.Lerp(1f, isReturning ? 2f : 0f, resistance);
        }

        public void StartReturn()
        {
            _returnCts?.Cancel();
            _returnCts = new CancellationTokenSource();

            ReturnToBoundsAsync(_returnCts.Token).Forget();
        }

        private async UniTask ReturnToBoundsAsync(CancellationToken token)
        {
            var start = _transposer.m_FollowOffset;
            var target = new Vector3(
                Mathf.Clamp(start.x, _rangeX.Min, _rangeX.Max),
                Mathf.Clamp(start.y, _rangeY.Min, _rangeY.Max),
                start.z
            );

            var duration = 0f;
            while (duration < _config.returnDuration)
            {
                duration += Time.deltaTime;
                var t = Mathf.Clamp01(duration / _config.returnDuration);

                var curve = _config.returnCurve.Evaluate(t);
                _transposer.m_FollowOffset = Vector3.LerpUnclamped(start, target, curve);

                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            _transposer.m_FollowOffset = target;
        }
    }
}