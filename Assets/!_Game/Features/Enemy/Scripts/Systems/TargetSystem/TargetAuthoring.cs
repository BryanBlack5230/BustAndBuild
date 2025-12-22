using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public enum TargetType : byte
{
    None = 0,
    Ally = 1,
    Wall = 2,
    Beacon = 3
}

public class TargetAuthoring : MonoBehaviour
{
    public class Baker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new Target
						{
						
						});
        }
    }
}

public struct Target : IComponentData
{
    public Entity targetEntity;
    public TargetType targetType;
    public float targetPriority;
}


public struct TargetCandidate
{
    public Entity entity;
    public float3 position;
    public TargetType type;
    public float priority;
    public float distance;
    public bool isAggressive;
}

public struct TargetCandidateComparer : IComparer<TargetCandidate>
{
    public int Compare(TargetCandidate a, TargetCandidate b)
    {
        return b.priority.CompareTo(a.priority); // Descending order
    }
}

public struct ThreatInfo
{
    public float3 position;
    public float threatLevel;
}