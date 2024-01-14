using System.Collections.Generic;
using UnityEngine;


public static class CoreHelper
{
	private static Camera _camera;

	public static Camera camera
	{
		get
		{
			if (_camera == null) _camera = Camera.main;
			return _camera;
		}
	}

	private static readonly Dictionary<float, WaitForSeconds> _waitDictionary = new Dictionary<float, WaitForSeconds>();
	public static WaitForSeconds GetWait(float time)
	{
		if (_waitDictionary.TryGetValue(time, out var wait)) return wait;

		_waitDictionary[time] = new WaitForSeconds(time);
		return _waitDictionary[time];
	}

	private static float _perspectiveAngle;
	private static float _groundScreenPercent;
	public static void SetAngleAndGroundSize(float angle, float percent)
	{
		_perspectiveAngle = angle;
		_groundScreenPercent = percent;
	}

	public static float GetAngle()
	{
		return _perspectiveAngle;
	}

	public static float GetGroundSize()
	{
		return _groundScreenPercent;
	}

	public static float groundBottomY { get; private set; }
	public static float groundTopY { get; private set; }
	public static float screenLength { get; private set; }
	public static float GetDepthModifier(float yCoord)
	{
		float clampedY = Mathf.Clamp(yCoord, groundBottomY, groundTopY);
		float depthLength = screenLength - 2 * (clampedY - groundBottomY) 
							* Mathf.Tan(_perspectiveAngle * Mathf.Deg2Rad);
		return depthLength / screenLength;
	}

	public static void RecalcScreenSize()
	{
		Vector3 bottomLeft = _camera.ViewportToWorldPoint(Vector3.zero);
		Vector3 topRight = _camera.ViewportToWorldPoint(Vector3.one);

		screenLength = topRight.x - bottomLeft.x;
		float groundWidth = (topRight.y - bottomLeft.y) * _groundScreenPercent;
		groundBottomY = bottomLeft.y;
		groundTopY = bottomLeft.y + groundWidth;
	}

	private static Vector2 _wallCoords;
	public static void SetWall(Vector2 wall)
	{
		_wallCoords = wall;
	}
	public static Vector2 GetWallPos(float yCoord)
	{
        float intersectionX = _wallCoords.x + (_wallCoords.y - yCoord) 
											* Mathf.Tan(_perspectiveAngle * Mathf.Deg2Rad);
        return new Vector2(intersectionX, yCoord);
	}
	
}
