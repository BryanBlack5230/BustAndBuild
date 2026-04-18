using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace GameEngine.AI
{
    public class UnitMoverAuthoring : MonoBehaviour
    {
        public float moveSpeed;
        public class Baker : Baker<UnitMoverAuthoring>
        {
            public override void Bake(UnitMoverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitMover
                {
                    moveSpeed = authoring.moveSpeed,
                });
            }
        }
    }

    public struct UnitMover : IComponentData, IEnableableComponent
    {
        public float moveSpeed;
        public float turnSpeed;
    }

    public struct Destination : IComponentData
    {
        public float3 Value;
        public float StoppingDistanceSq;
    }

    public struct EvasionDestination : IComponentData
    {
        public float3 MoveDirection;
        public float3 Position;
    }

}
