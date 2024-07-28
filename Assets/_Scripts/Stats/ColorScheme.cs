
using UnityEngine;

[CreateAssetMenu(fileName = "ColorScheme", menuName = "Settings/New Color Scheme", order = 1)]
public class ColorScheme : ScriptableObject
{
	[SerializeField] public StateColorScheme aware;
	[SerializeField] public StateColorScheme suspicious;
	[SerializeField] public StateColorScheme scared;
	[SerializeField] public StateColorScheme angry;
	[System.Serializable]
	public struct StateColorScheme
	{
		public Color topColor;
		public Color botColor;
		public float blendHeight;
	}
}
