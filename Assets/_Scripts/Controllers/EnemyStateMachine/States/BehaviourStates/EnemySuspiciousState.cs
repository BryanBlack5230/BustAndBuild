using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySuspiciousState : EnemyBaseState, IRootStateEnemy
{
	public EnemySuspiciousState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName)
	{
		_isRootState = true;
	}



	public override void CheckSwitchStates()
	{
		if (_context.IsAware)
			SwitchState(_factory.Aware(), _currentSubState);
	}

	public override void EnterState()
	{
		SelectTarget();
		if (_currentSubState == null)
			InitializeSubState();
	}

	public override void ExitState()
	{
		_context.IsSuspicious = false;
		// _currentSubState?.ExitState();
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

	public override void OnCollisionEnter2D(Collision2D other)	{_currentSubState?.OnCollisionEnter2D(other);}

	public void SelectTarget()
	{
		_context.Target = _context.Castle;//TODO
	}

	public override void UpdateState()
	{
		ChangeColor();
		CheckSwitchStates();
	}

	public void ChangeColor()
	{
		var mat = _context.transform.GetChild(0)
							.transform.GetChild(5).GetComponent<Renderer>().material;
		
		mat.SetColor("_ColorTop", _context.SuspiciousTopColor);
		mat.SetColor("_ColorBot", _context.SuspiciousBotColor);
		mat.SetFloat("_BlendHeight", _context.Suspiciouslend);
	}
}
