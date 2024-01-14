using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWalkingState : EnemyBaseState
{
	public EnemyWalkingState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory){}
	private Vector2 _targetPos;
	private float _currentSpeed;

	public override void CheckSwitchStates()
	{
		if (_context.IsNearTarget)
			SetSubState(_factory.Attack());
	}

	public override void EnterState()
	{
		((IRootStateEnemy)_currentSuperState).SelectTarget();
		StartWalking();
	}

	public override void ExitState(){}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		_context.Mover.MoveTo(_targetPos, _currentSpeed);
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
		// transform.rotation = _originalRotation;

		_targetPos = GetTargetPos(_context.Target.transform);
		_context.Mover.StartMoveAction(_targetPos, _currentSpeed);
	}

	private Vector2 GetTargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(_context.transform.position.y) : target.position;
	}

	private bool CloseToTarget()
	{
		return Vector3.Distance(_targetPos, _context.transform.position) < _context.AttackRange;
	}

	private void UpdateSizeAndSpeedByDepth()
	{
		float depthModifier = CoreHelper.GetDepthModifier(_context.transform.position.y);

		_context.transform.localScale = new Vector2(_context.OriginalSize.x * depthModifier,
													_context.OriginalSize.y * depthModifier);

		_currentSpeed = _context.Mover.MaxSpeed * depthModifier;
	}
}
