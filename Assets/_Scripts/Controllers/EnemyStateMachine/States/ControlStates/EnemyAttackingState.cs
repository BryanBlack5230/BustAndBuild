using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackingState : EnemyBaseState
{
	public EnemyAttackingState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName)	{}
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

	public override void EnterState()
	{
		_context.IsometricHandler.SetIsometricObjectUpdate(false);
		_context.IsOnGround = true;
	}

	public override void ExitState()
	{
		_context.Animator.ResetTrigger(_context.AttackTriggerCached);
		_context.Animator.StopPlayback();
		_timeSinceLastAttack = 0;
		_context.IsNearTarget = false;
		_context.IsometricHandler.SetIsometricObjectUpdate(true);
	}

	public override void InitializeSubState(){}

	public override void UpdateState()
	{
		float attackCoolDown = _context.AttackCooldown * _context.StateAttackSpeedCDModifier;
		Debug.Log($"{_debugInfo};attacking [{_context.Target.name}], coolDown[{_timeSinceLastAttack}/{attackCoolDown}]s");
		_timeSinceLastAttack += Time.deltaTime;

		if (_timeSinceLastAttack > attackCoolDown)
			Attack();
		
		if (_context.Target.IsDead())
			((IRootStateEnemy)_currentSuperState).SelectTarget();
		
		CheckDistanceToTarget();
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
		var hitPos = CoreHelper.GetWallPos(_context.transform.position.y);
		var hitParticles = Object.Instantiate(_context.WallHitParticles, hitPos, Quaternion.identity);
		var ps = hitParticles.GetComponent<ParticleSystem>();
		Object.Destroy(hitParticles, ps.main.duration);
		SoundManager.Instance.PlaySound(_context.AudioList.GetClip(SoundType.Attack));
	}

	private void CheckDistanceToTarget()
	{
		var dist = Vector3.Distance(GetTargetPos(_context.Target.transform), _context.transform.position);
		float epsilon = 0.001f;
		bool inRange = (dist <= _context.AttackRange + epsilon) 
			&& (dist >= _context.AttackRange * 0.5f - epsilon);
		if (!inRange)
			_context.IsNearTarget = false;
	}

	private Vector2 GetTargetPos(Transform target)
	{
		return target.CompareTag("Castle") ? CoreHelper.GetWallPos(_context.transform.position.y) : target.position;
	}

	public override void OnBodyCollision(Collision2D other){}
	public override void OnIncomingCollisionTrigger(Collider2D other){}
}
