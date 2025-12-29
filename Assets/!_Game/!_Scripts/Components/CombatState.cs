using Unity.Entities;

namespace GameEngine.AI
{
    public struct CombatState : IComponentData
    {
        public bool inCombat;
        public Entity currentEnemy;
        public float attackCooldown;
        public float attackRange;
    }
}