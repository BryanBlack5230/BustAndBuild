using UnityEngine;

public class TomatoFieldController : MonoBehaviour
{
	[SerializeField] Sprite[] tomatoLevels;
	private SpriteRenderer[] _tomatoes;
	private void Awake() 
	{
		_tomatoes = GetComponentsInChildren<SpriteRenderer>();
		SetLevel(0);
	}

	public void SetLevel(int level)
	{
		foreach (var tomato in _tomatoes)
		{
			tomato.sprite = tomatoLevels[level];
		}
	}
}
