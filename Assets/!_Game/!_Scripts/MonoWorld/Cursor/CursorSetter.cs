#if UNITY_EDITOR
using UnityEditor;
#endif
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
    public class CursorSetter : IDisposable, ILoadUnit
    {
        private Dictionary<string, Texture2D> _textures;

        public CursorSetter()
        {
            Cursor.SetCursor(PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
            Register();
        }

        UniTask ILoadUnit.Load()
        {
            var cursorsToLoad = RuntimeConstants.Cursors.All;
            _textures = new Dictionary<string, Texture2D>(cursorsToLoad.Length);

            foreach (var cursorToLoad in cursorsToLoad)
            {
                _textures.Add(cursorToLoad, Resources.Load($"Cursors/Textures/{cursorToLoad}") as Texture2D);
            }
            return UniTask.CompletedTask;
        }

        private void Register()
        {
            EventManager.Input.ObjectGrabbed += SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed += SetHoldingGroundCursor;
            EventManager.Input.Release += SetOpenHandCursor;
        }

        private void SetOpenHandCursor() => Cursor.SetCursor(_textures[RuntimeConstants.Cursors.Open], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingObjectCursor() => Cursor.SetCursor(_textures[RuntimeConstants.Cursors.ObjectHold], Vector2.zero, CursorMode.ForceSoftware);

        private void SetHoldingGroundCursor(bool actuallyHolding)
        {
            if (actuallyHolding) Cursor.SetCursor(_textures[RuntimeConstants.Cursors.GroundHold], Vector2.zero, CursorMode.ForceSoftware);
        }

        public void Dispose()
        {
            EventManager.Input.ObjectGrabbed -= SetHoldingObjectCursor;
            EventManager.Input.GroundGrabbed -= SetHoldingGroundCursor;
            EventManager.Input.Release -= SetOpenHandCursor;
        }
    }
}

