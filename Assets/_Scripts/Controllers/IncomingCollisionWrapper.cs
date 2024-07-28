using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncomingCollisionWrapper : MonoBehaviour
{
	private EnemyStateMachine _parent;
	private void OnTriggerEnter2D(Collider2D other) 
	{
		if (_parent == null)
		_parent = transform.parent //BodyPivot
					.transform.parent//GFX
					.transform.parent//Enemy
					.GetComponent<EnemyStateMachine>();

		if (other.gameObject.CompareTag("Enemy"))
			_parent.OnIncomingCollisionTrigger(other);
	}

	private EnemyStateMachine FindParent()
	{
		Transform parent = transform.parent;
		while (parent != null)
		{
			EnemyStateMachine enemyStateMachine = parent.GetComponent<EnemyStateMachine>();
			if (enemyStateMachine != null)
			{
				return enemyStateMachine;
			}
			parent = parent.parent;
		}
		return null;
	}
}
