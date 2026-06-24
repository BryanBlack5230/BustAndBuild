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

## Pickup Settle Is Speed-Only → Mid-Air Settle; Bob Must Be Upward-Only
**Context:** "Some pickups spawn below ground and fall through / disappear." (`PickupFloatSystem`, `PickupSpawnUtility`, scene `PickupSpawner` tuning.)
**Finding (two compounding bugs):**
1. `PickupFloatSystem`'s settle test is **speed-only** (`speedSq < RestSpeedThreshold²` for `RestDuration`), with **no ground-contact requirement**. The scene tunes `restDuration ≈ 0.03s` (~2 frames) and `restSpeedThreshold 0.6`. A pickup is spawned with **zero velocity**, so it settles in **mid-air at its spawn height before gravity ever accelerates it past 0.6 m/s** — it never falls to the ground. So `RestY ≈ spawnY`, NOT ground level.
2. The settled bob was `RestY + sin(phase) * Amplitude` — **symmetric**, so the trough is `Amplitude` *below* `RestY`. With scene `floatAmplitude 1` but `spawnHeight 0.2`, the trough sat ~0.8 m **underground** every cycle.
**Fix:** bob **upward from the rest point**: `RestY + (1 - cos(phase)) * 0.5 * Amplitude` (trough pinned at `RestY`, period unchanged, same peak). Robust for any `Amplitude` and survives post-bump re-settles (where `RestY` = ground-contact height). Note `floatAmplitude 1` is huge for a ~0.4-scale pickup (the `PickupAuthoring` baker default is `0.1`) — aesthetic, not correctness, once the bob is upward-only.
**Why it matters:** "settle when slow" is NOT "settle when grounded." Any hover body spawned at rest will freeze in mid-air; and a symmetric sine bob dives below its baseline whenever amplitude > clearance. Both are invisible until you trace the actual tuning values.

## Pickup Drop Spawn Must Be Ground-Anchored, Not Corpse-Anchored
**Context:** `PickupSpawnUtility.Spawn` originally placed pickups at `dyingUnit.LocalToWorld.Position.y + spawnHeight`.
**Finding:** A unit killed by a throw/smash is briefly **penetrating the ground on its death frame** (`DeathSystem` destroys it same-frame as `ApplyDamageSystem`), so its `LocalToWorld.Position.y` can be **below the ground surface** → pickup spawns underground. Fix: raycast straight down (`CollisionWorld.CastRay`, start a few units above the corpse, filter `CollidesWith = 1 << Ground(7)`) and spawn at `hitY + spawnHeight`, falling back to corpse Y on a miss. Plumbed by both `PickupSpawnOnDeathSystem` and `EnemyEscapeSystem` (scared-escapee loot): `state.RequireForUpdate<PhysicsWorldSingleton>()`, resolve the ground layer bit in `OnCreate` (managed `LayerMask.NameToLayer`), pass `[ReadOnly] CollisionWorld` into the job.
**Why it matters:** Spawn-position fix alone does NOT cure the symptom — the mid-air-settle + symmetric-bob bug above still drives it underground. Both fixes are needed together.

## Where the Battle Ground Actually Lives (CollisionWorld topology)
**Context:** Needed to know what a downward ground raycast can hit at runtime, and why scene colliders looked layer-0.
**Finding:** The walkable ground (the **Island**, layer 7 = `Ground`) lives in `WorldECS.unity`, loaded as a **baked SubScene** by `2.World` (SubScene GUID `45ec3ca1…` = `WorldECS`). It stays loaded under the battle, so the DOTS `CollisionWorld` *does* contain a layer-7 ground. The battle scenes' structural box colliders are **layer 0 (Default)** baked from `BattleGroundSceneECS` (walls/obstacles, not the floor). `3.BattleGroundScene.unity` is the **non-ECS** management/camera scene — its GameObjects are NOT baked, so they are absent from the `CollisionWorld`. Units walk on the Island, so a `Ground`-layer (7) downward ray is the correct surface probe.
**Collision matrix (`DynamicsManager`):** PickUps (layer 10) mask `ff8cfcff` collides with layers **0–7 (incl. Default and Ground)**, 10, 11, 15, 18–31; NOT Grabbable(8)/Obstacle(9). `Ground` (7) = `ffffffff` (collides with everything).
**Side note:** `PearlAuthoring`→`PickupAuthoring` and `PearlSpawnerAuthoring`→`PickupSpawnerAuthoring` were renamed keeping the same `.meta` GUID, so old prefabs/scenes show a stale `m_EditorClassIdentifier` string (e.g. `Runtime::PearlSpawnerAuthoring`) but still bind to the renamed script — don't be misled when reading YAML.

## Pickup Prefabs Use Discrete Collision (tunneling risk)
`PearlPickup`/`WoodPickup`/`StonePickup` Rigidbodies have `m_CollisionDetection: 0` (Discrete) and `m_DefaultMaxDepenetrationVelocity` is 10. Multiple pickups spawned within `scatter` overlap, and the solver can launch an overlapping pickup at up to 10 m/s — fast enough to **tunnel through a thin/static ground** in one discrete step. Secondary suspect if pickups still vanish after the spawn-anchor + upward-bob fixes; the cure would be continuous CCD on the prefab or non-overlapping spawn placement.

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

## Broadphase Distance Queries (`CalculateDistance` + custom `ICollector<DistanceHit>`)
**Context:** Distance-Calc refactor (see [[steering-and-ai]]). Replacing N×M center-distance loops and 8-way `SphereCast` fans with a single point-distance query that returns exact **surface** distances. Verified against `com.unity.physics@1.4.2`.

- **`CollisionWorld.CalculateDistance<T>(PointDistanceInput input, ref T collector)`** is the overload to use (also on `PhysicsWorld.CollisionWorld`). `PointDistanceInput { float3 Position; float MaxDistance; CollisionFilter Filter; }` — `MaxDistance` does the culling.
- **`DistanceHit.Distance => Fraction`, and for *distance* queries `Fraction` is the ABSOLUTE metric distance**, not a normalized 0..1 raycast fraction. (For casts it *is* a 0..1 fraction — don't carry raycast intuition over.) Other fields: `.Position` (world-space closest point on the hit surface), `.Entity`, `.SurfaceNormal`. So `distSq = hit.Distance*hit.Distance` and direction `= normalizesafe(hit.Position - myPos)` are both valid.
- **"Collect every hit in range" collector:** make `MaxFraction` a fixed get-only property == the query radius (never shrink it) and `EarlyOutOnFirstHit => false`. `MaxFraction` shrinking is how you'd narrow toward a single closest hit; keeping it constant + letting `PointDistanceInput.MaxDistance` cull delivers *all* in-range hits to `AddHit`. `AddHit` returns true=accept / false=reject (only affects `NumHits` when you don't shrink).
- **Self-exclusion is mandatory:** the query returns the querying body's own collider at distance 0 — `if (hit.Entity == Self) return false` in `AddHit`.
- **A compound/multi-collider body returns its child colliders too.** A wall whose root has a full-size box *and* small `WallChild` detail colliders on the same layer yields multiple hits; discriminate the ones you want by `ComponentLookup.HasComponent` on `hit.Entity` (e.g. `WallSection` only) and `return false` for the rest. Arena bound cubes on the same Obstacle layer are rejected the same way.
- The collector is a plain struct holding **copies** of the job's `[ReadOnly]` `ComponentLookup`s / hashmaps; no extra `[ReadOnly]` attribute needed on its fields — access is governed by the owning job's field declarations. Works under Burst + `ScheduleParallel` (read-only `PhysicsWorld`).

## DynamicsManager.asset Collision Matrix — Hex Format
`ProjectSettings/DynamicsManager.asset` stores `m_LayerCollisionMatrix` as a single hex string of 32 × 4 bytes (256 chars). Each layer's mask is a 32-bit uint encoded **little-endian** (LSB byte first). To toggle collision between layers A and B you must edit BOTH rows symmetrically — Unity's editor manages this, but if you edit the YAML directly you have to do both yourself, or the asymmetric mask will fail the collision check (Unity Physics builds `CollisionFilter.CollidesWith` per body from one row of the matrix; both directions must agree).

Decoding example: layer 10 (`PickUps`) hex `ff8cfcff` = uint `0xFFFC8CFF`:
- byte 0 (chars 0–1) `ff` = bits 0–7 (Default..Ground)
- byte 1 (chars 2–3) `8c` = `10001100` → bits 8 (Grabbable)=0, 9 (Obstacle)=0, 10 (PickUps)=1, 11=1, 15=1
- etc.

To enable collision between PickUps (10) and Unit (6): set bit 10 in Unit's row AND set bit 6 in PickUps' row.

**Gotcha:** SubScene baking captures `CollisionFilter` at bake time. Editing the matrix requires re-baking the subscene for the change to take effect at runtime.
