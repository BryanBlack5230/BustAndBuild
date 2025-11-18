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

    public struct UnitMover : IComponentData
    {
        public float moveSpeed;
        public float3 targetPosition;
    }

}
