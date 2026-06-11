---
name: dots-troubleshoot
description: Symptom-to-fix routing for ECS/DOTS runtime failures in this project. Triggers when debugging Unity console errors or weird ECS behavior - ECB playback exceptions (AssertEntityHasComponent, AssertNoQueuedManagedDeferredCommands), InvalidOperationException on ComponentLookup, ArgumentException on IsComponentEnabled, systems silently not running or matching nothing, components missing at runtime that were added in a Baker, stale enableable flags, units stuck or sliding offscreen, pushes/velocity writes being ignored, entities at wrong world positions.
---

# DOTS Troubleshooting — Symptom Index

Match the symptom, jump to the fix. Most entries have a full writeup in `Claude/learnings/` — file noted per entry. **General rule for exception storms: only the FIRST error matters; later ones in the same frame are usually cascade noise.**

## Exceptions

**`ArgumentException: AssertEntityHasComponent` at ECB playback (names a recording system)**
→ A job did `ECB.Instantiate(prefab)` + `ECB.SetComponent(entity, T)` but the prefab doesn't bake `T`. The error fires at playback in `EndSimulationEntityCommandBufferSystem`, NOT at the buggy call site. Check the authoring that supplies the prefab — it's referencing a prefab missing a sibling `*Authoring`. Fix at bake time with a Baker validation log. (`ecs-patterns.md` → "ECB Playback Failure")

**`AssertNoQueuedManagedDeferredCommands` / multiple ECB errors naming different systems (e.g. DeathSystem)**
→ Cascade noise from the first ECB exception in the same flush. Ignore all but the first. (`ecs-patterns.md` → "ECB Errors Cascade")

**`InvalidOperationException: writes to the ComponentLookup<T> ... must call JobHandle.Complete`**
→ Main-thread code reads T via `ComponentLookup` while a scheduled job writes T (often via an aspect's `EnabledRefRW<T>`). `lookup.Update(ref state)` does NOT sync. Fix: read T through `SystemAPI.Query<EnabledRefRO<T>>` (syncs via state.Dependency) instead of a lookup; `state.CompleteDependency()` is the heavy-handed fallback. (`ecs-patterns.md` → "Aspects")

**`ArgumentException: A component with type:T has not been added to the entity` from `IsComponentEnabled<T>`**
→ A cross-frame tracked `Entity` whose archetype isn't guaranteed. Guard: `Exists && HasComponent<T> && IsComponentEnabled<T>`. (`ecs-combat-and-collisions.md` → "ThrowSettingsSetter Entity-Tracking Pitfall")

**`EntityQueryDescValidationException: duplicate component type name T`**
→ Job uses an aspect containing `EnabledRefRW<T>` AND a `[WithAll/WithDisabled(typeof(T))]` attribute. The aspect already registered T. Move the condition inside the aspect method. (`ecs-patterns.md` → "Aspects")

## Silent no-ops (no error, nothing happens)

**System runs but matches zero entities / `SetComponentEnabled(query, ...)` does nothing**
→ Default queries exclude disabled enableables. `EnabledRefRW<T>` parameters silently make T enabled-only-matching. Fix: `[WithPresent(typeof(T))]` per component or `IgnoreComponentEnabledState` on the query. (`ecs-patterns.md` → "[WithPresent] vs [WithOptions]")

**A job with `ref SomePhysicsComponent` parameter skips entities that clearly should match**
→ The component may not be baked at all. `PhysicsGravityFactor` is NOT auto-baked for gravity-enabled bodies — add it explicitly in a Baker. Verify in the Entity Debugger before debugging logic. (`unity-physics-gotchas.md`)

**Component added in a Baker is missing at runtime**
→ Two known causes: (1) `PostTransformMatrix` (and identity-redundant transform components) get stripped when baked with identity value — add at runtime via an init system with `WithNone<T>`; (2) cross-baker `AddComponent` on a child entity owned by another Baker is unreliable — the child's own Baker must add it. (`ecs-patterns.md` → "PostTransformMatrix", "Cross-Baker AddComponent")

**Event-driven ECS state change does nothing at scene start, works later**
→ Subscenes bake async; the managed event fired before entities existed, and the query ran against an empty set. Use the wake-on-demand `SystemBase` pattern (sleep, retry until query non-empty, apply, sleep). (`ecs-patterns.md` → "Subscene Baking Timing", "Wake-On-Demand")

**Inspector-assigned SO/prefab reference appears set but is null/ignored after bake**
→ Multiple Unity-serialized classes in one file — wiring silently dies. One MonoBehaviour/ScriptableObject per file, filename = class name. (`scriptable-object-patterns.md` → "One Unity-Serialized Class Per File")

## Stale / wrong state

**"I just set an enableable flag via ECB but the reader still sees the old value" (same frame)**
→ EndSim ECB plays at end of `SimulationSystemGroup`; any consumer earlier in that tick reads pre-playback state via lookups. Fix: BeginSim ECB on the producer, direct write, or make the consumer stale-tolerant (single-tick signal pattern). (`ecs-combat-and-collisions.md` → "ECB-vs-Lookup Timing Trap", "Single-Tick Signal Pattern")

**Unit ignores a velocity write / knockback gets eaten next frame**
→ `UnitMoverSystem` rewrites `PhysicsVelocity.Linear.xz` every frame unless `UnableToAct` is enabled. Enable `InAir` (airborne semantics) or `Stun` (grounded) alongside the velocity write. (`ecs-combat-and-collisions.md` → "UnitMoverSystem Velocity Override")

**Unit stuck immobile forever after a hit/landing**
→ Historical class of bug: a signal component doubling as a state machine got stranded by an early-exit. Check that one-shot signals are consumed (disabled) at the TOP of the consumer loop and timers live on separate components. (`ecs-combat-and-collisions.md` → "Single-Tick Signal Pattern")

**Post-physics velocity reads always show pre-sim values / LocalTransform writes get overwritten**
→ System scheduled `[UpdateAfter(PhysicsSimulationGroup)]` can still run BEFORE `ExportPhysicsWorld`. Move to `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]`. (`unity-physics-gotchas.md` → "Post-Physics System Placement")

## Wrong positions / spatial weirdness

**Entities navigate to / spawn near world origin instead of the intended area**
→ `PhysicsCollider...CalculateAabb()` without a `RigidTransform` returns a LOCAL-space AABB. Pass the world pose: `CalculateAabb(new RigidTransform(rot, pos))`. (`unity-physics-gotchas.md`)

**Cursor-proximity / mouse-target checks feel offset, worse with height**
→ `MousePositionProvider.worldMousePosition` projects to a constant-Z plane (screen-space legacy), not the ground. Tilted camera makes the error ~3.7m per meter of height. Project the cursor ray onto the TARGET's Y-plane per consumer; `CursorWorldPosition` stores ray origin+direction for exactly this. (`input-system.md` → "MousePositionProvider", "CursorWorldPosition")

**Boundary/bounce behavior triggers too late or too early at screen edges**
→ Frustum checks must use entity EDGES (4 corners, vertical extent scaled by `abs(camUp.y)`), not center. (`input-system.md` → "Screen Boundary Checks Must Use Entity Edges")

**Two-layer collision enabled in matrix but filters still don't collide (or only one direction)**
→ `DynamicsManager.asset` hex matrix must be edited symmetrically (both layers' rows), AND subscenes capture `CollisionFilter` at bake time — re-bake after matrix edits. (`unity-physics-gotchas.md` → "Collision Matrix Hex")

## Behavior oddities that are NOT bugs (confirmed design)

- `DeathSystem` destroys every `IsDead` entity — intentional; non-dying entities will use `IsInvulnerable` + events.
- `Emotion.Scared` has no writer; ally `IsInvulnerable` never clears — both WIP, don't strip or re-report.
- Damage/physics continuing during pause — interim state; "soft pause" (time-scale) is the plan.
- Allies exempt from screen-bounce damage; ally damage ×0.25 in collisions — balance choices (moving to config per `Claude/ConfigTask.md`).

## Play-mode-specific weirdness

**Ghost double-firing / stale singletons / counters starting non-zero on second Play**
→ Domain reload is OFF. Static mutable state needs a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset next to the state. (`play-mode-and-hot-reload.md`)

**Code edit during Play has no effect**
→ Hot Reload cannot patch Burst-compiled jobs, ECS source-gen (Components/Systems), Reflex bindings, or field additions. Restart Play. (`play-mode-and-hot-reload.md`)
