using UnityEngine;
using Unity.Entities;

public class TargetAuthoring : MonoBehaviour
{
    public class Baker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new Target
						{
						
						});
        }
    }
}

public struct Target : IComponentData
{
    public Entity targetEntity;
}