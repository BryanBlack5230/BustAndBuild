using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class BaseAreaAuthoring : MonoBehaviour
    {
        public Faction Faction;

        public class Baker : Baker<BaseAreaAuthoring>
        {
            public override void Bake(BaseAreaAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BaseArea { Faction = authoring.Faction });
            }
        }
    }

    public struct BaseArea : IComponentData
    {
        public Faction Faction;
    }
}
