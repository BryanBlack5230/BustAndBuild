using UnityEngine;
using Unity.Entities;

public class GrabbedAuthoring : MonoBehaviour
{
    public class Baker : Baker<GrabbedAuthoring>
    {
        public override void Bake(GrabbedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Grabbed());
            SetComponentEnabled<Grabbed>(entity, false);
        }
    }
}

public struct Grabbed : IComponentData, IEnableableComponent
{
// NOOP
}