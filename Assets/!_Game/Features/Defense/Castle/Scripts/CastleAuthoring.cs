using UnityEngine;
using Unity.Entities;

public class CastleAuthoring : MonoBehaviour
{
    public class Baker : Baker<CastleAuthoring>
    {
        public override void Bake(CastleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new Castle
						{
						    hasBeenBreached = false
						});
        }
    }
}

public struct Castle : IComponentData
{
    public bool hasBeenBreached;
}