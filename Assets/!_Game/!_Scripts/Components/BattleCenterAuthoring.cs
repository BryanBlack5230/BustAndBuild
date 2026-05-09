using Unity.Entities;
using UnityEngine;

public class BattleCenterAuthoring : MonoBehaviour
{
    public float HalfWidthOffset = 0f;
    public float HalfHeightOffset = 0f;

    public class Baker : Baker<BattleCenterAuthoring>
    {
        public override void Bake(BattleCenterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BattleScreenCenter
            {
                HalfWidthOffset = authoring.HalfWidthOffset,
                HalfHeightOffset = authoring.HalfHeightOffset,
            });
        }
    }
}

public struct BattleScreenCenter : IComponentData
{
    public float HalfWidthOffset;
    public float HalfHeightOffset;
}
