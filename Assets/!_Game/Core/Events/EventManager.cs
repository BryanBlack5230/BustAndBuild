using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Core.Events
{
    public static class EventManager
    {
        public static Action<float3> OnEnemyDied;

        public static class Input
        {
            public static Action ObjectGrabbed;
            public static Action<bool> GroundGrabbed; //true - actually grabbed, false - start grabbing
            public static Action Release;

            public static Action<bool> SceneChangeRequest; // false - up, true - down
        }
    }
    
    public struct EnemyDiedEvent : IBufferElementData
    {
        public Entity EnemyEntity;
        public float3 EnemyPosition;
    }
}


