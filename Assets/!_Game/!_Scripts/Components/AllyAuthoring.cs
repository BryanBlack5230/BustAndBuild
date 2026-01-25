using GameEngine.AI;
using Unity.Entities;
using UnityEngine;

public class AllyAuthoring : MonoBehaviour
{
    public Faction faction = Faction.Ally;
    public AllyType allyType = AllyType.Soldier;
    public Transform unitBase;
    public float moveSpeed = 3f;
    public float stoppingDistance = 1f;
    public float checkInterval = 0.5f;
    public float health = 100f;
    public float attackDamage = 20f;
    public float attackCooldown = 2f;
    public float attackRange = 0.5f;
    
    public class Baker : Baker<AllyAuthoring>
    {
        public override void Bake(AllyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // tags
            AddComponent(entity, new UnableToAct());
            AddComponent(entity, new Grabbed());
            AddComponent(entity, new IsDead());
            AddComponent(entity, new AttackCooldownExpirationTimestamp());
            
            SetComponentEnabled<UnableToAct>(entity, false);
            SetComponentEnabled<Grabbed>(entity, false);
            SetComponentEnabled<IsDead>(entity, false);
            SetComponentEnabled<AttackCooldownExpirationTimestamp>(entity, false);
            
            // components
            AddComponent(entity, new Unit { faction = authoring.faction, });
            AddComponent(entity, new BattleUnitBase{position = authoring.transform.position}); // TODO this needs to be set up properly
            AddComponent(entity, new AllyUnitType { Value = authoring.allyType, });
            AddComponent(entity, new UnitMover { moveSpeed = authoring.moveSpeed, });
            AddComponent(entity, new Destination { StoppingDistanceSq = authoring.stoppingDistance * authoring.stoppingDistance });
            AddComponent(entity, new FindTarget { Timer = authoring.checkInterval });
            AddComponent(entity, new Target());
            AddComponent(entity, new Health { Value = authoring.health, Max = authoring.health });
            AddBuffer<DamageBufferElement>(entity);
            AddComponent(entity, new AttackData { Damage = authoring.attackDamage, CooldownTime = authoring.attackCooldown, AttackRange = authoring.attackRange});
            AddComponent(entity, new BattleBrain{ CanAttack = false});
            AddComponent(entity, new EmotionalState {Value = Emotion.Normal});
            AddComponent(entity, new ActionState { Value = ActionType.Moving });
        }
    }
}