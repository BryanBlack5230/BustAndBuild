using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using DG.Tweening;

public class EnemyWalkingState : EnemyBaseState
{
	public EnemyWalkingState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName){}
	private Vector2 _targetPos;
	private float _currentSpeed;
	private float _backwardModifyier = 0.8f;

	public override void CheckSwitchStates()
	{
		if (_context.IsGrabbed)
			SwitchState(_factory.Grabbed());
		else if (_context.IsDead)
			SwitchState(_factory.Dead());
		else if (_context.IsNearTarget)
			SwitchState(_factory.Attack());
	}

	public override void EnterState()
	{
		((IRootStateEnemy)_currentSuperState).SelectTarget();
		_context.IsOnGround = true;
		_context.Shadow.DOFade(0.71f, 0.5f);
		StartWalking();
	}

	public override void ExitState(){}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		UpdateSizeAndSpeedByDepth();
		_targetPos = GetTargetPos(_context.Target.transform);
		var dist = Vector3.Distance(_targetPos, _context.transform.position);
		Vector2 moveToPos;
		if (dist <= _context.AttackRange * 0.5f)
		{
			moveToPos = _targetPos - Vector2.right * _context.AttackRange * 0.5f;
			_currentSpeed *= _backwardModifyier;
		}
		else
			moveToPos = _targetPos;

		_context.Mover.MoveTo(moveToPos, _currentSpeed);
		Debug.Log($"{_debugInfo};moving towards [{_context.Target.name}, {Vector3.Distance(_targetPos, _context.transform.position)}m] with [{_currentSpeed}/{_context.Mover.MaxSpeed}] speed");
		if (_currentSpeed > _context.Mover.MaxSpeed) Debug.LogWarning($"{_debugInfo}; exceeding speed limit with [{_currentSpeed}/{_context.Mover.MaxSpeed}] speed");
		if (CloseToTarget())
			_context.IsNearTarget = true;

		CheckSwitchStates();
	}

	private void StartWalking()
	{
		_context.RB.velocity = Vector2.zero;
		_context.RB.isKinematic = true;
		_context.RB.angularVelocity = 0f;
		_context.RB.SetRotation(Quaternion.identity);
		_context.Animator.SetTrigger(_context.WalkingTriggerCached);

		_targetPos = GetTargetPos(_context.Target.transform);
		_context.Mover.StartMoveAction(_targetPos, _currentSpeed);
	}

	private Vector2 GetTargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(_context.transform.position.y) : target.position;
	}

	private bool CloseToTarget()
	{
		var dist = Vector3.Distance(_targetPos, _context.transform.position);
		float epsilon = 0.001f;
		float plus = _context.AttackRange + epsilon;
		float minus = _context.AttackRange * 0.5f - epsilon;
		return (dist <= _context.AttackRange + epsilon) 
			&& (dist >= _context.AttackRange * 0.5f - epsilon);
	}

	private void UpdateSizeAndSpeedByDepth()
	{
		float depthModifier = CoreHelper.GetDepthModifier(_context.transform.position.y);

		_context.transform.localScale = new Vector2(_context.OriginalSize.x * depthModifier,
													_context.OriginalSize.y * depthModifier * _context.StateSizeModifier);

		_currentSpeed = _context.Mover.MaxSpeed * depthModifier * _context.StateSpeedModifier;
	}

	public override void OnBodyCollision(Collision2D other){}
	public override void OnIncomingCollisionTrigger(Collider2D other){}
	public override void OnAttackEvent(){}
}
