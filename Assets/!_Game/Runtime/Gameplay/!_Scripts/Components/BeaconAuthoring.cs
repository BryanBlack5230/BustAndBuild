using UnityEngine;
using Unity.Entities;

public class BeaconAuthoring : MonoBehaviour
{
    private const float DefaultHealth = 6000f;

    [Tooltip("Per-type beacon tuning from the ConfigHub. Editing it re-bakes the subscene. Leave unassigned to fall back to the default.")]
    public BeaconConfigSO config;

    [Tooltip("Optional per-instance health override layered on top of the config.")]
    public OptionalFloat healthOverride;

    public class Baker : Baker<BeaconAuthoring>
    {
        public override void Bake(BeaconAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.config != null) DependsOn(authoring.config);
            var health = authoring.healthOverride.Resolve(authoring.config != null ? authoring.config.Health : DefaultHealth);

            AddComponent(entity, new BeaconTag { });
            AddComponent(entity, new Structure());
            AddComponent(entity, new Health {Max = health, Value = health });
            AddComponent(entity, new IsDead());
            SetComponentEnabled<IsDead>(entity, false);
            AddBuffer<DamageBufferElement>();
        }
    }
}

public struct BeaconTag : IComponentData
{

}
