using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncomingCollisionWrapper : MonoBehaviour
{
	private EnemyStateMachine _parent;
	private void OnTriggerEnter2D(Collider2D other) 
	{
		if (_parent == null)
			_parent = transform.parent.transform.parent.GetComponent<EnemyStateMachine>();

		if (other.gameObject.CompareTag("Enemy"))
			_parent.OnIncomingCollisionTrigger(other);
	}
}
