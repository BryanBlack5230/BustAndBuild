using Unity.Entities;
using UnityEngine;

public class InAirAuthoring : MonoBehaviour
{
    public class Baker : Baker<InAirAuthoring>
    {
        public override void Bake(InAirAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new InAir());
            SetComponentEnabled<InAir>(entity, false);
        }
    }
}

public struct InAir : IComponentData, IEnableableComponent { }
