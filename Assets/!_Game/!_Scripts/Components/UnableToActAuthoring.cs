using Unity.Entities;
using UnityEngine;

public class UnableToActAuthoring: MonoBehaviour
{
    public class Baker : Baker<UnableToActAuthoring>
    {
        public override void Bake(UnableToActAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnableToAct());
            SetComponentEnabled<UnableToAct>(entity, false);
        }
    }
}

public struct UnableToAct : IComponentData, IEnableableComponent
{
// NOOP
}