using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Camera
{
    public class CameraInputHandler
    {
        private readonly float _holdDuration;
        private CancellationTokenSource _holdCts;
        private Countdown _countdown;
        
        public event Action DragStarted;
        public event Action DragEnded;

        public CameraInputHandler(float holdDuration)
        {
            _holdDuration = holdDuration;
        }

        public void Dispose()
        {
            _holdCts?.Cancel();
            _holdCts?.Dispose();
            _holdCts = null;
        }

        public void HandleGrabEvent(in GroundGrabbedEvent evt)
        {
            if (evt.ActuallyHolding)
            {
                CancelHold();
                DragStarted?.Invoke();
                return;
            }

            CancelHold();
            _holdCts = new CancellationTokenSource();

            _countdown = new Countdown((int)_holdDuration);
            _countdown.StartCountdownAsync().Forget();

            WaitForHoldAsync(_holdCts.Token).Forget();
        }

        public void HandleRelease(in ReleaseEvent _)
        {
            CancelHold();
            DragEnded?.Invoke();
        }

        private async UniTask WaitForHoldAsync(CancellationToken token)
        {
            var elapsed = 0f;

            try
            {
                while (elapsed < _holdDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                EventBus.Raise(new GroundGrabbedEvent(true));
            }
            catch { }
        }

        private void CancelHold()
        {
            _holdCts?.Cancel();
            _holdCts?.Dispose();
            _holdCts = null;

            _countdown?.Cancel();
            _countdown?.Hide();
            _countdown = null;
        }
    }
}