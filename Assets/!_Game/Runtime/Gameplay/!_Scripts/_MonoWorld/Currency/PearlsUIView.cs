using Reflex.Attributes;
using TMPro;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public sealed class PearlsUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _counterLabel;
        [SerializeField] private RectTransform _magnetTarget;
        [SerializeField] private string _labelFormat = "Pearls: {0}";

        public RectTransform MagnetTarget => _magnetTarget;

        private Wallet _wallet;

        [Inject]
        private void Construct(Wallet wallet)
        {
            _wallet = wallet;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            Refresh(_wallet != null ? _wallet.Get(CurrencyType.Pearls) : 0);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        }

        private void OnCurrencyChanged(in CurrencyChangedEvent evt)
        {
            if (evt.Type != CurrencyType.Pearls) return;
            Refresh(evt.NewTotal);
        }

        private void Refresh(int total)
        {
            if (_counterLabel != null) _counterLabel.text = string.Format(_labelFormat, total);
        }
    }
}
