using Unity.Collections;
using Unity.Entities;

public struct ThrowVelocitySettings : IComponentData
{
    public float MinVelocity;
    public float MaxVelocity;
    public FixedList512Bytes<float> CurveSamples;
}
