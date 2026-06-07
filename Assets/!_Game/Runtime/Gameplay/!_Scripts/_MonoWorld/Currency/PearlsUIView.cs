using Reflex.Attributes;
using TMPro;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;

namespace BarkingBird.Runtime.Gameplay.Currency
{
    public sealed class PearlsUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private RectTransform magnetTarget;
        [SerializeField] private string labelFormat = "Pearls: {0}";

        public RectTransform MagnetTarget => magnetTarget;

        private WorldCurrency _currency;

        [Inject]
        private void Construct(WorldCurrency currency)
        {
            _currency = currency;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PearlsChangedEvent>(OnPearlsChanged);
            Refresh(_currency != null ? _currency.Pearls : 0);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PearlsChangedEvent>(OnPearlsChanged);
        }

        private void OnPearlsChanged(in PearlsChangedEvent evt) => Refresh(evt.NewTotal);

        private void Refresh(int total)
        {
            if (counterLabel != null) counterLabel.text = string.Format(labelFormat, total);
        }
    }
}
