using UnityEngine;

public class IsometricObjectHandler : MonoBehaviour
{
	private IsometricObject[] _isometricBodyParts;
	void Start()
	{
		Transform gfx = transform.GetChild(0).transform;

		_isometricBodyParts = gfx.GetComponentsInChildren<IsometricObject>();
	}

	public void SetIsometricObjectUpdate(bool enabled)
	{
		foreach (var io in _isometricBodyParts)
		{
			io.enabled = enabled;
		}
	}
}
