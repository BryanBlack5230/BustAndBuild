using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameManagement
{
    public class Countdown
    {
        private TextMeshProUGUI _countdownText;
        private int _countdownDuration;
        private Color _startColor = Color.cyan;
        private Color _endColor = Color.blue;

        public Countdown(int countdownDuration)
        {
            _countdownDuration = countdownDuration;
            _endColor.a = 0.5f;
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
        }

        public async Task StartCountdownAsync()
        {
            _countdownText.gameObject.SetActive(true);

            for (int i = _countdownDuration; i > 0; i--)
            {
                _countdownText.text = i.ToString();

                _countdownText.color = _startColor;
                _countdownText.DOColor(_endColor, 1f).SetEase(Ease.InOutSine);

                await Task.Delay(1000);
            }

            _countdownText.gameObject.SetActive(false);
        }
    }
}