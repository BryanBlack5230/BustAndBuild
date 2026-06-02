#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Input;
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
            EventBus.Subscribe<ObjectGrabbedEvent>(SetHoldingObjectCursor);
            EventBus.Subscribe<GroundGrabbedEvent>(SetHoldingGroundCursor);
            EventBus.Subscribe<ReleaseEvent>(SetOpenHandCursor);
        }

        private void SetOpenHandCursor(in ReleaseEvent _) => UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.Open], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingObjectCursor(in ObjectGrabbedEvent _) => UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.ObjectHold], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingGroundCursor(in GroundGrabbedEvent evt)
        {
            if (evt.ActuallyHolding) UnityEngine.Cursor.SetCursor(_textures[RuntimeConstants.Cursors.GroundHold], Vector2.zero, CursorMode.ForceSoftware);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<ObjectGrabbedEvent>(SetHoldingObjectCursor);
            EventBus.Unsubscribe<GroundGrabbedEvent>(SetHoldingGroundCursor);
            EventBus.Unsubscribe<ReleaseEvent>(SetOpenHandCursor);
        }
    }
}

