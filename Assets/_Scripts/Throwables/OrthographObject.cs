using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class OrthographObject : MonoBehaviour
{
	private Vector2 _originalSize;
	private float _grabbedPosY;
	private Rigidbody2D _rb;
	private Transform _movingTarget;
	public event Action OnLanded;
	public bool IsOnGround {get; private set;}

	private void Update() 
	{
		if (IsOnGround) return;

		if (HasLanded())
			Landed();
	}
	public void Init(Transform movingTarget)
	{
		_movingTarget = movingTarget;
		_originalSize = _movingTarget.localScale;

		_rb = _movingTarget.GetComponent<Rigidbody2D>();
	}

	public void Throw(Vector2 impulse, float grabbedPosY)
	{
		IsOnGround = false;
		_grabbedPosY = grabbedPosY;

		_rb.isKinematic = false;
		_rb.velocity = Vector2.zero;
		_rb.angularVelocity = 0f;

		_rb.AddForce(impulse, ForceMode2D.Impulse);
	}

	public void Landed()
	{
		IsOnGround = true;
		StopMovement();

		OnLanded?.Invoke();
	}

	public void StopMovement()
	{
		_rb.isKinematic = true;
		_rb.velocity = Vector2.zero;
		_rb.angularVelocity = 0f;
	}
	
	public Vector2 ChangeSize()
	{
		float depthModifier = CoreHelper.GetDepthModifier(transform.position.y);

		return new Vector2(_originalSize.x * depthModifier, _originalSize.y * depthModifier);
	}

	public Vector2 GetOriginalSize()
	{
		return _originalSize;
	}

	private float RandomizeLanding()
	{
		return Mathf.Clamp(_grabbedPosY + UnityEngine.Random.Range(-1f, 1f), 
							CoreHelper.groundBottomY, 
							CoreHelper.groundTopY);
	}

	private bool HasLanded()
	{
		return transform.position.y <= RandomizeLanding();
	}
}
