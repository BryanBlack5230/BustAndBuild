using System;
using TMPro;
using UnityEngine;

namespace BB.Resources
{
	public class Pearls : MonoBehaviour
	{
		[SerializeField] CollectiblePearlManager collectiblePearlManager;
		private int _pearlCount;
		private TextMeshProUGUI _pearlCountText;
		void Start()
		{
			_pearlCount = 0;
			_pearlCountText = transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
		}

		private void ResetScore()
		{
			_pearlCount = 0;
		}

		public bool Take(int amount)
		{
			if (_pearlCount - amount < 0) return false;

			_pearlCount -= amount;
			return true;
		}

		public void Add(int amount)
		{
			_pearlCount += amount;
		}

		private void Collected()
		{
			Add(10);
		}

		private void HandleEnemyDeath(Vector2 deathPoint, Vector2 inertia, float landPosY, object sender, EventArgs e)
		{
			collectiblePearlManager.SpawnPearls(UnityEngine.Random.Range(9, 11), inertia, deathPoint, landPosY);
		}

		void Update()
		{
			_pearlCountText.text = $"x{_pearlCount}";
		}

		private void OnEnable() 
		{
			EnemyDeadState.EnemyDeadEvent += HandleEnemyDeath;
			GameStateManager.GameRestarted += ResetScore;
			collectiblePearlManager.OnCollected += Collected;
		}

		private void OnDisable()
		{
			EnemyDeadState.EnemyDeadEvent -= HandleEnemyDeath;
			GameStateManager.GameRestarted += ResetScore;
			collectiblePearlManager.OnCollected -= Collected;
		}
	}
}

