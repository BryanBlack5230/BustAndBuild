using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public float health = 10f;
    
    public class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Health { Value = authoring.health, Max = authoring.health });
            AddBuffer<DamageBufferElement>(entity);
        }
    }
}

public struct Health : IComponentData
{
    public float Value;
    public float Max;
    public bool IsDead => Value <= 0;
}

[InternalBufferCapacity(8)]
public struct DamageBufferElement : IBufferElementData
{
    public float Value;
}

public struct IsDead : IComponentData, IEnableableComponent {}