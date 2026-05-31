using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public class Countdown
    {
        private TextMeshProUGUI _countdownText;
        private int _countdownDuration;
        private Color _startColor = Color.cyan;
        private Color _endColor = Color.blue;
        
        private CancellationTokenSource _cts;

        public Countdown(int countdownDuration)
        {
            _countdownDuration = countdownDuration;
            _endColor.a = 0.5f;
            
            _cts = new CancellationTokenSource();
            CreateTextObject();
        }

        private void CreateTextObject()
        {
            GameObject textObject = new("CountdownText");
            _countdownText = textObject.AddComponent<TextMeshProUGUI>();

            _countdownText.fontSize = 100;
            _countdownText.alignment = TextAlignmentOptions.Center;
            _countdownText.rectTransform.SetParent(GameObject.FindObjectOfType<Canvas>().transform, false);
            _countdownText.rectTransform.sizeDelta = new Vector2(500, 200);
            _countdownText.gameObject.SetActive(false);
        }

        public async UniTask StartCountdownAsync()
        {
            _countdownText.gameObject.SetActive(true);

            try
            {
                for (int i = _countdownDuration; i > 0; i--)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    _countdownText.text = i.ToString();

                    Tween.StopAll(_countdownText);
                    _countdownText.color = _startColor;
                    Tween.Color(_countdownText, _endColor, 1f, Ease.InOutSine);

                    await UniTask.Delay(1000, cancellationToken: _cts.Token);
                }
            }
            catch
            {
                // cancelled → ignore
            }
            finally
            {
                _countdownText.gameObject.SetActive(false);
            }
        }

        public void Cancel()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
        }
        
        public void Hide()
        {
            if (_countdownText != null)
                _countdownText.gameObject.SetActive(false);
        }

    }
}