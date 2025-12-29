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
            AddComponent(entity, 
						new Target
						{
						
						});
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