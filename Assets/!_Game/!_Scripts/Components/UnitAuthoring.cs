using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class UnitAuthoring : MonoBehaviour
{
    public Faction faction;
    public class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new Unit
						{
						    faction = authoring.faction,
						});
        }
    }
}

[System.Serializable]
public enum Faction {Unknown, Ally, Enemy}

[System.Serializable]
public enum EnemyType : byte
{
    Grunt = 0,
    Tank = 1,
    Rusher = 2
}

[System.Serializable]
public enum AllyType : byte
{
    Soldier = 0,
    Archer = 1,
    Mage = 2
}

public struct Unit : IComponentData
{
    public Faction faction;
}

public struct EnemyUnitType : IComponentData
{
    public EnemyType Value;
}

public struct AllyUnitType : IComponentData
{
    public AllyType Value;
}