using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class PathfinderAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PathfinderAuthoring>
        {
            public override void Bake(PathfinderAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FinalDestination ());
                AddComponent(entity, new PathTarget ());
            }
        }
    }

    public struct FinalDestination : IComponentData 
    {
        public float3 Value;
    }


    public struct PathTarget : IComponentData 
    {
        public float3 Value;
    }
}
