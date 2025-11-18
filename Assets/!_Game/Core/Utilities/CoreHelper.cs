using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Utils
{
    public static class CoreHelper
    {
        private static Camera _mainCamera;
        public static Camera MainCamera
        {
            get
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                return _mainCamera;
            }
        }
        
        private static readonly Dictionary<float, WaitForSeconds> _waitDictionary = new();
        public static WaitForSeconds GetWait(float time)
        {
            if (_waitDictionary.TryGetValue(time, out var wait)) return wait;

            _waitDictionary[time] = new WaitForSeconds(time);
            return _waitDictionary[time];
        }
        
        public static Vector2 CursorPos() => MainCamera.ScreenToWorldPoint(Input.mousePosition);
        public static Unity.Mathematics.float2 CursorPos2DECS() => (Vector2)MainCamera.ScreenToWorldPoint(Input.mousePosition);
        public static Unity.Mathematics.float3 CursorPosECS() => MainCamera.ScreenToWorldPoint(Input.mousePosition);
        public static string TimeNow() => System.DateTime.Now.ToString("HH:mm:ss.fff");
    }
}

