using System;
using System.Collections;
using System.Collections.Generic;
using BB.Resources;
using UnityEngine;
using UnityEngine.Pool;

public class SpawnManager : MonoBehaviour
{

	[SerializeField] float minSpawnRate = 0.5f;
	[SerializeField] float maxSpawnRate = 3f;
	[SerializeField] int minEnemiesPerSpawn = 1;
	[SerializeField] int maxEnemiesPerSpawn = 5;
	[SerializeField] EnemyStateMachine enemyPrefab;
	[SerializeField] GameObject enemyContainer;
	[SerializeField] float beginWithAmount = 2f;
	private ObjectPool<EnemyStateMachine> _pool;
	private Vector2 _spawnPoint;
	private Coroutine _spawnCoroutine;

	void Start()
	{
		_spawnPoint = enemyContainer.transform.parent.transform.position;
		_pool = new ObjectPool<EnemyStateMachine>(CreateEnemy, OnTakeEnemyFromPool, OnReturnEnemyToPool, 
			enemy => 
			{
				Destroy(enemy.gameObject);
			}, 
			false, 
			30);
		
		// InvokeRepeating(nameof(Spawn), 0.2f, spawnRate);

		// SpawnBatch();
	}

	private void Spawn()
	{
		_pool.Get();
	}

	private EnemyStateMachine CreateEnemy()
	{
		var enemy = Instantiate(enemyPrefab);
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[{enemy.GetInstanceID().ToString()}];was born");
		enemy.SetPool(_pool);
		enemy.Castle = ConstantTargets.CastleWall.GetComponent<Health>();
		enemy.transform.parent = enemyContainer.transform;
		enemy.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);
		return enemy;
	}

	private void OnTakeEnemyFromPool(EnemyStateMachine enemy)
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[{enemy.ID}];was revived");
		enemy.gameObject.SetActive(true);
		enemy.transform.SetPositionAndRotation(RandomPosition(), Quaternion.identity);
		enemy.Revive();
	}

	private void OnReturnEnemyToPool(EnemyStateMachine enemy)
	{
		Debug.Log($"[{CoreHelper.TimeNow()}];[{GetType().Name}];[{enemy.ID}];has returned to it's pool");
		enemy.gameObject.SetActive(false);
	}

	public void Reset()
	{
		_pool.Clear();
	}

	private Vector3 RandomPosition()
	{
		return new Vector3(ConstantTargets.Sea.position.x + UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-4f, -1.56f), 0);
	}

	private void SpawnBatch()
	{
		for (int i = 0; i < beginWithAmount; i++)
		{
			Spawn();
		}
	}

	public void StartSpawning()
	{
		if (_spawnCoroutine == null)
			_spawnCoroutine = StartCoroutine(SpawnCoroutine());
	}

	public void StopSpawning()
	{
		if (_spawnCoroutine != null)
		{
			StopCoroutine(_spawnCoroutine);
			_spawnCoroutine = null;
		}
	}

	public void ChangeSpawnRate(float minSpawn, float maxSpawn, int minEnemies, int maxEnemies)
	{
		minSpawnRate = minSpawn;
		maxSpawnRate = maxSpawn;
		minEnemiesPerSpawn = minEnemies;
		maxEnemiesPerSpawn = maxEnemies;
	}

	private IEnumerator SpawnCoroutine()
	{
		while (true)
		{
			float randomSpawnRate = UnityEngine.Random.Range(minSpawnRate, maxSpawnRate);
			yield return new WaitForSeconds(randomSpawnRate);

			int enemyCount = UnityEngine.Random.Range(minEnemiesPerSpawn, maxEnemiesPerSpawn + 1);
			for (int i = 0; i < enemyCount; i++)
			{
				Spawn();
			}
		}
	}

	private void OnEnable() 
	{
		GameStateManager.GameRestarted += StopSpawning;
	}

	private void OnDisable() 
	{
		GameStateManager.GameRestarted -= StopSpawning;
	}
}
