using UnityEngine;
using Unity.Entities;

public class WallSectionAuthoring : MonoBehaviour
{
    public class Baker : Baker<WallSectionAuthoring>
    {
        public override void Bake(WallSectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new WallSection
						{
                            isDestroyed = false,
						});
        }
    }
}

public struct WallSection : IComponentData 
{
    public bool isDestroyed;
}
