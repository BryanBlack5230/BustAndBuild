using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackingState : EnemyBaseState
{
	public EnemyAttackingState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory)	{}
	private float _timeSinceLastAttack = Mathf.Infinity;
	public override void CheckSwitchStates()
	{
		if (_context.IsGrabbed)
			SwitchState(_factory.Grabbed());
		else if (_context.IsDead)
			SwitchState(_factory.Dead());
		else if (!_context.IsNearTarget)
			SwitchState(_factory.Walk());
	}

	public override void EnterState(){}

	public override void ExitState()
	{
		_context.Animator.ResetTrigger(_context.AttackTriggerCached);
		_context.Animator.StopPlayback();
		_timeSinceLastAttack = 0;
		_context.IsNearTarget = false;
	}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		_timeSinceLastAttack += Time.deltaTime;

		if (_timeSinceLastAttack > _context.AttackCooldown)
			Attack();
		
		if (_context.Target.IsDead())
			((IRootStateEnemy)_currentSuperState).SelectTarget();
		
		CheckSwitchStates();
	}

	private void Attack()
	{
		_context.Animator.SetTrigger(_context.AttackTriggerCached);
		_timeSinceLastAttack = 0;
	}

	public override void OnAttackEvent()
	{
		_context.Target.TakeDamage(_context.AttackDamage);
		_context.Audio.Play("attack");
	}

	public override void OnCollisionEnter2D(Collision2D other)
	{
		throw new System.NotImplementedException();
	}
}
