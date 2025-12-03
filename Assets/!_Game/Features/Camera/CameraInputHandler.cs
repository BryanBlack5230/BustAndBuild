using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Events;
using GameManagement;
using UnityEngine;

namespace Game.Feature.Camera
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
        }

        public void HandleGrabEvent(bool alreadyHolding)
        {
            if (alreadyHolding)
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

        public void HandleRelease()
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

                EventManager.Input.GroundGrabbed?.Invoke(true);
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