using Unity.Entities;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class StunAuthoring : MonoBehaviour
    {
        public class Baker : Baker<StunAuthoring>
        {
            public override void Bake(StunAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Stun());
                SetComponentEnabled<Stun>(entity, false);
            }
        }
    }

    public struct Stun : IComponentData, IEnableableComponent
    {
        public float Remaining;
    }
}
