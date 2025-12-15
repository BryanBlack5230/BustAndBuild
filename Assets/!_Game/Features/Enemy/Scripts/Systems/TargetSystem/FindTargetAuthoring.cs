using Game.Configs;
using UnityEngine;
using Unity.Entities;

public struct FindTargetConfigReference : IComponentData
{
    public BlobAssetReference<FindTargetConfigBlob> ConfigBlob;
}

public class FindTargetAuthoring : MonoBehaviour
{
    public class Baker : Baker<FindTargetAuthoring>
    {
        public override void Bake(FindTargetAuthoring authoring)
        {
            var unitAuthoring = authoring.GetComponent<UnitAuthoring>();
            var sourceFaction = unitAuthoring ? unitAuthoring.faction : Faction.Unknown;
            var oppositeFaction = GetOppositeFaction(sourceFaction);
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new FindTarget
						{
                            targetFaction = oppositeFaction,
                            noTargetInRange = true
						});
        }
        
        private Faction GetOppositeFaction(Faction faction)
        {
            return faction switch
            {
                Faction.Ally => Faction.Enemy,
                Faction.Enemy => Faction.Ally,
                _ => Faction.Unknown
            };
        }
    }
}

public struct FindTarget : IComponentData
{
    public Faction targetFaction;
    public float timer;
    public bool noTargetInRange;
}