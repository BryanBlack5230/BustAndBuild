using UnityEngine;


public class ScreenChanged : MonoBehaviour
{
	private float _prevWidth;
	private float _prevLenght;
	private void Awake() {
		Camera cam = CoreHelper.camera; // initializing camera in Core Helper
		SetSizes();
	}

	private void Update() {
		if (_prevLenght != Screen.width || _prevWidth != Screen.height)
		{
			SetSizes();
			CoreHelper.RecalcScreenSize();
			// CoreHelper.SetWall(wall.position);
		}
	}

	private void SetSizes()
	{
		_prevWidth = Screen.height;
		_prevLenght = Screen.width;
	}
	
}
