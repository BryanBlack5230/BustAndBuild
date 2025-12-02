using System;
using Game.Core.Events;
using UnityEngine;

namespace Game.Feature.Input
{
    /// <summary>
    /// A helper class to show cursor while recording videos in editor, should not be present in build version 
    /// </summary>
    public class DummyCursorSetter : IGameUpdateListener, IDisposable
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private const float Z_POSITION = -13f; // close to camera so that the dummy is the same size as actual cursor

        private readonly Sprite _openHandSprite;
        private readonly Sprite _holdingObjectSprite;
        private readonly Sprite _holdingGroundSprite;

        private readonly Transform _dummyCursorTransform;
        private readonly SpriteRenderer _dummyCursorRenderer;
        public DummyCursorSetter(MousePositionProvider mousePositionProvider, GameDataSetter gameData)
        {
            var settings = gameData.gameSettings;
            _mousePositionProvider = mousePositionProvider;
            _dummyCursorTransform = GameObject.Instantiate(gameData.gameSettings.cursorDummyPrefab, Vector3.zero, Quaternion.identity);
            
            _openHandSprite = settings.openHandSprite;
            _holdingObjectSprite = settings.holdingObjectSprite;
            _holdingGroundSprite = settings.holdingGroundSprite;
            
            _dummyCursorRenderer = _dummyCursorTransform.GetComponent<SpriteRenderer>();
            _dummyCursorRenderer.sprite = _openHandSprite;

            Subscribe();
        }

        public void OnUpdate(float deltaTime) => _dummyCursorTransform.position = _mousePositionProvider.worldMousePosition(Z_POSITION);

        private void Subscribe()
        {
            EventManager.Input.ObjectGrabbed += SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed += SetHoldingGroundCursor;
            EventManager.Input.Release += SetOpenHandCursor;
        }

        private void SetOpenHandCursor() => _dummyCursorRenderer.sprite = _openHandSprite;

        private void SetHoldingObjectCursor() => _dummyCursorRenderer.sprite = _holdingObjectSprite;

        private void SetHoldingGroundCursor(bool actuallyHolding)
        {
            if (actuallyHolding) _dummyCursorRenderer.sprite = _holdingGroundSprite;
        }

        public void Dispose()
        {
            EventManager.Input.ObjectGrabbed -= SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed -= SetHoldingGroundCursor;
            EventManager.Input.Release -= SetOpenHandCursor;
            
            GameObject.Destroy(_dummyCursorTransform.gameObject);
        }
    }
}