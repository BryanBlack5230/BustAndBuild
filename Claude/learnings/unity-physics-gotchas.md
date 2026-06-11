# Unity Physics Gotchas

Engine-level Unity Physics (DOTS) behaviors discovered the hard way. Project battle systems that consume physics live in [[ecs-combat-and-collisions]]; generic DOTS patterns in [[ecs-patterns]].

## PhysicsCollider AABB — Local vs World (Critical Gotcha)
**Context:** `BattleCoordinatorSystem.SetBases` originally called `colliders[i].Value.Value.CalculateAabb()` to cache base AABBs. Enemy retreat destinations landed at world-space coords near `(-7, 0.5, -6)` — i.e. on the world origin instead of inside the actual base.

**Finding:** `PhysicsCollider.Value.Value.CalculateAabb()` returns the AABB in the **collider's local space**, including the box `Center` offset baked into the geometry. To get a world-space AABB, use the overload `CalculateAabb(RigidTransform)` and pass the entity's world pose:
```csharp
var worldTransform = new RigidTransform(transforms[i].Rotation, transforms[i].Position);
var worldAabb = colliders[i].Value.Value.CalculateAabb(worldTransform);
```
Requires `using Unity.Mathematics;` (RigidTransform) and `using Unity.Transforms;` (LocalToWorld for `.Rotation`/`.Position`).

**Why it matters:** Anything that consumes a `PhysicsCollider`'s AABB for spatial queries (`ClosestPoint`, overlap, frustum check) and assumes world space will silently use local space. `PhysicsUtility.GetRandomPointInsideCollider` is already correct (it transforms); ad-hoc reads at call sites are not. Pattern: when reading collider AABBs, always pair with a `LocalToWorld` query.

## Post-Physics System Placement (PearlFloatSystem)
**Context:** `PearlFloatSystem` initially used `[UpdateInGroup(typeof(PhysicsSystemGroup))] [UpdateAfter(typeof(PhysicsSimulationGroup))]` (same as `InAirCollisionSystem`). Pearls fell to the ground but never transitioned to floating state — rest timer never accumulated, Y override never showed.
**Finding:** `PhysicsSystemGroup` contains, in order: `PhysicsInitializeGroup` → `PhysicsSimulationGroup` → `ExportPhysicsWorld` → `AfterPhysicsSystemGroup`. `ExportPhysicsWorld` is the system that syncs internal sim state back into `PhysicsVelocity` and `LocalTransform` component buffers. `[UpdateAfter(PhysicsSimulationGroup)]` only constrains "after step 2" — the scheduler is free to place the system before OR after `ExportPhysicsWorld`. If before:
- `PhysicsVelocity.Linear` reads return the PRE-simulation value (whatever was there at frame start). A falling body can show > threshold velocity indefinitely → no rest detection.
- Writes to `LocalTransform.Position` get clobbered by `ExportPhysicsWorld` running after.

**Fix:** Use `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]` for any system that needs POST-physics `PhysicsVelocity` or that writes `LocalTransform` for rendering.

**Why `InAirCollisionSystem`'s placement works**: it consumes `CollisionEvent` from `SimulationSingleton` (populated DURING `PhysicsSimulationGroup`, available after it) and writes velocity via ECB (which plays back at `EndSimulationEntityCommandBufferSystem`, end of frame — well after `ExportPhysicsWorld`). Reading velocity via `_velocityLookup` for the reflection math gives the velocity AT collision-record time, which is what bounce reflection actually wants.

**Rule of thumb:**
- Reading collision events / writing via ECB → `[UpdateInGroup(PhysicsSystemGroup)] [UpdateAfter(PhysicsSimulationGroup)]` is fine.
- Reading post-sim velocity directly, OR writing `LocalTransform` directly → `[UpdateInGroup(AfterPhysicsSystemGroup)]`.

## `PhysicsGravityFactor` Is Not Auto-Baked
Unity's `RigidbodyBaker` only adds `PhysicsGravityFactor` for non-default cases (e.g., `useGravity = false` bakes Value=0). A dynamic Rigidbody with `useGravity = true` won't get the component unless you add it explicitly. For runtime gravity toggling, add it in your authoring `Baker`:
```csharp
AddComponent(entity, new PhysicsGravityFactor { Value = 1f });
```
Without this, an `IJobEntity` with `ref PhysicsGravityFactor` parameter silently doesn't match the entity, and the system appears not to run on those entities.

## Hybrid Kinematic-via-Dynamic Pattern (Pearls)
To make a dynamic body behave like it's hovering in place while still being knockable by other bodies:
1. **Don't** mark it kinematic. Keep it dynamic with its collider.
2. Each post-physics tick: set `PhysicsVelocity.Linear = 0`, `Angular = 0`, set `PhysicsGravityFactor.Value = 0`, write the desired `LocalTransform.Position.y` (sine bob).
3. To detect "I was pushed", check post-physics `lengthsq(velocity.Linear) > thresholdSq` — when a unit overlaps the hover-body, the solver applies impulse to resolve penetration, producing nonzero velocity. That's the wake signal.
4. On wake: clear the velocity-zeroing flag, restore `gravity.Value = 1`. Physics integrates naturally next frame.

**Why not `ICollisionEventsJob` for wake?** Collision events fire every frame for stable resting contacts (pearl on ground, pearl touching pearl). The post-physics velocity check naturally filters: stable contacts produce ~0 velocity (solver fully resolves), real impacts produce > threshold. Simpler and no spurious wakes.

## Exempting a Layer from Existing Collision-Response Systems
When a new collider type (e.g., pearls on `PickUps` layer) starts colliding with units thanks to a layer-matrix change, existing systems like `InAirCollisionSystem` will start firing their bounce/landed logic on those collisions. Gate by layer-bit check on the OTHER entity's `CollisionFilter.BelongsTo`:
```csharp
// In OnCreate (not [BurstCompile] — LayerMask.NameToLayer is managed)
_pickUpsLayerBit = 1u << LayerMask.NameToLayer(RuntimeConstants.PhysicLayers.PickUps);

// In the collision job
if (IsPickUp(entityA) || IsPickUp(entityB)) return;

private bool IsPickUp(Entity entity)
    => ColliderLookup.TryGetComponent(entity, out var collider)
       && (collider.Value.Value.GetCollisionFilter().BelongsTo & PickUpsLayerBit) != 0;
```
Mirrors the existing `_groundLayerBit` pattern in `InAirCollisionSystem` — keep them consistent so the file stays readable.

## DynamicsManager.asset Collision Matrix — Hex Format
`ProjectSettings/DynamicsManager.asset` stores `m_LayerCollisionMatrix` as a single hex string of 32 × 4 bytes (256 chars). Each layer's mask is a 32-bit uint encoded **little-endian** (LSB byte first). To toggle collision between layers A and B you must edit BOTH rows symmetrically — Unity's editor manages this, but if you edit the YAML directly you have to do both yourself, or the asymmetric mask will fail the collision check (Unity Physics builds `CollisionFilter.CollidesWith` per body from one row of the matrix; both directions must agree).

Decoding example: layer 10 (`PickUps`) hex `ff8cfcff` = uint `0xFFFC8CFF`:
- byte 0 (chars 0–1) `ff` = bits 0–7 (Default..Ground)
- byte 1 (chars 2–3) `8c` = `10001100` → bits 8 (Grabbable)=0, 9 (Obstacle)=0, 10 (PickUps)=1, 11=1, 15=1
- etc.

To enable collision between PickUps (10) and Unit (6): set bit 10 in Unit's row AND set bit 6 in PickUps' row.

**Gotcha:** SubScene baking captures `CollisionFilter` at bake time. Editing the matrix requires re-baking the subscene for the change to take effect at runtime.
