using System;
using System.Collections;
using UnityEngine;

public class EnemyDeadState : EnemyBaseState, IRootStateEnemy
{
	public EnemyDeadState(EnemyStateMachine currentContext, EnemyStateFactory factory, EnemyStates stateName)
	: base (currentContext, factory, stateName)
	{
		_isRootState = true;
	}
	public override void CheckSwitchStates()
	{
		if (!_context.IsDead)
			SwitchState(_factory.Aware());
	}

	public delegate void EnemyDeadEventHandler(Vector2 deathPoint, Vector2 inertia, float grabbedPosY, object sender, EventArgs e);
	public static event EnemyDeadEventHandler EnemyDeadEvent;

	public override void EnterState()
	{
		if (!_context.IsReturningToPool)
		{
			Debug.Log($"{_debugInfo};is now dead");
			Vector2 deathPoint = _context.transform.position;
			SoundManager.Instance.PlaySound(_context.AudioList.GetClip(SoundType.Death));
			OnEnemyDead(deathPoint);
		} else
		{
			Debug.Log($"{_debugInfo};is returned to pool without dying");
			_context.IsReturningToPool = false;
		}
		
		
		_context.Mover.Cancel();
		_context.RB.isKinematic = true;
		_context.transform.GetChild(0).gameObject.SetActive(false);
		
		ReturnToPool();
	}

	public override void ExitState()
	{
		_context.IsAware = true;
		_context.IsSuspicious = false;
		_context.Health.Revive();
		// _context.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);//doubles with lines in SpawnManager
		_context.GrabbedPosY = _context.transform.position.y;
		_context.transform.GetChild(0).gameObject.SetActive(true);
		Debug.Log($"{_debugInfo};is now alive");
	}

	public override void InitializeSubState(){}

	public override void OnAttackEvent(){}

	public override void OnBodyCollision(Collision2D other){}
	public override void OnIncomingCollisionTrigger(Collider2D other){}

	public override void UpdateState()
	{
		CheckSwitchStates();
	}

	private void ReturnToPool()
	{
		if (_context.Pool != null)
			_context.Pool.Release(_context);
		else
			_context.Destroy();
	}

	protected virtual void OnEnemyDead(Vector2 deathPoint)
	{
		EnemyDeadEvent?.Invoke(deathPoint, _context.RB.velocity, _context.GrabbedPosY, this, EventArgs.Empty);
	}

	public void ChangeColor(){}
	public void SelectTarget(){}
}
