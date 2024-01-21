using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAwareState : EnemyBaseState, IRootStateEnemy
{
	public EnemyAwareState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory)
	{
		_isRootState = true;
	}
	public override void CheckSwitchStates()
	{
		if (_context.IsSuspicious)
			SwitchState(_factory.Suspicious());
	}

	public override void EnterState()
	{
		ChangeColor();
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

	public override void OnAttackEvent(){_currentSubState?.OnAttackEvent();}

	public override void OnCollisionEnter2D(Collision2D other){ _currentSubState?.OnCollisionEnter2D(other);}

	public void SelectTarget()
	{
		_context.Target = _context.Castle;//TODO
	}

	public override void UpdateState()
	{
		CheckSwitchStates();
	}

	public void ChangeColor()
	{
		var mat = _context.transform.GetChild(0)
							.transform.GetChild(5).GetComponent<Renderer>().material;
		
		mat.SetColor("_ColorTop", _context.AwareTopColor);
		mat.SetColor("_ColorBot", _context.AwareBotColor);
		mat.SetFloat("_BlendHeight", _context.AwareBlend);
	}
}
