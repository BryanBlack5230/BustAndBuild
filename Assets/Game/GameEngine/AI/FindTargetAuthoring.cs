using UnityEngine;
using Unity.Entities;

public class FindTargetAuthoring : MonoBehaviour
{
    public float range = 5f; 
    public float timerMax;
    public class Baker : Baker<FindTargetAuthoring>
    {
        public override void Bake(FindTargetAuthoring authoring)
        {
            var unitAuthoring = authoring.GetComponent<UnitAuthoring>();
            
            var sourceFaction = Faction.Unknown;
            if (unitAuthoring) sourceFaction = unitAuthoring.faction;

            var oppositeFaction = GetOppositeFaction(sourceFaction);
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, 
						new FindTarget
						{
                            range = authoring.range,
                            timerMax = authoring.timerMax,
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
    public float range;
    public Faction targetFaction;
    public float timer;
    public float timerMax;
    public bool noTargetInRange;
}