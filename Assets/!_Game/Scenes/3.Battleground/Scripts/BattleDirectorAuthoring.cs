using UnityEngine;
using Unity.Entities;

public class BattleDirectorAuthoring : MonoBehaviour
{
    public class Baker : Baker<BattleDirectorAuthoring>
    {
        public override void Bake(BattleDirectorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new BattleDirector
            {
                IsDirty = true 
            });

            AddBuffer<EnemyUnitReference>(entity);
            AddBuffer<AllyUnitReference>(entity);
        }
    }
}

public struct BattleDirector : IComponentData
{
    public bool IsDirty;
}

public struct EnemyUnitReference : IBufferElementData { public Entity Value; }
public struct AllyUnitReference : IBufferElementData { public Entity Value; }