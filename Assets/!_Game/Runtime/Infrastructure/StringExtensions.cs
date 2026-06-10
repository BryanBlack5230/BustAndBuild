using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure
{
    public static class StringExtensions
    {
        private static string GetColor(string name)
        {
            var hue = (uint) name.GetHashCode() / (float) uint.MaxValue;
            var color = Color.HSVToRGB(hue, 0.6f, 1f);
            return ColorUtility.ToHtmlStringRGBA(color);
        }

        public static string ColorBasedOnID(this string text, string id)
        {
            var color = GetColor(id);
            return $"<color=#{color}>{text}</color>";
        }
    }
}