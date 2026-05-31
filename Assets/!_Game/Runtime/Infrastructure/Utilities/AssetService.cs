using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    public static class AssetService
    {
        public static Resources R { get; } = new();
    }
    
    public sealed class Resources
    {
        public T Load<T>(string path) where T : Object
        {
            var result = UnityEngine.Resources.Load<T>(path);
            return result;
        }

        public T[] LoadAll<T>(string path) where T : Object
        {
            return UnityEngine.Resources.LoadAll<T>(path);
        }
    }
}