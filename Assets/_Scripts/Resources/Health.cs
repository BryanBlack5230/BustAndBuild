using UnityEngine;
using BB.Stats;
using BB.Core;


namespace BB.Resources
{
	public class Health : MonoBehaviour
	{
		[SerializeField] float health = 100f;
		public float maxHealth {get; private set;}
		private int cashedDeath = Animator.StringToHash("die");
		private bool isDead = false;
		public bool Killable = true;

		private void Start()
		{
			
			if (TryGetComponent<BaseStats>(out var bs))
				maxHealth = bs.GetHealth();
			else
			{
				Debug.LogWarning($"{gameObject.name}; does not have BaseStats component");
				maxHealth = health;
			}
			health = maxHealth;
		}

		public bool IsDead()
		{
			return isDead;
		}

		public void TakeDamage(GameObject instigator, float damage)
		{
			if (!Killable) return;

			health = Mathf.Max(health - damage, 0);
			if (health == 0)
			{
				Die();
			}
		}

		public void TakeDamage(float damage)
		{
			if (!Killable) return;
			
			health = Mathf.Max(health - damage, 0);
			if (health == 0)
			{
				Die();
			}
		}

		public float GetPercentage()
		{
			return health / maxHealth;
		}

		public float GetCurrent()
		{
			return health;
		}

		private void Die()
		{
			if (isDead) return;

			isDead = true;
			GetComponent<ActionScheduler>()?.CancelCurrentAction();
		}

		public void Revive()
		{
			isDead = false;
			health = maxHealth;
		}
	}
}
