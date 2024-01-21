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

		private void Start()
		{
			maxHealth = GetComponent<BaseStats>().GetHealth();
			health = maxHealth;
		}

		public bool IsDead()
		{
			return isDead;
		}

		public void TakeDamage(GameObject instigator, float damage)
		{
			health = Mathf.Max(health - damage, 0);
			if (health == 0)
			{
				Die();
				//AwardExpirience(instigator);
			}
		}

		public void TakeDamage(float damage)
		{
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
			// GetComponent<Animator>().SetTrigger(cashedDeath);
			GetComponent<ActionScheduler>()?.CancelCurrentAction();
			// gameObject.GetComponent<AIController>().StopAllCoroutines();
			// gameObject.GetComponent<AIController>().ReturnToPool();

		}

		public void Revive()
		{
			isDead = false;
			health = maxHealth;
		}

		// private void AwardExpirience(GameObject instigator)
		// {
		//     Experience experience = instigator.GetComponent<Experience>();
		//     if (experience == null) return;

		//     experience.GainExperience(GetComponent<BaseStats>().GetExperienceReward());
		// }
	}
}
