using UnityEngine;
using Unity.Entities;

public class BeaconAuthoring : MonoBehaviour
{
    public float health = 6000f;
    public class Baker : Baker<BeaconAuthoring>
    {
        public override void Bake(BeaconAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BeaconTag { });
            AddComponent(entity, new Health {Max = authoring.health, Value = authoring.health });
            AddComponent(entity, new IsDead());
            SetComponentEnabled<IsDead>(entity, false);
            AddBuffer<DamageBufferElement>();
        }
    }
}

public struct BeaconTag : IComponentData
{

}