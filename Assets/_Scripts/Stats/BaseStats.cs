using UnityEngine;

namespace BB.Stats
{
    public class BaseStats : MonoBehaviour
    {
        [Range(1, 99)]
        [SerializeField] int startingLevel = 1;
        [SerializeField] CharacterType characterType;
        [SerializeField] Progression progression = null;

        public float GetHealth()
        {
            return progression.GetHealth(characterType, startingLevel);
        }

        public float GetAttackDamage()
        {
            return progression.GetStat(characterType, startingLevel, Stat.AttackDamage);
        }

        public float GetExperienceReward()
        {
            return 10;
        }
    }
}
