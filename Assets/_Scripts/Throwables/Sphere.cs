using System.Collections;
using System.Collections.Generic;
using BB.Combat;
using UnityEngine;

public class Sphere : MonoBehaviour
{
	private Rigidbody2D _rb;
	private bool _isFlying;
	private Grabbable _grabbable;
	
	private void Awake() 
	{
		_rb = GetComponent<Rigidbody2D>();
		_grabbable = GetComponent<Grabbable>();
		_grabbable.OnSetPosition += OnSetPositionCallback;
	}

	private void OnSetPositionCallback(Vector2 newPos)
	{
		_rb.MovePosition(newPos);
	}

	private void Update() 
	{
		if (!_grabbable.IsFlung) return;
		if (!_isFlying) Throw(_grabbable.ThrowForce);

		if (HasLanded())
		{
			StopMovement();
		}
	}

	public void Throw(Vector2 throwForce)
	{
		_grabbable.IsFlung = true;
		_isFlying = true;
		_rb.isKinematic = false;
		_rb.velocity = Vector2.zero;
		_rb.AddForce(throwForce, ForceMode2D.Impulse);
	}

	private bool HasLanded()
	{
		return transform.position.y <= RandomizeLanding();
	}

	private float RandomizeLanding()
	{
		return Mathf.Clamp(_grabbable.GrabbedPosY + Random.Range(-1f, 1f), 
							CoreHelper.groundBottomY, 
							CoreHelper.groundTopY);
	}

	public void StopMovement()
	{
		_isFlying = false;
		_grabbable.IsFlung = false;
		_rb.isKinematic = true;
		_rb.velocity = Vector2.zero;
		_rb.angularVelocity = 0f;
	}

	private void OnDestroy() 
	{
		_grabbable.OnSetPosition -= OnSetPositionCallback;
	}
}
