using Unity.Entities;
using Unity.Physics;
using UnityEngine;

/// <summary>
/// The single persistent Beacon Core entity (ADR-0006): a grabbable sphere the player carries into the
/// Beacon socket to start the Day. Toggled via <c>EntityManager.SetEnabled</c> (hidden on insert,
/// re-enabled on day-end drop) rather than destroy+respawn.
///
/// Bakes the full grabbable+claimable archetype so the prefab only needs this component plus a
/// Grabbable-layer collider and a dynamic PhysicsBody:
/// <list type="bullet">
///   <item><c>BeaconCore</c> tag — identifies the Core to the controller/bridge and suppresses the throw arc.</item>
///   <item><c>Grabbed</c> + <c>InAir</c> (disabled) — the grab/throw archetype.</item>
///   <item><c>Claimable</c> — opts into the placement settle-check.</item>
///   <item><c>PristineMass</c> — captured at runtime so the day-end drop can restore an un-frozen mass.</item>
/// </list>
/// Deliberately does NOT bake <c>BounceDamage</c>: that omission is the Core's bounce/landing-damage
/// exemption (InAirCollisionSystem early-outs without it), so a flicked Core deals no damage.
/// </summary>
public class BeaconCoreAuthoring : MonoBehaviour
{
    public class Baker : Baker<BeaconCoreAuthoring>
    {
        public override void Bake(BeaconCoreAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BeaconCore());
            AddComponent(entity, new Claimable());
            AddComponent(entity, new PristineMass());

            AddComponent(entity, new Grabbed());
            AddComponent(entity, new InAir());
            SetComponentEnabled<Grabbed>(entity, false);
            SetComponentEnabled<InAir>(entity, false);
        }
    }
}

public struct BeaconCore : IComponentData { }

/// <summary>
/// The Core's natural (un-frozen) <see cref="PhysicsMass"/>, captured once at runtime by
/// <c>BeaconEcsBridge</c> while the Core is resting. Grabbing freezes the mass (InverseMass=0) and the
/// insert path can hide the Core before the normal release-time restore runs, so the day-end drop
/// restores this to avoid the Core coming back frozen/immovable.
/// </summary>
public struct PristineMass : IComponentData
{
    public PhysicsMass Value;
    public bool Captured;
}
