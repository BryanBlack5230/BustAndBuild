using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace BarkingBird.Runtime.Gameplay.AI
{
    public class UnitMoverAuthoring : MonoBehaviour
    {
        public float moveSpeed;
        public float turnSpeed;
        public class Baker : Baker<UnitMoverAuthoring>
        {
            public override void Bake(UnitMoverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitMover
                {
                    moveSpeed = authoring.moveSpeed,
                    turnSpeed = authoring.turnSpeed
                });
            }
        }
    }

    public struct UnitMover : IComponentData
    {
        public float moveSpeed;
        public float turnSpeed;
    }

    public struct Destination : IComponentData
    {
        public float3 Value;
        public float StoppingDistanceSq;
    }
}
