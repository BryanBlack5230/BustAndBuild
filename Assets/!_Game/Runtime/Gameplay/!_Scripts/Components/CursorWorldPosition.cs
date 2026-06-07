using Unity.Entities;
using Unity.Mathematics;

public struct CursorWorldPosition : IComponentData
{
    public float3 RayOrigin;
    public float3 RayDirection;
    public bool IsValid;
}