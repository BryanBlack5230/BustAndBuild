using UnityEngine;
using Unity.Entities;

public class BattleDirectorAuthoring : MonoBehaviour
{
    public class Baker : Baker<BattleDirectorAuthoring>
    {
        public override void Bake(BattleDirectorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new BattleCoordinator
            {
                IsBattleActive = false,
                ForceGlobalReevaluation = false,
                WasCastleBreached = false,
            });

            AddBuffer<EnemyUnitReference>(entity);
            AddBuffer<AllyUnitReference>(entity);
        }
    }
}

public struct BattleCoordinator : IComponentData
{
    public bool IsBattleActive;
    public bool ForceGlobalReevaluation;
    public bool WasCastleBreached;
}

public struct EnemyUnitReference : IBufferElementData { public Entity Value; }
public struct AllyUnitReference : IBufferElementData { public Entity Value; }