using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FakePathfinderResultAuthoring : MonoBehaviour
{
    public class Baker : Baker<FakePathfinderResultAuthoring>
    {
        public override void Bake(FakePathfinderResultAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FakePathfinderPoint {GatePosition = authoring.transform.position});
        }
    }
}

public struct FakePathfinderPoint : IComponentData 
{
    public float3 GatePosition;
}
