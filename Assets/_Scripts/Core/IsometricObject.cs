using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class IsometricObject : MonoBehaviour
{
	[SerializeField, Tooltip("Will use this object to compute z-order")] Transform target;
	[SerializeField, Tooltip("Use this to offset the object slightly in front or behind the Target object")] int targetOffset = 0;
	private const int _isometricRangePerYUnit = 100;
	
	void Update()
	{
		if (target == null)
			target = transform;
		Renderer renderer = GetComponent<Renderer>();
		renderer.sortingOrder = -(int)(target.position.y * _isometricRangePerYUnit) + targetOffset;
	}
}
