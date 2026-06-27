using Unity.Entities;
using Unity.Physics;

// Marker for large attackable buildings (walls, beacon, future barracks). Added in each structure's
// Baker. Its presence is what flips targeting/brain math from center-distance to surface-distance, so
// new structure types need zero AI edits (discriminate by HasComponent<TargetBounds>, not Target.Type).
public struct Structure : IComponentData { }

// World-space AABB of a Structure, cached once at runtime by StructureBoundsSystem. Consumers measure
// distance to World.ClosestPoint(point) so units stop/attack at the surface instead of the transform
// pivot, which sits far inside a big collider. NOT baked: the world transform isn't reliably available
// at bake time (CalculateAabb with no transform returns LOCAL space — see unity-physics-gotchas).
public struct TargetBounds : IComponentData
{
    public Aabb World;
}
