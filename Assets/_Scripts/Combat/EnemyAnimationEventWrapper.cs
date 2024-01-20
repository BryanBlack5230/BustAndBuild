using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationEventWrapper : MonoBehaviour
{
	private EnemyStateMachine _parent;
	private void Start() 
	{
		_parent = transform.parent.GetComponent<EnemyStateMachine>();
	}
	public void Attack()
	{
		_parent.OnAttackEvent();
	}
}
