using System;
using TMPro;
using UnityEngine;

namespace BB.Resources
{
	public class Pearls : MonoBehaviour
	{
		[SerializeField] GameObject pearlPrefab;
		private int _pearlCount;
		private TextMeshProUGUI _pearlCountText;
		void Start()
		{
			_pearlCount = 0;
			_pearlCountText = transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();

			EnemyDeadState.EnemyDeadEvent += HandleEnemyDeath;
			GameStateManager.GameRestarted += ResetScore;
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

		private void HandleEnemyDeath(Vector2 deathPoint, object sender, EventArgs e)
		{
			Add(100);
		}

		void Update()
		{
			_pearlCountText.text = $"x{_pearlCount}";
		}

		private void OnDestroy()
		{
			EnemyDeadState.EnemyDeadEvent -= HandleEnemyDeath;
		}
	}
}

