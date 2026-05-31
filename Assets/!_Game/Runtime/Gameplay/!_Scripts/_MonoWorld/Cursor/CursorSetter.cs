#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Settings;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Cursor
{
    public class CursorSetter : IDisposable, ILoadUnit
    {
        private Dictionary<string, Texture2D> _textures;

        public CursorSetter()
        {
            UnityEngine.Cursor.SetCursor(PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
            Register();
        }

        UniTask ILoadUnit.Load()
        {
            var cursorsToLoad = RuntimeConstants.Cursors.All;
            _textures = new Dictionary<string, Texture2D>(cursorsToLoad.Length);

            foreach (var cursorToLoad in cursorsToLoad)
            {
                _textures.Add(cursorToLoad, AssetService.R.Load<Texture2D>(RuntimeConstants.Cursors.TexturesPath + cursorToLoad));
            }
            return UniTask.CompletedTask;
        }

        private void Register()
        {
            EventManager.Input.ObjectGrabbed += SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed += SetHoldingGroundCursor;
            EventManager.Input.Release += SetOpenHandCursor;
        }

        private void SetOpenHandCursor() => UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.Open], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingObjectCursor() => UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.ObjectHold], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingGroundCursor(bool actuallyHolding)
        {
            if (actuallyHolding) UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.GroundHold], Vector2.zero, CursorMode.ForceSoftware);
        }

        public void Dispose()
        {
            EventManager.Input.ObjectGrabbed -= SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed -= SetHoldingGroundCursor;
            EventManager.Input.Release -= SetOpenHandCursor;
        }
    }
}

