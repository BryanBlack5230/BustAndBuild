using UnityEngine;

namespace BB.Stats
{
	[CreateAssetMenu(fileName = "Progression", menuName = "Stats/New Progression", order = 0)]
	public class Progression : ScriptableObject
	{
		[SerializeField] ProgressionCharacterClass[] characterClasses = null;

		public float GetHealth(CharacterType characterClass, int level)
		{
			foreach (ProgressionCharacterClass progressionClass in characterClasses)
			{
				if (progressionClass.characterClass == characterClass)
				{
					return progressionClass.health[level - 1];
				}
			}
			return 0;
		}

		public float GetStat(CharacterType characterClass, int level, Stat targetStat)
		{
			foreach (ProgressionCharacterClass progressionClass in characterClasses)
			{
				if (progressionClass.characterClass == characterClass)
				{
					foreach (ProgressionStat progressionStat in progressionClass.stats)
					{
						if (progressionStat.stat == targetStat)
							return progressionStat.levels[level - 1];
					}
				}
			}
			return 0;
		}

		[System.Serializable]
		class ProgressionCharacterClass
		{
			public CharacterType characterClass;
			public float[] health;
			public ProgressionStat[] stats;
		}

		[System.Serializable]
		class ProgressionStat
		{
			public Stat stat;
			public float[] levels;
		}
	}
}
