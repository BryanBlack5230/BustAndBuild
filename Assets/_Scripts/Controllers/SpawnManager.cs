using System.Collections;
using System.Collections.Generic;
using BB.Resources;
using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{

	[SerializeField] float spawnRate = 1f;
	[SerializeField] AIController enemyPrefab;
	[SerializeField] GameObject enemyContainer;
	[SerializeField] Health target;
	[SerializeField] float beginWithAmount = 2f;
	[SerializeField] bool stopSpawning = false;
	private ObjectPool<AIController> _pool;

	void Start()
	{
		_pool = new ObjectPool<AIController>(CreateEnemy, OnTakeEnemyFromPool, OnReturnEnemyToPool, 
			enemy => 
			{
				Destroy(enemy.gameObject);
			}, 
			false, 
			30);
		
		InvokeRepeating(nameof(Spawn), 0.2f, spawnRate);
	}

	private void Spawn()
	{
		_pool.Get();
	}

	private AIController CreateEnemy()
	{
		var enemy = Instantiate(enemyPrefab);
		enemy.SetPool(_pool);
		enemy.transform.parent = enemyContainer.transform;
		return enemy;
	}

	private void OnTakeEnemyFromPool(AIController enemy)
	{
		enemy.gameObject.SetActive(true);
		enemy.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);
		enemy.SetTarget(target);
		enemy.SetAnimator(enemy.transform.GetChild(0).gameObject.GetComponent<Animator>());
		enemy.Revive();
	}

	private void OnReturnEnemyToPool(AIController enemy)
	{
		enemy.gameObject.SetActive(false);
	}

	private Vector3 RandomPosition()
	{
		return new Vector3(-11f, Random.Range(-4f, -1.56f), 0);
	}

	public void Reset()
	{
		_pool.Clear();
	}
}
