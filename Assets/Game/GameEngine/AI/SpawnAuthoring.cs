using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class SpawnAuthoring : MonoBehaviour
{
    [System.Serializable]
    public enum SpawnType { Point, Area, Radius, Attached }

    public GameObject prefab;
    public float timeBetweenSpawns = 2f;
    public SpawnType strategy = SpawnType.Point;
    [Space]
    public Transform spawnAttachedTarget;
    public GameObject areaColliderGameObject;
    public float radius = 10f;

    public class Baker : Baker<SpawnAuthoring>
    {
        public override void Bake(SpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnSettings
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                spawnInterval = authoring.timeBetweenSpawns
            });
            
            AddComponent(entity, new SpawnPosition());

            switch (authoring.strategy)
            {
                case SpawnType.Point:
                    AddComponent(entity, new SpawnByPoint
                    {
                        position = authoring.transform.position
                    });
                    break;

                case SpawnType.Area:
                    AddComponent(entity, new SpawnByArea
                    {
                        areaCollider = GetEntity(authoring.areaColliderGameObject, TransformUsageFlags.None)
                    });
                    break;

                case SpawnType.Radius:
                    AddComponent(entity, new SpawnByRadius
                    {
                        center = authoring.transform.position,
                        radius = authoring.radius
                    });
                    break;

                case SpawnType.Attached:
                    AddComponent(entity, new SpawnAttached
                    {
                        targetEntity = GetEntity(authoring.spawnAttachedTarget, TransformUsageFlags.Dynamic),
                        offset = float3.zero
                    });
                    break;
            }
        }
    }
}
public struct SpawnSettings : IComponentData
{
    public Entity prefab;
    public float spawnInterval;
    public float timer;
}

// All spawners have this one
public struct SpawnPosition : IComponentData
{
    public float3 value;
}


// Fixed point
public struct SpawnByPoint : IComponentData
{
    public float3 position;
}

// Random point inside a box area
public struct SpawnByArea : IComponentData
{
    public Entity areaCollider;
}

// Random point inside a sphere
public struct SpawnByRadius : IComponentData
{
    public float3 center;
    public float radius;
}

// Attached to another entity’s transform
public struct SpawnAttached : IComponentData
{
    public Entity targetEntity;
    public float3 offset;
}