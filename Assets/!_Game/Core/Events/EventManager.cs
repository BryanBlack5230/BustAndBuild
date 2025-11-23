using System;
using Unity.Entities;
using Unity.Mathematics;

public static class EventManager
{
    public static Action<float3> OnEnemyDied;

    public static class Input
    {
        public static Action ObjectGrabbed;
        public static Action GroundGrabbed;
        public static Action Release;
    }
}

public struct EnemyDiedEvent : IBufferElementData
{
    public Entity EnemyEntity;
    public float3 EnemyPosition;
}
