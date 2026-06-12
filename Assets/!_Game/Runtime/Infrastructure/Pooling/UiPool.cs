#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Pooling
{
    /// <summary>
    /// Data-keyed pool for UI list rows. Each data object gets one pooled
    /// <see cref="UiPoolItem"/> child under this transform; removed rows are
    /// deactivated and reused instead of destroyed. Pre-placed UiPoolItem
    /// children are absorbed into the free queue on Awake.
    /// </summary>
    public sealed class UiPool : MonoBehaviour
    {
        // Fresh items are parked far offscreen for the frame before a layout
        // group positions them, so they never flash at the origin.
        private static readonly Vector3 OffscreenPosition = new(1000000f, 1000000f, 0f);

        [SerializeField] private UiPoolItem? _itemPrefab;

        private readonly Dictionary<object, UiPoolItem> _activeItems = new();
        private readonly Queue<UiPoolItem> _freeItems = new();
        private readonly List<object> _removalBuffer = new();

        private bool _isTearingDown;

        internal bool IsTearingDown => _isTearingDown;

        private void Awake()
        {
            if (!_itemPrefab)
                throw new InvalidOperationException($"{nameof(UiPool)} on '{name}' has no item prefab assigned.");

            var preplacedItems = GetComponentsInChildren<UiPoolItem>(true);
            foreach (var item in preplacedItems)
            {
                item.gameObject.SetActive(false);
                _freeItems.Enqueue(item);
            }
        }

        private void OnDestroy()
        {
            // Children are destroyed along with the pool — touching them here
            // risks MissingReferenceException, so only bookkeeping is cleared.
            _isTearingDown = true;
            _activeItems.Clear();
            _freeItems.Clear();
        }

        public void AddItem(object data, bool asFirstSibling = false)
        {
            if (_activeItems.ContainsKey(data))
                return;

            var item = TakeFreeItem();
            if (asFirstSibling)
                item.transform.SetAsFirstSibling();
            else
                item.transform.SetAsLastSibling();
            item.transform.localPosition = OffscreenPosition;

            _activeItems.Add(data, item);
            item.Link(data, this);
        }

        /// <summary>
        /// Adds a row for the data (or finds the existing one) and returns the
        /// view component of the row container.
        /// </summary>
        public T AddItem<T>(object data, bool asFirstSibling = false) where T : MonoBehaviour
        {
            AddItem(data, asFirstSibling);
            var view = GetItem<T>(data);
            if (!view)
                throw new InvalidOperationException(
                    $"Pool item prefab of '{name}' has no {typeof(T).Name} component.");
            return view!;
        }

        public T? GetItem<T>(object data) where T : MonoBehaviour
        {
            if (!_activeItems.TryGetValue(data, out var item))
                return null;
            var view = item.GetComponent<T>();
            return view ? view : item.GetComponentInChildren<T>();
        }

        public void RemoveItem(object data)
        {
            if (!_activeItems.TryGetValue(data, out var item))
                return;
            _activeItems.Remove(data);
            item.Unlink();
            _freeItems.Enqueue(item);
        }

        public void RemoveWhere(Func<object, bool> predicate)
        {
            _removalBuffer.Clear();
            foreach (var key in _activeItems.Keys)
            {
                if (predicate(key))
                    _removalBuffer.Add(key);
            }

            foreach (var key in _removalBuffer)
                RemoveItem(key);
        }

        public void Clear()
        {
            _removalBuffer.Clear();
            foreach (var key in _activeItems.Keys)
                _removalBuffer.Add(key);

            foreach (var key in _removalBuffer)
                RemoveItem(key);
        }

        // Called by UiPoolItem.OnDisable when a linked row is deactivated from
        // outside; the item is already inactive, so Unlink's SetActive is skipped.
        internal void ReturnDisabledItem(UiPoolItem item)
        {
            if (_isTearingDown || item.Data == null)
                return;
            if (!_activeItems.Remove(item.Data))
                return;
            item.UnlinkInactive();
            _freeItems.Enqueue(item);
        }

        private UiPoolItem TakeFreeItem()
        {
            // Items destroyed from outside survive in the queue as fake-null — skip them.
            while (_freeItems.Count > 0)
            {
                var pooled = _freeItems.Dequeue();
                if (pooled)
                    return pooled;
            }

            var item = Instantiate(_itemPrefab!, OffscreenPosition, Quaternion.identity, transform);
            item.gameObject.SetActive(false);
            return item;
        }
    }
}
