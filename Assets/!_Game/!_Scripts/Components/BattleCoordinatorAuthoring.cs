using UnityEngine;
using Unity.Entities;
using Unity.Physics;

public class BattleCoordinatorAuthoring : MonoBehaviour
{
    public class Baker : Baker<BattleCoordinatorAuthoring>
    {
        public override void Bake(BattleCoordinatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new BattleCoordinator
            {
                IsBattleActive = false,
                ForceGlobalReevaluation = false,
                WasCastleBreached = false,
            });
            AddComponent(entity, new FactionBases());

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

public struct FactionBases : IComponentData
{
    public Aabb AllyBaseBounds;
    public Aabb EnemyBaseBounds;
    public bool IsInitialized;
}

public struct EnemyUnitReference : IBufferElementData { public Entity Value; }
public struct AllyUnitReference : IBufferElementData { public Entity Value; }