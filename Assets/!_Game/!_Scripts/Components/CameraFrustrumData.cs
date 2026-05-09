using Unity.Entities;
using Unity.Mathematics;

public struct CameraFrustumData : IComponentData
{
    public float4x4 WorldToCameraMatrix;
    public float Fov;
    public float Aspect;
    public bool IsLive;
}


