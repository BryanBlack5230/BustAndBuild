using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistortionTrail : MonoBehaviour
{
	private LineRenderer _lr;
    private Quaternion _originalRotation;

    // Start is called before the first frame update
    void Start()
	{
		_lr = transform.GetChild(0).gameObject.transform.GetChild(0)
								   .gameObject.GetComponent<LineRenderer>();
		_lr.enabled = false;
		_originalRotation = transform.rotation;
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	
	IEnumerator DrawDistortion(float time, Vector2 throwForce)
	{
		float sizeMod = 0.4f;
		SetArrow(throwForce, sizeMod);
		//yield return new WaitForSeconds(time);
		// while (_rb.velocity.magnitude > 0.1f) //while (isFlying || _rb.velocity.magnitude > 1f)
		// {
			sizeMod -= 0.05f;
			// SetArrow(_rb.velocity, sizeMod);
			yield return null;
		// }
		RemoveArrow();
	}

	void SetArrow(Vector2 throwForce, float sizeMod)
	{
		_lr.positionCount = 2;
		_lr.SetPosition(0, Vector3.zero);
		_lr.transform.rotation = _originalRotation;
		_lr.SetPosition(1, -1f * throwForce * sizeMod);
		_lr.enabled = true;
	}

	void RemoveArrow()
	{
		_lr.enabled = false;
	}
}
