#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Game.Core.Events;
using UnityEngine;

namespace Game.Feature.Input
{
    public class CursorSetter : IDisposable
    {
        private Texture2D _openHandCursor;
        private Texture2D _holdingObjectCursor;
        private Texture2D _holdingGroundCursor;
        
        public CursorSetter(GameDataSetter gameData)
        {
            var settings = gameData.gameSettings;
            
            _openHandCursor = settings.openHandCursor;
            _holdingObjectCursor = settings.holdingObjectCursor;
            _holdingGroundCursor = settings.holdingGroundCursor;

            Cursor.SetCursor(PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
            Subscribe();
        }
        
        private void Subscribe()
        {
            EventManager.Input.ObjectGrabbed += SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed += SetHoldingGroundCursor;
            EventManager.Input.Release += SetOpenHandCursor;
        }

        private void SetOpenHandCursor() => Cursor.SetCursor(_openHandCursor, Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingObjectCursor() => Cursor.SetCursor(_holdingObjectCursor, Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingGroundCursor(bool actuallyHolding)
        {
            if (actuallyHolding) Cursor.SetCursor(_holdingGroundCursor, Vector2.zero, CursorMode.ForceSoftware);
        }

        public void Dispose()
        {
            EventManager.Input.ObjectGrabbed -= SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed -= SetHoldingGroundCursor;
            EventManager.Input.Release -= SetOpenHandCursor;
        }
    }
}

