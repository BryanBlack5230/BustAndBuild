using UnityEngine;


public class ScreenChanged : MonoBehaviour
{
	private float prevWidth;
	private float prevLenght;
	[SerializeField] Transform wall;
	private void Awake() {
		Camera cam = CoreHelper.camera; // initializing camera in Core Helper
		SetSizes();
		CoreHelper.SetWall(wall.position);
	}

	private void Update() {
		if (prevLenght != Screen.width || prevWidth != Screen.height)
		{
			SetSizes();
			CoreHelper.RecalcScreenSize();
			CoreHelper.SetWall(wall.position);
		}
	}

	private void SetSizes()
	{
		prevWidth = Screen.height;
		prevLenght = Screen.width;
	}
	
}
