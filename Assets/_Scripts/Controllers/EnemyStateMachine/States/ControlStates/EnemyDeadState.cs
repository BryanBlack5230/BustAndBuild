using System;
using System.Collections;
using UnityEngine;

public class EnemyDeadState : EnemyBaseState
{
	public EnemyDeadState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory){}
	public override void CheckSwitchStates()
	{
		if (!_context.IsDead)
			SwitchState(_factory.Aware());
	}

	public delegate void EnemyDeadEventHandler(Vector2 deathPoint, object sender, EventArgs e);
    public static event EnemyDeadEventHandler EnemyDeadEvent;

	public override void EnterState()
	{
		Vector2 deathPoint = _context.transform.position;
		OnEnemyDead(deathPoint);
		_context.Mover.Cancel();
		_context.RB.isKinematic = true;
		_context.transform.GetChild(0).gameObject.SetActive(false);
		_context.Audio.Play("death");
		ReturnToPool();
		// StartCoroutine(PlayDeatAudio());//TODO
		
		IEnumerator PlayDeatAudio() 
		{
			while (_context.Audio.isPlaying){
				yield return null;
			}
			ReturnToPool();
		}
	}

	public override void ExitState()
	{
		_context.IsAware = true;
		_context.IsSuspicious = false;
		_context.Health.Revive();
		_context.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);//doubles with lines in SpawnManager
		_context.GrabbedPosY = _context.transform.position.y;
		_context.transform.GetChild(0).gameObject.SetActive(true);
	}

	public override void InitializeSubState(){}

	public override void OnAttackEvent(){}

	public override void OnCollisionEnter2D(Collision2D other){}

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

	private Vector3 RandomPosition()
	{
		return new Vector3(-11f, UnityEngine.Random.Range(-4f, -1.56f), 0);
	}

	protected virtual void OnEnemyDead(Vector2 deathPoint)
    {
        EnemyDeadEvent?.Invoke(deathPoint, this, EventArgs.Empty);
    }
}
