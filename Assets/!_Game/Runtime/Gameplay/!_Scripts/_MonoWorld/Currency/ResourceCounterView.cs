using Reflex.Attributes;
using TMPro;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    /// <summary>
    /// A single resource slot on the HUD: shows the wallet total for one <see cref="CurrencyType"/>
    /// and exposes the magnet fly-to target + icon so the magnet controller can animate pickups here.
    /// Add one per resource on the resource bar.
    /// </summary>
    public sealed class ResourceCounterView : MonoBehaviour
    {
        [SerializeField] private CurrencyType _type = CurrencyType.Pearls;
        [SerializeField] private TMP_Text _counterLabel;
        [SerializeField] private RectTransform _magnetTarget;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _labelFormat = "{0}";

        public CurrencyType Type => _type;
        public RectTransform MagnetTarget => _magnetTarget;
        public Sprite Icon => _icon;

        private Wallet _wallet;

        [Inject]
        private void Construct(Wallet wallet)
        {
            _wallet = wallet;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            Refresh(_wallet != null ? _wallet.Get(_type) : 0);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        }

        private void OnCurrencyChanged(in CurrencyChangedEvent evt)
        {
            if (evt.Type != _type) return;
            Refresh(evt.NewTotal);
        }

        private void Refresh(int total)
        {
            if (_counterLabel != null) _counterLabel.text = string.Format(_labelFormat, total);
        }
    }
}
