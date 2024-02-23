using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyCollisionWrapper : MonoBehaviour
{
    private EnemyStateMachine _parent;
	private void OnCollisionEnter2D(Collision2D other) 
	{
		if (_parent == null)
			_parent = transform.parent.transform.parent.GetComponent<EnemyStateMachine>();

		if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Border"))
			_parent.OnBodyCollision(other);
	}
}
