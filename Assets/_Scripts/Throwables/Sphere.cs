using System.Collections;
using System.Collections.Generic;
using BB.Combat;
using UnityEngine;

public class Sphere : MonoBehaviour
{
	private Rigidbody2D _rb;
	private Grabbable _grabbable;
	private OrthographObject _orthographObj;
	
	private void Awake() 
	{
		_rb = GetComponent<Rigidbody2D>();
		_grabbable = GetComponent<Grabbable>();
		_orthographObj = GetComponent<OrthographObject>();
	}

	private void Start() 
	{
		_orthographObj.Init(transform);
	}

	private void OnSetPositionCallback(Vector2 newPos)
	{
		_rb.MovePosition(newPos);
	}

	public void Throw(Vector2 throwForce)
	{
		_orthographObj.Throw(throwForce, _grabbable.GrabbedPosY);
		_grabbable.IsFlung = true;
	}

	public void CallLanded()
	{
		_orthographObj.Landed();
	}

	public void StopMovement()
	{
		_grabbable.IsFlung = false;
	}

	private void OnEnable() 
	{
		_grabbable.OnSetPosition += OnSetPositionCallback;
		_grabbable.OnThrow += Throw;
		_orthographObj.OnLanded += StopMovement;
	}

	private void OnDisable() 
	{
		_grabbable.OnSetPosition -= OnSetPositionCallback;
		_grabbable.OnThrow -= Throw;
		_orthographObj.OnLanded -= StopMovement;
	}
}
