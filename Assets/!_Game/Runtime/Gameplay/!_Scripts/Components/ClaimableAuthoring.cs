using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks an entity as placeable by the generic settle-check seam (ADR-0007): an <c>InAir</c> entity
/// tagged <c>Claimable</c>, moving slower than a place's velocity threshold and inside its radius, is
/// claimed by that place (Beacon Core -> insert; future Units -> assign). The stable contract is
/// <c>Claimable</c> + <c>AssignablePlace</c>; detection is Mono-side (<c>PlacementController</c>) for now
/// and lifts to ECS when unit-assignment brings volume.
///
/// Stand-alone authoring (mirrors <c>GrabbedAuthoring</c>/<c>InAirAuthoring</c>) so any grabbable prefab can
/// opt in. The Beacon Core bakes it via <c>BeaconCoreAuthoring</c> too, so its prefab does not need this.
/// </summary>
public class ClaimableAuthoring : MonoBehaviour
{
    public class Baker : Baker<ClaimableAuthoring>
    {
        public override void Bake(ClaimableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Claimable());
        }
    }
}

public struct Claimable : IComponentData { }
