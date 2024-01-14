using System.Collections;
using System.Collections.Generic;
using BB.Resources;
using Unity.VisualScripting;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
	[SerializeField] GameObject gameOverPopup;
	[SerializeField] Health castle;
	[SerializeField] GameObject enemyPool;
	void Start()
	{
		gameOverPopup.SetActive(false);
	}

	void Update()
	{
		if (castle.IsDead())
		{
			gameOverPopup.SetActive(true);
			Time.timeScale = 0;
		}
	}

	public void Restart()
	{
		gameOverPopup.SetActive(false);
		Time.timeScale = 1;
		castle.Revive();
		
		foreach (var enemy in enemyPool.GetComponentsInChildren<AIController>())
		{
			enemy.ReturnToPool();
		}
	}
}
