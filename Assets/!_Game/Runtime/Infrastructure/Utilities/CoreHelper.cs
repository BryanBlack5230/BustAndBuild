using System.Collections.Generic;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public static class CoreHelper
    {
        private static Camera _mainCamera;
        public static Camera MainCamera
        {
            get
            {
                if (_mainCamera == null) _mainCamera = Camera.main; // don't forget to tag a camera with MainCamera tag
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
        
        public static string TimeNow() => System.DateTime.Now.ToString("HH:mm:ss.fff");
    }
}

