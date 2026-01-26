using Game.Configs;
using UnityEngine;
using Unity.Entities;

public enum TargetType : byte
{
    None = 0,
    Unit = 1,
    Wall = 2,
    Beacon = 3
}

public class TargetAuthoring : MonoBehaviour
{
    public class Baker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Target());
            AddComponent(entity, new TargetSearchCooldownExpirationTimestamp());
            SetComponentEnabled<TargetSearchCooldownExpirationTimestamp>(entity, false);
        }
    }
}

public struct Target : IComponentData
{
    public Entity TargetEntity;
    public TargetType Type;
    public float CurrentScore; // Debugging help: why did I pick this?
    public float DistanceToTarget;
}

public struct TargetProfiles : IComponentData
{
    public BlobAssetReference<TargetProfilesBlob> Blob;
}

public struct TargetSearchCooldownExpirationTimestamp : IComponentData, IEnableableComponent
{
    public double Value;
}