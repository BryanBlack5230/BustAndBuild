using GameEngine.AI;
using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public Faction faction = Faction.Enemy;
    public EnemyType enemyType = EnemyType.Grunt;
    public Transform unitBase;
    public float moveSpeed = 3f;
    public float stoppingDistance = 1f;
    public float health = 50f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;
    public float attackRange = 0.5f;
    
    public class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // tags
            AddComponent(entity, new UnableToAct());
            AddComponent(entity, new Grabbed());
            AddComponent(entity, new IsDead());
            AddComponent(entity, new AttackCooldownExpirationTimestamp());
            AddComponent(entity, new TargetSearchCooldownExpirationTimestamp());

            SetComponentEnabled<UnableToAct>(entity, false);
            SetComponentEnabled<Grabbed>(entity, false);
            SetComponentEnabled<IsDead>(entity, false);
            SetComponentEnabled<AttackCooldownExpirationTimestamp>(entity, false);
            SetComponentEnabled<TargetSearchCooldownExpirationTimestamp>(entity, false);

            // components
            AddComponent(entity, new Unit { faction = authoring.faction, });
            AddComponent(entity, new EnemyUnitType { Value = authoring.enemyType, });
            AddComponent(entity, new UnitMover { moveSpeed = authoring.moveSpeed, });
            AddComponent(entity, new Destination { StoppingDistanceSq = authoring.stoppingDistance * authoring.stoppingDistance});
            AddComponent(entity, new Target());
            AddComponent(entity, new Health { Value = authoring.health, Max = authoring.health });
            AddBuffer<DamageBufferElement>(entity);
            AddComponent(entity, new AttackData { Damage = authoring.attackDamage, CooldownTime = authoring.attackCooldown, AttackRange = authoring.attackRange});
            AddComponent(entity, new BattleBrain { CanAttack = false});
            AddComponent(entity, new EmotionalState {Value = Emotion.Normal});
            AddComponent(entity, new ActionState { Value = ActionType.Moving });
        }
    }
}