using UnityEngine;
using Unity.Entities;

public class WallSectionAuthoring : MonoBehaviour
{
    public float health = 200f;
    public class Baker : Baker<WallSectionAuthoring>
    {
        public override void Bake(WallSectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new WallSection { isDestroyed = false, });
            AddComponent(entity, new Health{ Max = authoring.health, Value = authoring.health });
            AddBuffer<DamageBufferElement>(entity);
        }
    }
}

public struct WallSection : IComponentData 
{
    public bool isDestroyed;
}
