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
		InitializeSubState();
		SelectTarget();
	}

	public override void ExitState()
	{
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
	}

	public void SelectTarget()
	{
	}

	public override void UpdateState()
	{
		CheckSwitchStates();
	}
}
