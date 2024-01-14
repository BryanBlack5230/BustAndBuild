using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderBounce : MonoBehaviour
{
	private EdgeCollider2D _collider;
	void Start()
	{
		_collider = GetComponent<EdgeCollider2D>();
	}

	private void OnCollisionEnter2D(Collision2D other) {
		Vector2 surfaceNormal = other.contacts[0].normal;
		Vector2 reflectedDirection = Vector2.Reflect(other.relativeVelocity, surfaceNormal);

		other.gameObject.GetComponent<Rigidbody2D>().AddForce(reflectedDirection, ForceMode2D.Impulse);
	}
}
