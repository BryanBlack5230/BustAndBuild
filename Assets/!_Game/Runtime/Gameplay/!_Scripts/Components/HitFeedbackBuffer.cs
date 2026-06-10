using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(4)]
public struct HitFeedbackBufferElement : IBufferElementData
{
    // World-space pushback direction (opposite of incoming hit).
    public float3 HitDirection;
}
