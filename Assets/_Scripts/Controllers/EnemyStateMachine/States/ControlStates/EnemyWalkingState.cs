using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using DG.Tweening;
using System;

public class EnemyWalkingState : EnemyBaseState
{
	public EnemyWalkingState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName){}
	private Vector2 _targetPos;
	private float _currentSpeed;
	private float _backwardModifyier = 0.8f;
	private float _currentYPos;

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
		_currentYPos = _context.transform.position.y;
		UpdateSizeAndSpeedByDepth();
		StartWalking();
	}

	public override void ExitState(){}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		if (DepthChanged())
		{
			UpdateSizeAndSpeedByDepth();
		}

		_targetPos = GetTargetPos(_context.Target.transform);
		var dist = Vector3.Distance(_targetPos, _context.transform.position);
		Vector2 moveToPos;
		if (dist <= _context.AttackRange * 0.5f)
		{
			moveToPos = _targetPos - _context.AttackRange * 0.5f * Vector2.right;
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

    private bool DepthChanged()
    {
		if (_currentYPos == _context.transform.position.y) return false;

		_currentYPos = _context.transform.position.y;
		return true;
    }

    private void StartWalking()
	{
		_context.RB.velocity = Vector2.zero;
		_context.RB.isKinematic = true;
		_context.RB.angularVelocity = 0f;
		_context.RB.SetRotation(Quaternion.identity);
		// _context.Animator.SetTrigger(_context.WalkingTriggerCached);

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

	private void WalkingAnimation()
	{
		float bobbingHeight = 0.2f;
		float bobbingSpeed = 1f;
		Sequence _subStateSequence = DOTween.Sequence();
        float maxYScale = 1f;
        float minYScale = 1f * (1 - bobbingHeight);

        // _subStateSequence
        //     .Append(body.DOScaleY(minYScale, bobbingSpeed).SetEase(Ease.InOutQuad))
        //     .Append(body.DOScaleY(maxYScale, bobbingSpeed).SetEase(Ease.InOutQuad))
        //     .SetLoops(-1, LoopType.Yoyo);
	}

	public override void OnBodyCollision(Collision2D other){}
	public override void OnIncomingCollisionTrigger(Collider2D other){}
	public override void OnAttackEvent(){}
}
