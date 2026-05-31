using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Input;
using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Cursor
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
                _textures.Add(cursorToLoad, AssetService.R.Load<Sprite>(RuntimeConstants.Cursors.SpritesPath + cursorToLoad));
            }

            _cursorPrefab = AssetService.R.Load<Transform>(RuntimeConstants.Cursors.DummyPrefabPath);
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