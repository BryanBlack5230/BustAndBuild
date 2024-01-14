using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationEventWrapper : MonoBehaviour
{
	private AIController _parent;
	private void Start() 
	{
		_parent = transform.parent.GetComponent<AIController>();
	}
	public void Attack()
	{
		_parent.OnAttackEvent();
	}
}
