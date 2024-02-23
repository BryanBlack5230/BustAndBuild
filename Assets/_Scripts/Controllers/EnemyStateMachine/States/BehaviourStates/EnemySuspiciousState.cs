using System.Collections;
using System.Collections.Generic;
using BB.Resources;
using UnityEngine;

public class EnemySuspiciousState : EnemyBaseState, IRootStateEnemy
{
	public EnemySuspiciousState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName)
	{
		_isRootState = true;
	}

	private SpriteRenderer _leftEyeRenderer;
	private SpriteRenderer _rightEyeRenderer;
	private float _leftPupilHalfLenght;
	private float _rightPupilHalfLenght;
	private float _lefttEyeHalfLength;
	private float _rightEyeHalfLength;
	private Vector2 _leftPupilSize;
	private Vector2 _rightPupilSize;

	public override void CheckSwitchStates()
	{
		if (_context.IsAware)
			SwitchState(_factory.Aware(), _currentSubState);
	}

	public override void EnterState()
	{
		_context.StateSizeModifier = 0.8f;
		_context.StateSpeedModifier = 0.8f;
		_context.StateAttackSpeedCDModifier = 1.2f;
		_leftPupilSize = _context.Pupils.left.localScale;
		_rightPupilSize = _context.Pupils.right.localScale;
		SelectTarget();
		if (_currentSubState == null)
			InitializeSubState();
		ChangePupilSize(1.5f);
		GetEyeShape();
	}

	public override void ExitState()
	{
		_context.IsSuspicious = false;
		ChangePupilSize(1f);
	}

	public override void InitializeSubState()
	{
		if (_context.IsDead)
			SetSubState(_factory.Dead());
		else if (_context.IsGrabbed)
			SetSubState(_factory.Grabbed());
		else if (_context.IsFlung)
			SetSubState(_factory.Flung());
		else if (_context.IsNearTarget)
			SetSubState(_factory.Attack());
		else
			SetSubState(_factory.Walk());

		_currentSubState.EnterState();
	}

	public override void OnAttackEvent()	{_currentSubState?.OnAttackEvent();}

	public override void OnBodyCollision(Collision2D other)	{_currentSubState?.OnBodyCollision(other);}
	public override void OnIncomingCollisionTrigger(Collider2D other){ _currentSubState?.OnIncomingCollisionTrigger(other);}

	public void SelectTarget()
	{
		if (_context.transform.position.x > CoreHelper.GetWallPos(_context.transform.position.y).x + 1f || _context.Castle.IsDead())
			_context.Target = ConstantTargets.Brazier.GetComponent<Health>();
		else
			_context.Target = _context.Castle;//TODO
	}

	public override void UpdateState()
	{
		ChangeColor();
		EyeFollow();
		CheckSwitchStates();
	}

	public void ChangeColor()
	{
		var mat = _context.transform.GetChild(0)
							.transform.GetChild(11).GetComponent<Renderer>().material;
		
		if (mat == null)
			Debug.LogError($"{_debugInfo};[material is not set]");

		mat.SetColor("_ColorTop", _context.SuspiciousTopColor);
		mat.SetColor("_ColorBot", _context.SuspiciousBotColor);
		mat.SetFloat("_BlendHeight", _context.Suspiciouslend);

		_context.ColorStart = _context.SuspiciousTopColor;
		_context.ColorEnd = _context.SuspiciousBotColor;
	}

	private void ChangePupilSize(float size)
	{
		_context.Pupils.left.localScale = _leftPupilSize * size;
		_context.Pupils.right.localScale = _rightPupilSize * size;
	}

	private void GetEyeShape()
	{
		_leftEyeRenderer = _context.Eyes.left.GetComponent<SpriteRenderer>();
		_rightEyeRenderer = _context.Eyes.right.GetComponent<SpriteRenderer>();

		Bounds leftEyeSpriteBounds = _leftEyeRenderer.bounds;
		Bounds rightEyeSpriteBounds = _rightEyeRenderer.bounds;

		_lefttEyeHalfLength = Mathf.Abs(leftEyeSpriteBounds.min.x - leftEyeSpriteBounds.max.x) * 0.5f;
		_rightEyeHalfLength = Mathf.Abs(rightEyeSpriteBounds.min.x - rightEyeSpriteBounds.max.x) * 0.5f;

		SpriteRenderer lefttPupilRend = _context.Pupils.left.GetComponent<SpriteRenderer>();
		SpriteRenderer rightPupilRend = _context.Pupils.right.GetComponent<SpriteRenderer>();

		Bounds leftPupilSpriteBounds = lefttPupilRend.bounds;
		Bounds rightPupilSpriteBounds = rightPupilRend.bounds;

		_leftPupilHalfLenght = Mathf.Abs(leftPupilSpriteBounds.min.x - leftPupilSpriteBounds.max.x) * 0.5f;
		_rightPupilHalfLenght = Mathf.Abs(rightPupilSpriteBounds.min.x - rightPupilSpriteBounds.max.x) * 0.5f;
	}

	private void EyeFollow()
	{
		Bounds leftEyeSpriteBounds = _leftEyeRenderer.bounds;
		Bounds rightEyeSpriteBounds = _rightEyeRenderer.bounds;

		Vector2 leftCenter = new Vector2((leftEyeSpriteBounds.min.x + leftEyeSpriteBounds.max.x)* 0.5f, 
										(leftEyeSpriteBounds.min.y + leftEyeSpriteBounds.max.y)* 0.5f);
		Vector2 rightCenter = new Vector2((rightEyeSpriteBounds.min.x + rightEyeSpriteBounds.max.x)* 0.5f, 
										(rightEyeSpriteBounds.min.y + rightEyeSpriteBounds.max.y)* 0.5f);

		Vector2 leftDirection = (leftCenter - CoreHelper.CursorPos());
		Vector2 rightDirection = (rightCenter - CoreHelper.CursorPos());

		float leftDistance = (leftDirection.magnitude > 5f) ? _lefttEyeHalfLength * 0.8f : _lefttEyeHalfLength * (leftDirection.magnitude / 6);
		float righttDistance = (rightDirection.magnitude > 5f) ? _rightEyeHalfLength * 0.8f : _rightEyeHalfLength * (rightDirection.magnitude / 6);

		_context.Pupils.left.position = leftCenter - leftDirection.normalized * (leftDistance - _leftPupilHalfLenght);
		_context.Pupils.right.position = rightCenter - rightDirection.normalized * (righttDistance - _rightPupilHalfLenght);
	}
}
