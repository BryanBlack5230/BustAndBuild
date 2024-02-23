using UnityEngine;

public class IsometricObjectHandler : MonoBehaviour
{
	private IsometricObject[] _isometricBodyParts;

	public void SetIsometricObjectUpdate(bool enabled)
	{
		if (_isometricBodyParts == null) Cache();
		
		foreach (var io in _isometricBodyParts)
		{
			io.enabled = enabled;
		}
	}

	private void Cache()
	{
		Transform gfx = transform.GetChild(0).transform;

		_isometricBodyParts = gfx.GetComponentsInChildren<IsometricObject>();
	}
}
