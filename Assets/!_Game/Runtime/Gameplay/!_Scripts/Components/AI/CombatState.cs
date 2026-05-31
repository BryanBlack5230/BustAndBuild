using Unity.Entities;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public struct CombatState : IComponentData
    {
        public bool inCombat;
        public Entity currentEnemy;
        public float attackCooldown;
        public float attackRange;
    }
}