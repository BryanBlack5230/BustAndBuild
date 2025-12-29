using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.Core.Events;
using GameEngine.Utils;
using UnityEngine;
using Resources = UnityEngine.Resources;

namespace Game.Feature.Input
{
    /// <summary>
    /// A helper class to show cursor while recording videos in editor, should not be present in build version 
    /// </summary>
    public class DummyCursorSetter : IGameUpdateListener, IDisposable, ILoadUnit
    {
        private readonly MousePositionProvider _mousePositionProvider;
        private const float Z_POSITION = -13f; // close to camera so that the dummy is the same size as actual cursor

        private Transform _cursorPrefab;
        private Transform _dummyCursorTransform;
        private SpriteRenderer _dummyCursorRenderer;
        private Dictionary<string, Sprite> _textures;

        public DummyCursorSetter(MousePositionProvider mousePositionProvider)
        {
            _mousePositionProvider = mousePositionProvider;
            Subscribe();
        }

        UniTask ILoadUnit.Load()
        {
            var cursorsToLoad = RuntimeConstants.Cursors.All;
            _textures = new Dictionary<string, Sprite>(cursorsToLoad.Length);

            foreach (var cursorToLoad in cursorsToLoad)
            {
                _textures.Add(cursorToLoad, Resources.Load($"Cursors/Sprites/{cursorToLoad}") as Sprite);
            }
            
            _cursorPrefab = Resources.Load($"Cursors/{RuntimeConstants.Cursors.Dummy}") as Transform;
            _dummyCursorTransform = GameObject.Instantiate(_cursorPrefab, Vector3.zero, Quaternion.identity);
           
            _dummyCursorRenderer = _dummyCursorTransform.GetComponent<SpriteRenderer>();
            return UniTask.CompletedTask;
        }

        public void OnUpdate(float deltaTime) => _dummyCursorTransform.position = _mousePositionProvider.worldMousePosition(Z_POSITION);

        private void Subscribe()
        {
            EventManager.Input.ObjectGrabbed += SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed += SetHoldingGroundCursor;
            EventManager.Input.Release += SetOpenHandCursor;
        }

        private void SetOpenHandCursor() => _dummyCursorRenderer.sprite = _textures[RuntimeConstants.Cursors.Open];

        private void SetHoldingObjectCursor() => _dummyCursorRenderer.sprite = _textures[RuntimeConstants.Cursors.ObjectHold];

        private void SetHoldingGroundCursor(bool actuallyHolding)
        {
            if (actuallyHolding) _dummyCursorRenderer.sprite = _textures[RuntimeConstants.Cursors.GroundHold];
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