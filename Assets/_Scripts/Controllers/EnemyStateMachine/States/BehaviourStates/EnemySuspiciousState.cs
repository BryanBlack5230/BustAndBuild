using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySuspiciousState : EnemyBaseState, IRootStateEnemy
{
	public EnemySuspiciousState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory)
	{
		_isRootState = true;
	}
	public override void CheckSwitchStates()
	{
		if (_context.IsAware)
			SwitchState(_factory.Aware());
	}

	public override void EnterState()
	{
		SelectTarget();
		InitializeSubState();
	}

	public override void ExitState()
	{
	}

	public override void InitializeSubState()
	{
		_currentSubState?.ExitState();

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

	public override void OnCollisionEnter2D(Collision2D other)	{_currentSubState?.OnCollisionEnter2D(other);}

	public void SelectTarget()
	{
		_context.Target = _context.Castle;//TODO
	}

	public override void UpdateState()
	{
		CheckSwitchStates();
	}
}
