using UnityEngine;
using Unity.Entities;

public class WallSectionAuthoring : MonoBehaviour
{
    private const float DefaultHealth = 200f;

    [Tooltip("Owning castle (per-placement identity).")]
    public GameObject castle;

    [Tooltip("Per-type wall tuning from the ConfigHub. Editing it re-bakes the subscene. Leave unassigned to fall back to the default.")]
    public WallSectionConfigSO config;

    [Tooltip("Optional per-instance health override layered on top of the config (e.g. a tougher wall).")]
    public OptionalFloat healthOverride;

    public class Baker : Baker<WallSectionAuthoring>
    {
        public override void Bake(WallSectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var castle = GetEntity(authoring.castle, TransformUsageFlags.None);

            if (authoring.config != null) DependsOn(authoring.config);
            var health = authoring.healthOverride.Resolve(authoring.config != null ? authoring.config.Health : DefaultHealth);

            AddComponent(entity, new WallSection { CastleEntity = castle, });
            AddComponent(entity, new Structure());
            AddComponent(entity, new Health{ Max = health, Value = health });
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
