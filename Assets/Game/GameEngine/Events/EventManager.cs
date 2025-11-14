using System;
using Unity.Entities;
using Unity.Mathematics;

public static class EventManager
{
    public static Action<float3> OnEnemyDied;
}

public struct EnemyDiedEvent : IBufferElementData
{
    public Entity EnemyEntity;
    public float3 EnemyPosition;
}
