using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Resources;

public class BrazierController : MonoBehaviour
{
	[SerializeField] Color activeColor;
	[SerializeField] Color inactiveColor;
	[SerializeField] GameObject sphere;
	[SerializeField] GameObject brazierLight;
	[SerializeField] GameObject brazierBase;
	[SerializeField] GameObject brazierDestroyed;
	private Health _health;
	private bool isDead;
	private void Start() 
	{
		_health = GetComponent<Health>();
		Reset();
	}
	private void Update() 
	{
		if (isDead == _health.IsDead()) return;

		isDead = _health.IsDead();
		if (isDead)
		{
			brazierBase.SetActive(false);
			brazierLight.SetActive(false);
			brazierDestroyed.SetActive(true);
			sphere.GetComponent<SpriteRenderer>().color = inactiveColor;
			GameStateManager.Instance.GameOver();
		}
	}

	private void OnEnable() {
		GameStateManager.GameRestarted += Reset;
	}

	private void OnDisable() {
		GameStateManager.GameRestarted -= Reset;
	}

	private void Reset()
	{
		_health.Revive();
		isDead = false;

		brazierBase.SetActive(true);
		brazierLight.SetActive(true);
		brazierDestroyed.SetActive(false);
		sphere.GetComponent<SpriteRenderer>().color = activeColor;
	}

	
}
