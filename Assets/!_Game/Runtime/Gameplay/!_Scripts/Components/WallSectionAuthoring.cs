using UnityEngine;
using Unity.Entities;

public class WallSectionAuthoring : MonoBehaviour
{
    public GameObject castle;
    public float health = 200f;
    public class Baker : Baker<WallSectionAuthoring>
    {
        public override void Bake(WallSectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var castle = GetEntity(authoring.castle, TransformUsageFlags.None);
            AddComponent(entity, new WallSection { CastleEntity = castle, });
            AddComponent(entity, new Health{ Max = authoring.health, Value = authoring.health });
            AddComponent(entity, new IsDead());
            SetComponentEnabled<IsDead>(entity, false);
            
            AddBuffer<DamageBufferElement>(entity);
        }
    }
}

public struct WallSection : IComponentData 
{
    public Entity CastleEntity;
}

public struct WallCleanupTag : ICleanupComponentData 
{
    public Entity CastleEntity;
}
