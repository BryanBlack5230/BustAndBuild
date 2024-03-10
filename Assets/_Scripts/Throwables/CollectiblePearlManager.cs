using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class CollectiblePearlManager : MonoBehaviour
{
	public CollectiblePearl pearlPrefab;
	public float spawnRange = 1f;
	public Transform uiTarget;
	public Vector2 collectSpot;
	public event Action OnCollected;

	private ObjectPool<CollectiblePearl> pearlPool;

	private void Start()
	{
		collectSpot = CoreHelper.camera.ViewportToWorldPoint(uiTarget.position);
		pearlPool = new ObjectPool<CollectiblePearl>(CreatePooledPearl, OnTakePearlFromPool, OnReturnPearlToPool, OnDestroyPearl, false, 500, 1500);
	}

	private CollectiblePearl CreatePooledPearl()
	{
		var pearl = Instantiate(pearlPrefab, transform);
		pearl.Manager = this;
		// pearl.gameObject.SetActive(false);
		return pearl;
	}

	private void OnTakePearlFromPool(CollectiblePearl pearl)
	{
		pearl.gameObject.SetActive(true);
		pearl.Reset();
	}

	private void OnReturnPearlToPool(CollectiblePearl pearl)
	{
		pearl.gameObject.SetActive(false);
	}

	private void OnDestroyPearl(CollectiblePearl pearl)
	{
		Destroy(pearl.gameObject);
	}

	public void Release(CollectiblePearl pearl)
	{
		pearlPool.Release(pearl);
	}

	public void CollectedCall()
	{
		OnCollected?.Invoke();
	}

	public void SpawnPearls(float count, Vector2 inertia, Vector2 spawnPos, float landPosY)
	{
		for (int i = 0; i < count; i++)
		{
			CollectiblePearl pearl = pearlPool.Get();
			pearl.transform.position = spawnPos;
			pearl.Throw(inertia, landPosY);
		}
	}

	private void Reset()
	{
		CollectiblePearl[] pearls = GetComponentsInChildren<CollectiblePearl>();
		foreach (var pearl in pearls)
		{
			pearl.ReturnToPool();
		}
	}

	private void OnEnable() 
	{
		GameStateManager.GameRestarted += Reset;
	}

	private void OnDisable() 
	{
		GameStateManager.GameRestarted -= Reset;
	}
}
