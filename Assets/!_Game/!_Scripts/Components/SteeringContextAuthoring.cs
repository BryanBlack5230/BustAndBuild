using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SteeringContextAuthoring : MonoBehaviour
{
    public class Baker : Baker<SteeringContextAuthoring>
    {
        public override void Bake(SteeringContextAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SteeringContext ());
        }
    }
}

public static class SteeringConstants
{
    public static readonly float3[] Directions = new float3[]
    {
        new float3(0, 0, 1),                     // North
        math.normalize(new float3(1, 0, 1)),     // North-East
        new float3(1, 0, 0),                     // East
        math.normalize(new float3(1, 0, -1)),    // South-East
        new float3(0, 0, -1),                    // South
        math.normalize(new float3(-1, 0, -1)),   // South-West
        new float3(-1, 0, 0),                    // West
        math.normalize(new float3(-1, 0, 1))     // North-West
    };
    
    public static readonly string[] DirectionsText = new string[]
    {
        "N",     // North
        "NE",    // North-East
        "E",     // East
        "SE",    // South-East
        "S",     // South
        "SW",    // South-West
        "W",     // West
        "NW",    // North-West
    };
}

public struct SteeringContext : IComponentData
{
    public float AgentRadius;
    public ContextMap Interest;
    public ContextMap Danger;

    public float3 BestDirection;
}

public struct SteeringEnabled : IEnableableComponent, IComponentData { }

public struct ContextMap
{
    public float N, NE, E, SE, S, SW, W, NW;
    
    // Helper to access by index (0-7)
    public float this[int index]
    {
        get 
        {
            switch(index) {
                case 0: return N; case 1: return NE; case 2: return E; case 3: return SE;
                case 4: return S; case 5: return SW; case 6: return W; case 7: return NW;
                default: return this[index  % 8];
            }
        }
        set 
        {
            switch(index) {
                case 0: N = value; break; case 1: NE = value; break; case 2: E = value; break; case 3: SE = value; break;
                case 4: S = value; break; case 5: SW = value; break; case 6: W = value; break; case 7: NW = value; break;
            }
        }
    }
}

public struct SteerBehavior_Seek : IComponentData
{
    public float Weight;
}

public struct SteerBehavior_Obstacle : IComponentData
{
    public float DangerWeight;       // Multiplier for danger
    public float SurroundRadius;     // "Personal Space" (Sphere Radius)
    public float VisionSize;
    public float VisionDistance;

    public float UpdateInterval;     
    public LayerMask ObstacleLayer;
}

public struct ObstacleShadow : IComponentData
{
    public float3 MyLastPos;
    public ContextMap CachedDanger;
    public float Timer;
}

public struct SteerBehavior_Evasion : IComponentData 
{
    public float Weight;
}