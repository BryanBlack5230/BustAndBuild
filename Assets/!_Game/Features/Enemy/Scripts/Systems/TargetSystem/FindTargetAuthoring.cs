using Game.Configs;
using UnityEngine;
using Unity.Entities;

public class FindTargetAuthoring : MonoBehaviour
{
    public float UpdateInterval = 0.5f;
    
    public class Baker : Baker<FindTargetAuthoring>
    {
        public override void Bake(FindTargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FindTarget { Timer = authoring.UpdateInterval });
            AddComponent(entity, new Target());
        }
    }
}

public struct FindTarget : IComponentData
{
    public float Timer;
}

public struct TargetProfiles : IComponentData
{
    public BlobAssetReference<TargetProfilesBlob> Blob;
}