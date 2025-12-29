using GameEngine.AI;
using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public Faction faction = Faction.Enemy;
    public EnemyType enemyType = EnemyType.Grunt;
    public float moveSpeed = 3f;
    public float stoppingDistance = 1f;
    public float checkInterval = 0.5f;
    public class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Unit { faction = authoring.faction, });
            AddComponent(entity, new EnemyUnitType { Value = authoring.enemyType, });
            AddComponent(entity, new UnitMover { moveSpeed = authoring.moveSpeed, });
            AddComponent(entity, new UnableToAct());
            SetComponentEnabled<UnableToAct>(entity, false);
            AddComponent(entity, new Grabbed());
            SetComponentEnabled<Grabbed>(entity, false);
            AddComponent(entity, new Destination { StoppingDistance = authoring.stoppingDistance });
            AddComponent(entity, new FindTarget { Timer = authoring.checkInterval });
            AddComponent(entity, new Target());
        }
    }
}