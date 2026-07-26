using System.Collections.Generic;

using Unity.Entities;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Currency;
using BarkingBird.Runtime.Infrastructure.Utilities;
using Unity.Mathematics;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("Identity (per-instance)")]
        public Faction faction = Faction.Enemy;
        public EnemyType enemyType = EnemyType.Grunt;

        [Header("Stats (per-type, from the ConfigHub)")]
        [Tooltip("Per-type stats + drop table. Editing it re-bakes the subscene. Leave unassigned to fall back to UnitStats.EnemyDefault.")]
        public EnemyUnitProfile profile;

        [Tooltip("Optional per-instance overrides layered on top of the profile's stats.")]
        public UnitStatOverrides overrides;

        [Header("Drops override (per-instance)")]
        [Tooltip("When on, this instance drops the table below instead of the profile's.")]
        public bool overrideDrops;
        public List<DropTableEntry> drops = new();

        private void OnValidate()
        {
            if (profile != null && (int)profile.Type != (int)enemyType)
                Log.Battle.W($"profile '{profile.name}' is type {profile.Type} but enemyType is {enemyType}; the runtime type tag and the stats will disagree.");
        }

        public class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var baseStats = authoring.profile != null ? authoring.profile.Stats : UnitStats.EnemyDefault;
                if (authoring.profile != null) DependsOn(authoring.profile);
                var stats = authoring.overrides.Apply(baseStats);

                // tags
                AddComponent(entity, new UnableToAct());
                AddComponent(entity, new Grabbed());
                AddComponent(entity, new InAir());
                AddComponent(entity, new IsDead());
                AddComponent(entity, new AttackCooldownExpirationTimestamp());
                AddComponent(entity, new TargetSearchCooldownExpirationTimestamp());
                AddComponent(entity, new SteeringEnabled());
                AddComponent(entity, new HasLeftBase());
                AddComponent(entity, new Escaped());

                SetComponentEnabled<UnableToAct>(entity, false);
                SetComponentEnabled<Grabbed>(entity, false);
                SetComponentEnabled<InAir>(entity, false);
                SetComponentEnabled<IsDead>(entity, false);
                SetComponentEnabled<AttackCooldownExpirationTimestamp>(entity, false);
                SetComponentEnabled<TargetSearchCooldownExpirationTimestamp>(entity, false);
                SetComponentEnabled<SteeringEnabled>(entity, true);
                SetComponentEnabled<HasLeftBase>(entity, false);
                SetComponentEnabled<Escaped>(entity, false);

                // components
                AddComponent(entity, new Unit { faction = authoring.faction, });
                AddComponent(entity, new EnemyUnitType { Value = authoring.enemyType, });
                AddComponent(entity, new UnitMover { moveSpeed = stats.MoveSpeed, turnSpeed = stats.TurnSpeed});
                AddComponent(entity, new Destination { StoppingDistanceSq = stats.StoppingDistance * stats.StoppingDistance});
                AddComponent(entity, new Target());
                AddComponent(entity, new Health { Value = stats.Health, Max = stats.Health });
                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<HitFeedbackBufferElement>(entity);
                AddComponent(entity, new AttackData { Damage = stats.AttackDamage, CooldownTime = stats.AttackCooldown, AttackRange = stats.AttackRange});
                AddComponent(entity, new BattleBrain { CanAttack = false});
                AddComponent(entity, new EmotionalState {Value = Emotion.Aware});
                AddComponent(entity, new ActionState { Value = ActionType.Moving });

                AddComponent(entity, new BounceDamage
                {
                    BaseDamage = stats.BounceBaseDamage,
                    BounceCount = 0,
                    BounceDamageMultiplier = stats.BounceMultiplier,
                    BounceElasticity = stats.BounceElasticity
                });

                AddComponent(entity, new SteerBehavior_Seek{Weight = 1f});
                AddComponent(entity, new SteeringContext{AgentRadius = stats.AgentSize});
                AddComponent(entity, new SteerBehavior_Obstacle
                {
                    DangerWeight = stats.DangerWeight,
                    SurroundRadius = stats.SurroundRadius,
                    VisionSize = stats.AgentSize,
                    VisionDistance = stats.VisionDistance,
                    UpdateInterval = stats.ScanInterval,
                    ObstacleLayer = stats.ObstacleLayer,
                    Curve = stats.ObstacleDangerCurve,
                });
                AddComponent(entity, new ObstacleShadow{Timer = 0});

                AddComponent(entity, new PathTarget());
                AddComponent(entity, new FinalDestination());

                var dropSource = authoring.overrideDrops
                    ? authoring.drops
                    : (authoring.profile != null ? authoring.profile.Drops : null);

                var drops = AddBuffer<ResourceDrop>(entity);
                if (dropSource != null)
                {
                    foreach (var entry in dropSource)
                    {
                        drops.Add(new ResourceDrop
                        {
                            Type = entry.type,
                            MinCount = (int)math.floor(entry.minMaxCount.x),
                            MaxCount = (int)math.floor(entry.minMaxCount.y),
                            Chance = entry.chance,
                            Value = entry.value,
                        });
                    }
                }
            }
        }
    }
}
