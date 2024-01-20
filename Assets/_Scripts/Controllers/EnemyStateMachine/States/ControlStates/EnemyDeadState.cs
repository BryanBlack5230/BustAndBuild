using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeadState : EnemyBaseState
{
	public EnemyDeadState(EnemyStateMachine currentContext, EnemyStateFactory factory)
	: base (currentContext, factory){}
	public override void CheckSwitchStates()
	{
		if (!_context.IsDead)
			SetSubState(_factory.Walk());
	}

	public override void EnterState()
	{
		_context.Mover.Cancel();
		_context.RB.isKinematic = true;
		_context.transform.GetChild(0).gameObject.SetActive(false);
		_context.Audio.Play("death");
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
		_context.Health.Revive();
		_context.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);//doubles with lines in SpawnManager
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
		return new Vector3(-11f, Random.Range(-4f, -1.56f), 0);
	}
}
