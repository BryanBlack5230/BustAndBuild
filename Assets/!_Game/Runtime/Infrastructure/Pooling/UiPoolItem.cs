#nullable enable
using System;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Pooling
{
    /// <summary>
    /// Pooled row container managed by <see cref="UiPool"/>. Holds the data object
    /// the row represents; row views read it via <see cref="Data"/>/<see cref="GetData{T}"/>
    /// and refresh on <see cref="OnLinkChanged"/>.
    /// </summary>
    public sealed class UiPoolItem : MonoBehaviour
    {
        public event Action? OnLinkChanged;

        public object? Data { get; private set; }

        private UiPool? _owner;

        public T? GetData<T>() where T : class => Data as T;

        internal void Link(object data, UiPool owner)
        {
            Data = data;
            _owner = owner;
            gameObject.SetActive(true);
            OnLinkChanged?.Invoke();
        }

        internal void Unlink()
        {
            Data = null;
            gameObject.SetActive(false);
            OnLinkChanged?.Invoke();
        }

        // For the OnDisable return path — the GameObject is already being
        // deactivated, and SetActive is not allowed mid-OnDisable.
        internal void UnlinkInactive()
        {
            Data = null;
            OnLinkChanged?.Invoke();
        }

        private void OnDisable()
        {
            // Disabling a linked item from outside returns it to the pool;
            // during Unlink() Data is already null, so this does not re-enter.
            // Skipped during scene unload, when peer objects may already be gone.
            if (Data == null || !_owner)
                return;
            if (!gameObject.scene.isLoaded)
                return;
            _owner!.ReturnDisabledItem(this);
        }
    }
}
