using System;
using UnityEngine;

public class EnemyOutOfScreen : MonoBehaviour
{
	[SerializeField] GameObject warning;
	private int _counter;

	private void OnEnable() {
		GameStateManager.GameRestarted += Reset;
	}

	private void OnDisable() {
		GameStateManager.GameRestarted -= Reset;
	}
	
	private void OnTriggerEnter2D(Collider2D other) {
		if (other.TryGetComponent<EnemyStateMachine>(out EnemyStateMachine enemy))
		{
			string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
			Debug.Log($"[{timeStamp}];[murlock got into village]");
			_counter++;
			if (_counter > 1)
			{
				warning.SetActive(false);
				// GameStateManager.Instance.GameOver();
			}
			
			enemy.ReturnToPool();
			warning.SetActive(true);
		}
	}

	private void Reset()
	{
		_counter = 0;
	}
}
