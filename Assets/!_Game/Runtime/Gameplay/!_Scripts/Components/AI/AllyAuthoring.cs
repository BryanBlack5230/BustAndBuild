using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class AllyAuthoring : MonoBehaviour
    {
        public Faction faction = Faction.Ally;
        public AllyType allyType = AllyType.Soldier;
        public float moveSpeed = 3f;
        public float turnSpeed = 15f;
        public float stoppingDistance = 1f;
        public float health = 100f;
        public float attackDamage = 20f;
        public float attackCooldown = 2f;
        public float attackRange = 1.5f;
        
        public float bounceBaseDamage = 5f;
        public float bounceMultiplier = 2f;
        public float bounceElasticity = 0.8f;
        
        public float AgentSize = 1f;
        public float DangerWeight = 2.0f;
        public float SurroundRadius = 6.0f;
        public float VisionDistance = 3.0f;
        public float ScanInterval = 0.5f;
        public LayerMask ObstacleLayer;
        
        public Curve ObstacleDangerCurve = Curve.Quadratic;
        
        public class Baker : Baker<AllyAuthoring>
        {
            public override void Bake(AllyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // tags
                AddComponent(entity, new UnableToAct());
                AddComponent(entity, new Grabbed());
                AddComponent(entity, new InAir());
                AddComponent(entity, new IsDead());
                AddComponent(entity, new IsInvulnerable());
                AddComponent(entity, new AttackCooldownExpirationTimestamp());
                AddComponent(entity, new TargetSearchCooldownExpirationTimestamp());
                AddComponent(entity, new SteeringEnabled());

                SetComponentEnabled<UnableToAct>(entity, false);
                SetComponentEnabled<Grabbed>(entity, false);
                SetComponentEnabled<InAir>(entity, false);
                SetComponentEnabled<IsDead>(entity, false);
                SetComponentEnabled<IsInvulnerable>(entity, false);
                SetComponentEnabled<AttackCooldownExpirationTimestamp>(entity, false);
                SetComponentEnabled<TargetSearchCooldownExpirationTimestamp>(entity, false);
                SetComponentEnabled<SteeringEnabled>(entity, true);
                
                // components
                AddComponent(entity, new Unit { faction = authoring.faction, });
                AddComponent(entity, new AllyUnitType { Value = authoring.allyType, });
                AddComponent(entity, new UnitMover { moveSpeed = authoring.moveSpeed, turnSpeed = authoring.turnSpeed});
                AddComponent(entity, new Destination { StoppingDistanceSq = authoring.stoppingDistance * authoring.stoppingDistance });
                AddComponent(entity, new Target());
                AddComponent(entity, new Health { Value = authoring.health, Max = authoring.health });
                AddBuffer<DamageBufferElement>(entity);
                AddComponent(entity, new AttackData { Damage = authoring.attackDamage, CooldownTime = authoring.attackCooldown, AttackRange = authoring.attackRange});
                AddComponent(entity, new BattleBrain{ CanAttack = false});
                AddComponent(entity, new EmotionalState {Value = Emotion.Normal});
                AddComponent(entity, new ActionState { Value = ActionType.Moving });
                
                AddComponent(entity, new BounceDamage
                {
                    BaseDamage = authoring.bounceBaseDamage,
                    BounceCount = 0,
                    BounceDamageMultiplier = authoring.bounceMultiplier,
                    BounceElasticity = authoring.bounceElasticity
                });
                
                AddComponent(entity, new SteerBehavior_Seek{Weight = 1f});
                AddComponent(entity, new SteeringContext{AgentRadius = authoring.AgentSize});
                AddComponent(entity, new SteerBehavior_Obstacle
                {
                    DangerWeight = authoring.DangerWeight,
                    SurroundRadius = authoring.SurroundRadius,
                    VisionSize = authoring.AgentSize,
                    VisionDistance = authoring.VisionDistance,
                    UpdateInterval = authoring.ScanInterval,
                    ObstacleLayer = authoring.ObstacleLayer,
                    Curve = authoring.ObstacleDangerCurve,
                });
                AddComponent(entity, new ObstacleShadow{Timer = 0});
                
                AddComponent(entity, new PathTarget());
                AddComponent(entity, new FinalDestination());
            }
        }
    }
}
