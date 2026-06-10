# ECS / DOTS Architecture

## System Groups
- **`GameLoopSystemGroup`** (custom, in `SimulationSystemGroup`, after `BeginSimulationECB`): Gameplay systems that should pause with the game. Enabled flag toggled by `DotsGameLoopBridge`.
- **`SteeringSystemGroup`** (custom, inside `GameLoopSystemGroup`, after `BattleBrainSystem`, before `UnitMoverSystem`): Steering reset → seek/obstacle behaviors → resolve. `Steer_ResetSystem` is `OrderFirst = true`; `Steer_ResolveSystem` is `OrderLast = true`.
- **`FixedStepSimulationSystemGroup`**: `ScreenBounceSystem` runs here, after `PhysicsSystemGroup` — physics step has already integrated velocity by then.
- **`PhysicsSystemGroup`**: `InAirCollisionSystem` runs here, after `PhysicsSimulationGroup` (consumes `CollisionEvent`s).
- **`AfterPhysicsSystemGroup`** (Unity.Physics.Systems): `PearlFloatSystem` runs here — see "Unity Physics — Post-Physics System Placement" below for when this matters vs. `[UpdateAfter(PhysicsSimulationGroup)]`.
- **`SimulationSystemGroup, OrderLast = true`**: `ApplyDamageSystem`, `DeathSystem` — run after everything in the frame, including paused state.
- **`InitializationSystemGroup`**: `WallSectionInitSystem` (one-shot setup of `WallCleanupTag`).

## Pipeline (Battle Frame)
1. `SpawningSystem` — timers tick down, instantiate prefabs.
2. `BattleUnitRegistrationSystem` — newly-spawned units get added to `BattleCoordinator`'s `EnemyUnitReference`/`AllyUnitReference` buffers.
3. `BattleDirectorCleanupSystem` — removes dead entities from those buffers.
4. `BattleCoordinatorSystem` — caches faction base AABBs, tracks `IsBattleActive` and `WasCastleBreached`.
4a. `EnemyEscapeSystem` — for each enemy: flips `HasLeftBase` on first frame outside `EnemyBaseBounds`; once left, ticks `HasLeftBase.DwellTimer` while back inside AND (`!IsDayPhaseActive` OR `Scared`); at 2s, enables `Escaped` + `ECB.DestroyEntity`. Just-spawned enemies never escape because `HasLeftBase` starts disabled.
5. `TargetSearchSystem` → schedules `TargetScorerJob` (parallel, blob-config-driven scoring).
6. `BattleBrainSystem` → schedules `BrainDecisionJob`: decides `ActionState` (Stunned/Moving/Attacking/Evading) + `FinalDestination`.
7. `PathfindingDummySystem` — raycasts toward `FinalDestination` through `Obstacle` layer; falls back to fixed gate point if blocked.
8. `Steer_ResetSystem` → `Steer_SeekSystem` + `Steer_ObstacleAvoidanceSystem` → `Steer_ResolveSystem` (8-direction context map).
9. `AbleToActEvaluationSystem` — flips `UnableToAct` enabled based on `Grabbed | InAir | IsDead`.
10. `UnitMoverSystem` — applies `PhysicsVelocity` from `Destination`; skips if attacking/stunned.
11. `AttackSystem` — when in range and cooldown elapsed, appends to target's `DamageBufferElement` buffer (parallel writer).
12. After physics step: `InAirCollisionSystem` → bounces/landings.
13. After PhysicsSystemGroup: `ScreenBounceSystem` reflects off camera frustum edges using 4-corner check.
14. Last: `ApplyDamageSystem` schedules `ApplyDamageJob` which calls `HealthAspect.DrainBufferedDamage()` (sums DamageBuffer, decrements Health, flips `IsDead` *or* `IsInvulnerable`). `DeathSystem.DestroyDeadJob` follows immediately and destroys entities with `IsDead` enabled — same frame as the flip, not next.

## Enableable Components Pattern
Used as flags whose state changes frequently without restructuring chunks:
- `Grabbed`, `InAir`, `IsDead`, `IsInvulnerable`, `UnableToAct`, `SteeringEnabled`, `UnitRegisteredTag`, `Stun`, `Escaped`, `HasLeftBase`
- `AttackCooldownExpirationTimestamp`, `TargetSearchCooldownExpirationTimestamp` — combine timestamp data + enabled bit. System checks `IsComponentEnabled` to skip ready-to-act entities; if `Value > elapsedTime` keep enabled, else disable.
- `HasLeftBase` — same pattern: enableable bit ("has the enemy ever been outside its base") + data field `DwellTimer` (seconds accumulated while back inside under escape conditions). When the timer's relevance is gated by the enableable, fold them into one struct instead of adding a sibling component.
- `SpawnEnemies` — gates all spawn queries; toggled by `SpawningStateSystem` driven by `EventManager.Daylight` events.

## Mark-Then-Destroy Pattern (Alternative to IsDead → DeathSystem)
When a destruction path is semantically distinct from "killed by damage" (e.g., escape), use a dedicated enableable tag + same-frame `EndSimulationECB.DestroyEntity`. Single system handles both: `escaped.ValueRW = true; ECB.DestroyEntity(sortKey, entity);` — the tag exists for the rest of the frame so other systems (analytics, events) can observe before playback. Reuses the IsDead model without conflating semantics. `EnemyEscapeSystem` is the reference: tag is `Escaped`, lives in the same component file (`Components/AI/Escape.cs`) as the state component that drives it (`HasLeftBase`).

## `[WithPresent]` vs `[WithOptions(IgnoreComponentEnabledState)]`
Two ways to make an IJobEntity match disabled enableables — but they're not interchangeable:
- `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` — query-level. Affects **all** components in the query.
- `[WithPresent(typeof(A), typeof(B))]` — per-component override. Required when using `EnabledRefRW<T>` parameters, which otherwise auto-register T as match-enabled-only. Multiple types in one attribute work (`params Type[]`).
Example in `EnemyEscapeJob`: `[WithPresent(typeof(HasLeftBase), typeof(Escaped))]` — both have `EnabledRefRW` parameters and need to match regardless of state so the system can flip them. Also see `BrainDecisionJob` for the single-type form.

To query while ignoring the enabled flag: `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` on `IJobEntity` or in `WithOptions()` on the iterator.

**Critical gotcha — default queries exclude disabled `IEnableableComponent` entities.**  
Both `SystemAPI.Query<>().WithAll<T>()` and `EntityManager.CreateEntityQuery(ComponentType.ReadWrite<T>())` only match entities where `T` is *enabled*. If you need to operate on entities regardless of current enabled state (e.g., to bulk-enable them from disabled), you **must** use `IgnoreComponentEnabledState`:
```csharp
_query = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<SpawnEnemies>()
    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
    .Build(world.EntityManager);
world.EntityManager.SetComponentEnabled<SpawnEnemies>(_query, true); // now works on disabled entities
```
Without this, calling `SetComponentEnabled` to enable a component on baked-disabled entities silently does nothing because the query is empty.

## Subscene Baking Timing — Entities May Not Exist When Managed Events Fire
**Context:** Tried to call `EntityManager.SetComponentEnabled` from a managed bridge on `EventManager.Daylight.DayStarted`.  
**Finding:** Subscenes bake asynchronously. Managed events (e.g. `DayStarted`) can fire *before* any subscene entities exist. A one-shot event handler that calls `SetComponentEnabled` on a freshly built query will silently succeed against an empty result set.  
**Why it matters:** Always assume entities from subscenes may not be available when the first managed event fires, even if the world is created.

## Pattern — Wake-On-Demand Managed System for Deferred ECS State
When a managed bridge needs to set ECS component state but entities may not be loaded yet, use a `SystemBase` that sleeps until needed:
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawningStateSystem : SystemBase
{
    private EntityQuery _query;
    private bool _desiredState;

    protected override void OnCreate()
    {
        _query = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadWrite<SpawnEnemies>() },
            Options = EntityQueryOptions.IgnoreComponentEnabledState
        });
        Enabled = false; // sleep until needed
    }

    public void SetDesiredState(bool isDay) { _desiredState = isDay; Enabled = true; }

    protected override void OnUpdate()
    {
        if (_query.IsEmpty) return; // entities not baked yet — retry next frame
        EntityManager.SetComponentEnabled<SpawnEnemies>(_query, _desiredState);
        Enabled = false; // go back to sleep
    }
}
```
Bridge calls `world.GetOrCreateSystemManaged<SpawningStateSystem>().SetDesiredState(enabled)`. The system retries each frame until entities appear, then applies and sleeps. No polling overhead when idle.

## Component Naming/Layout Conventions
- One file per authoring; struct(s) live in same file below the MonoBehaviour.
- Authoring fields use lowercase (`health`, `damage`); component struct fields use PascalCase (newer) or lowercase (older).
- `Faction` enum: Unknown/Ally/Enemy. `EnemyType: byte` and `AllyType: byte` indexed into blob arrays.
- `Unit.faction` discriminates global allies vs enemies lists held by `BattleCoordinator`.

## ECS Folder/Namespace Split (post-reorg)
`Components/` and `Systems/` each have an `AI/` subfolder. Two namespace tiers:
- **Root files in global namespace (no `namespace` declaration):** `Health`, `Castle`, `Beacon`, `WallSection`, `WallChild`, `BattleCenter`, `BattleCoordinator`, `BounceDamage`, `Spawn`, `Attack`, `Grabbed`, `InAir`, `CameraFrustumData`, `ThrowVelocitySettings` (Components), and `ApplyDamageSystem`, `AttackSystem`, `BattleCoordinatorSystem`, `BattleDirectorCleanupSystem`, `CastleBreachSystem`, `DeathSystem`, `GizmoDrawSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`, `SpawningSystem`, `WallSectionInitSystem` (Systems).
- **`AI/` subfolder in `BarkingBird.Runtime.Gameplay.AI`:** brain/steering/targeting/pathfinding/unit-movement/unit-registration/ability-evaluation — `Ally`, `Enemy`, `Unit`, `UnitMover`, `UnableToAct`, `Target`, `BrainAi`, `BaseArea`, `Pathfinder`, `FakePathfinderResult`, `SteeringContext`, `MovementIntent`, `CombatState`, `UnitRegisteredTag` (Components); `BattleBrainSystem`, `Steer_*`, `TargetScorerJob`, `TargetSearchSystem`, `PathfindingDummySystem`, `UnitMoverSystem`, `BattleUnitRegistrationSystem`, `AbleToActEvaluationSystem` (Systems).

**Why it matters:** When code in root-namespace files references AI types it must `using BarkingBird.Runtime.Gameplay.AI;`; the reverse (AI files referencing root types like `Health`, `Castle`, `WallSection`) needs no using directive because root types are globally accessible. When adding a new file, decide bucket first — AI-pipeline → `AI/` + namespace; shared building block → root + no namespace.

## BattleCoordinator (Singleton)
Single entity bakes `BattleCoordinator` + `FactionBases` + buffers `EnemyUnitReference`/`AllyUnitReference`. `BattleCoordinatorSystem` initialises `FactionBases.AllyBaseBounds`/`EnemyBaseBounds` once it finds 2+ `BaseArea` entities with colliders. `FactionBases.IsInitialized` flag guards subsequent ticks.

Fields:
- `IsBattleActive` — set by coordinator each tick from `EnemyUnitReference.Length > 0`.
- `ForceGlobalReevaluation` — see Castle Breach section.
- `WasCastleBreached` — mirrors `Castle.hasBeenBreached`.
- `IsDayPhaseActive` — baked `true`. Flipped by `DaylightEcsBridge` on `DayStartedEvent`/`DayEndedEvent`. Read by `BattleBrainSystem` and `TargetSearchSystem` to route enemies home at night (see [[steering-and-ai]] — Day Phase Retreat).

## PhysicsCollider AABB — Local vs World (Critical Gotcha)
**Context:** `BattleCoordinatorSystem.SetBases` originally called `colliders[i].Value.Value.CalculateAabb()` to cache base AABBs. Enemy retreat destinations landed at world-space coords near `(-7, 0.5, -6)` — i.e. on the world origin instead of inside the actual base.

**Finding:** `PhysicsCollider.Value.Value.CalculateAabb()` returns the AABB in the **collider's local space**, including the box `Center` offset baked into the geometry. To get a world-space AABB, use the overload `CalculateAabb(RigidTransform)` and pass the entity's world pose:
```csharp
var worldTransform = new RigidTransform(transforms[i].Rotation, transforms[i].Position);
var worldAabb = colliders[i].Value.Value.CalculateAabb(worldTransform);
```
Requires `using Unity.Mathematics;` (RigidTransform) and `using Unity.Transforms;` (LocalToWorld for `.Rotation`/`.Position`).

**Why it matters:** Anything that consumes a `PhysicsCollider`'s AABB for spatial queries (`ClosestPoint`, overlap, frustum check) and assumes world space will silently use local space. `PhysicsUtility.GetRandomPointInsideCollider` is already correct (it transforms); ad-hoc reads at call sites are not. Pattern: when reading collider AABBs, always pair with a `LocalToWorld` query.

## TargetScorerJob (Blob-Driven Scoring)
`TargetProfilesBlob` is a `BlobAssetReference<TargetProfilesBlob>` containing two `BlobArray<TargetProfileBlob>` — enemy and ally profile lookups indexed by `EnemyType`/`AllyType` enum. Built by `BlobContainer.Initialize()` from `PrototypeConfigSetter.EnemyProfiles`/`AllyProfiles` (List<TargetProfile>) on bootstrap. Walls and beacon scored separately by reading `WallEntities`/`BeaconEntity` from system.
**`NativeDisableContainerSafetyRestriction`** on `TargetLookup` is used so the job can read other entities' Target component (for `AggroBonus` cross-check).

## Spawning Strategies
`SpawnAuthoring.strategy`: Point / Area / Radius / Attached → adds one of `SpawnByPoint`/`SpawnByArea`/`SpawnByRadius`/`SpawnByAttached`. Baker also adds `SpawnEnemies : IComponentData, IEnableableComponent` **disabled by default**. `SpawningSystem.OnUpdate` has 4 parallel `foreach`es, each with `.WithAll<SpawnEnemies>()` so only day-active spawners tick. `Spawn()` is declared as a **local method inside `OnUpdate`** because Burst sometimes complains about non-inlined helpers — see the inline comment.

Day/night toggle flow: `DayNightCycle` → `EventManager.Daylight.DayStarted/DayEnded` → `DaylightEcsBridge` → `SpawningStateSystem.SetDesiredState(bool)` → `EntityManager.SetComponentEnabled<SpawnEnemies>(query, bool)` on next frame once entities exist.

## Wall System
- `WallSectionAuthoring` bakes `WallSection { CastleEntity }` + `Health` + `IsDead`. Child wall pieces use `WallChildAuthoring` → `WallReference { ParentWallEntity }` (parent lookup).
- `WallCleanupTag : ICleanupComponentData` is added by `WallSectionInitSystem` on init. When the entity is destroyed, the cleanup tag remains; `CastleBreachSystem` sees `WithNone<WallSection>` + `WallCleanupTag` and flips `Castle.hasBeenBreached = true`, then removes the tag.

## Castle Breach → Targeting Switch
`BattleCoordinator.WasCastleBreached` triggers `ForceGlobalReevaluation`. `TargetScorerJob.CastleIsBreached` skips wall scoring once breached so units re-target. Pattern: anything that should force every unit to re-evaluate targets should set `coordinator.ForceGlobalReevaluation = true` on the coordinator entity.

## Camera Frustum Singleton
`CameraFrustumData` is a single entity created by `BattleCameraBorderSyncBridge.Initialize()`. Bridge runs as `IGameUpdateListener` and writes `WorldToCameraMatrix / Fov / Aspect / IsLive` each frame from Cinemachine main camera. ECS systems (`ScreenBounceSystem`, `ThrowTrajectoryPredictor`) read it as singleton.
`BattleScreenCenter` (`BattleCenterAuthoring`) provides `HalfWidthOffset` / `HalfHeightOffset` that **shrink** the effective play area inside the camera frustum.

## Damage Pipeline (HealthAspect-driven)
- `Health` + `DamageBufferElement` (capacity 8) + `IsDead` (enableable) on any health-bearing entity. Optional `IsInvulnerable` (enableable) opts an entity into death-save behavior.
- Producers append damage via `Ecb.AppendToBuffer(sortKey, targetEntity, new DamageBufferElement{Value=dmg})` from parallel jobs (`AttackSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`).
- `ApplyDamageJob` iterates `HealthAspect` with `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` and calls `hp.DrainBufferedDamage()`. The aspect owns the full invariant: sums the buffer, clamps `Health.Value`, flips `IsDead` on death — OR clamps at 1 and flips `IsInvulnerable` (trip-wire) if the entity has that component and damage would have killed it.
- `DeathSystem.DestroyDeadJob` runs after (`UpdateAfter(ApplyDamageSystem)`), queries `WithAll(IsDead)`, destroys via ECB. The aspect's direct enableable write means destroy happens same-frame as death (old ECB-based flip deferred this by one frame).
- **`Health` no longer carries a value-based `IsDead` getter.** The enableable is the single source of truth. Consumers read via `EnabledRefRO<IsDead>` in a query (preferred) or `ComponentLookup<IsDead>.IsComponentEnabled(entity)` (only when not also writing concurrently — see Aspect section).
- **IsInvulnerable opt-in:** Currently baked only by `AllyAuthoring` (disabled by default). `BattleBrainSystem` reads `ComponentLookup<IsInvulnerable>` and routes the unit to its base when enabled (`Emotion.Scared || isInvulnerable` branch). **No off-switch system exists yet** — once flipped, an ally permanently flees. Recovery condition (timer? heal threshold? cinematic?) is intentionally unspecified; the comment on `IsInvulnerable` declares "Cleared by an external recovery system" as a placeholder.

## ScreenBounceSystem Specifics (see [[input-system]])
- Checks all four entity corners against camera frustum (not center).
- Vertical extent compressed by camera tilt: `ehh = halfHeight * abs(camUp.y)`.
- `vel.z` is preserved across reflections (depth velocity untouched).
- Damage on bounce = `0.5 * BaseDamage * velocityPower` (velocity-curve-based 0..1 scaling from `ThrowVelocitySettings`).
- **Allies are exempt from wall-bounce damage** *and* their `BounceCount` does not increment on wall bounces — physics reflection still applies. Checked via `ComponentLookup<Unit>` + `unit.faction == Faction.Ally`.

## InAirCollisionSystem Specifics
Three collision-event branches, dispatched in `Execute(CollisionEvent)`:
1. **Both entities `InAir`** → mutual `Bounce(entity, normal)` — reflects velocity, applies `BounceElasticity`, increments `BounceCount`, queues damage = `0.5 * BaseDamage * velocityPower`. Skips if `dot(vel, normal) >= 0f` (already separating).
2. **One flying, one grounded but both *have* `InAir` component** → `SoftLand(flyer, grounded)`:
   - **Skips entirely if `length(flyerVel) < 2f`** — physics resolves the contact naturally.
   - Damage split 80/20 between flyer/grounded (each individually scaled by 0.25× if ally).
   - Flyer bounces back at `-originalLinear * 0.25f` and **keeps `InAir` enabled** (critical — see "UnitMoverSystem velocity override" below). Flyer's `BounceCount++`.
   - Grounded receives `horizontalDir * (flyerSpeed * 0.2f)` + upward `flyerSpeed * 0.1f`, and **`InAir` is enabled** so it actually flies (otherwise UnitMoverSystem would override the push).
3. **One flying, other is non-`InAir`** → either `Landed(entity)` (if `other` has `GroundLayerBit` in its `CollisionFilter.BelongsTo`) or `Bounce` (wall/obstacle).

`Landed` damage = `BaseDamage * velocityPower + velocityPower * BounceCount * BounceDamageMultiplier`. Allies scale `Landed` and air-`Bounce` damage by 0.25×; `SoftLand` does the 0.25× per-entity since flyer and grounded may have different factions.

## UnitMoverSystem Velocity Override — Why Pushes Get Eaten
`UnitMoverSystem` has `[WithDisabled(typeof(UnableToAct))]` and **rewrites `PhysicsVelocity.Linear.xz`** every frame from `Destination` (`Y` is preserved). If you write a push velocity to a unit but `UnableToAct` is still disabled, the push gets clobbered next frame.

`AbleToActEvaluationSystem` (runs `[UpdateBefore(UnitMoverSystem)]` in `GameLoopSystemGroup`) sets `UnableToAct = Grabbed || InAir || IsDead`. So the contract is: **to preserve a physics push on a unit, you must also enable `InAir` (or `Grabbed`/`IsDead`)** so `UnableToAct` flips on next frame.

Discovered when SoftLand-cascade bug looked like the grounded unit's knock-back was being ignored — it was being overridden by `UnitMoverSystem`. Fix: enable `InAir` on the grounded entity alongside the velocity write.

**Alternative for grounded-knockback (no airborne semantics): the `Stun` tag.** `Stun : IComponentData, IEnableableComponent { float Remaining }` is OR'd into `UnableToAct` by `AbleToActEvaluationSystem`. `DamagePushSystem` writes `Stun.Remaining = StunDuration` + enables it; `StunSystem` (runs `[UpdateBefore(AbleToActEvaluationSystem)]`) decrements `Remaining` and disables `Stun` at ≤ 0. `UnitMoverJob` skips the unit through the existing `[WithDisabled(UnableToAct)]` filter — no need to make `UnitMover` enableable. This replaces an earlier pattern that toggled `UnitMover` directly via `IEnableableComponent`, which was an implementation leak (the meaning of "disabled mover" really meant "stunned") and was prone to stranding units immobile (see "Single-Tick Signal Pattern" below). Currently `Stun` is baked alongside `DamagePushConfig` in `HitFeedbackAuthoring`; pull it out to `StunAuthoring` if/when a second stun source appears.

## ThrowVelocitySettings
`FixedList512Bytes<float>` of curve samples used by `ScreenBounceSystem` and `InAirCollisionSystem` for unified velocity-power calculation. Built each frame from `_velocityPowerCurve.Evaluate(t)` in `ThrowSettingsSetter.SyncVelocitySettings()` (64 samples). Min/Max velocity bounds also synced.

## ThrowSettingsSetter Entity-Tracking Pitfall
`ThrowSettingsSetter` holds a `_trackedEntity` across throws to display live debug stats. The cleanup check **must** be `Exists && HasComponent<InAir> && IsComponentEnabled<InAir>` — `EntityManager.IsComponentEnabled<T>` throws `ArgumentException("A component with type:T has not been added to the entity")` if the entity exists but lacks T. This trips when a non-unit grabbable (only `InAirAuthoring`-less prefab) becomes the tracked entity, or generally whenever the assumed archetype is not guaranteed.

Pattern: any cross-frame tracked `Entity` that's enabled-state-checked needs `HasComponent` guards because the archetype contract isn't enforced by the field type.

## Where `InAir` Comes From (Baking)
`InAir : IComponentData, IEnableableComponent` is added by **three** authoring scripts: `InAirAuthoring` (standalone, for non-unit grabbables), `AllyAuthoring`, `EnemyAuthoring`. All three add it **disabled**. Anything thrown via `GrabbingInteractor.Release` has its `InAir` enabled there. **A grabbable that uses neither `InAirAuthoring` nor a unit-authoring will never have `InAir`** — `InAirCollisionSystem` and `ScreenBounceSystem` won't process it, and `ThrowSettingsSetter` will throw without the `HasComponent` guard above.

## Gravity Sync
`ThrowSettingsSetter.SyncGravity()` writes `PhysicsStep.Gravity = (0, -Gravity, 0)` every frame. Lets you tune throw arcs from a `MonoBehaviour` inspector field.

## Aspects (IAspect) — Patterns and Gotchas
First aspect added: `HealthAspect` (Components/HealthAspect.cs). Use sparingly — only when the same multi-component access repeats in 3+ systems, OR there's an invariant between components worth enforcing in one place. Otherwise it's overhead with no payoff.

**Not OOP.** Aspects Burst-inline to the same code as `RefRW<T>` field plumbing. No data, no polymorphism. Components stay pure `IComponentData`.

**Optional fields:** `[Optional] private readonly EnabledRefRW<T> _field;` — entity matches even when T is absent. Check `_field.IsValid` before reading `.ValueRO` / writing `.ValueRW`. Used in `HealthAspect` for `IsInvulnerable` (only Ally bakes it).

**`EnabledRefRW<T>` field auto-registers T in the query with *default enabled-state matching*** — meaning the query only matches entities where T is **enabled**. For a "you-flip-it" enableable like `IsDead`, that's wrong: living entities have it disabled, so they'd be skipped. Symptom in this project: damage queued in buffer but never drained for living units. Fix: `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` on the IJobEntity + an early-return guard inside the aspect method (`if (_isDead.ValueRO) return;`).

**Attribute conflict:** Cannot combine an aspect with `EnabledRefRW<T>` and `[WithDisabled(typeof(T))]` / `[WithAll(typeof(T))]` on the same job — Unity throws `EntityQueryDescValidationException: duplicate component type name T`. The aspect already registered T; the attribute tries to re-register. Move the conditional into the aspect method instead.

**IJobEntity Execute signature:** Aspects work directly as Execute parameters: `void Execute(HealthAspect hp) => hp.DoThing();`. Burst-compiled, parallel-safe.

**Aspect writes + main-thread reads of the same component — dependency sync:** `ComponentLookup<T>.Update(ref state)` refreshes caches but does **not** sync against pending writer jobs. If main-thread code reads via lookup while another job writes T (e.g. via aspect's `EnabledRefRW<T>`), you get `InvalidOperationException: writes to the ComponentLookup<T>... must call JobHandle.Complete`. `SystemAPI.Query<EnabledRefRO<T>>` **does** sync via state.Dependency. Fix pattern: pull T into the query as an `EnabledRefRO<T>` field instead of using a lookup. Heavy-handed alternative: `state.CompleteDependency()` before the foreach, but it stalls workers. See `AbleToActEvaluationSystem` for the query-based fix.

## Random in Jobs
`Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime * 10007)` is the pattern used in `SpawningSystem`. Multiplier is a magic prime to spread the seed.

## ECB Playback Failure: `AssertEntityHasComponent` Almost Always Means a Stale Prefab
**Context:** `PearlSpawnOnDeathSystem` ECB threw `ArgumentException: AssertEntityHasComponent` at playback, with cascade noise from `DeathSystem` after.
**Finding:** When a job does `ECB.Instantiate(prefab)` then `ECB.SetComponent(newEntity, T)`, the assertion fails if the prefab wasn't authored with `T`. Symptom is at ECB **playback** (in `EndSimulationEntityCommandBufferSystem`), not at recording, and the message names the **recording** system. Common cause: an `Authoring` MonoBehaviour was assigned a prefab that's missing its sibling component authoring (e.g. PearlSpawnerAuthoring referencing a plain prefab that doesn't have `PearlAuthoring`).
**Why it matters:** Catch this at bake time rather than runtime — let Bakers reject misconfigured prefabs with a clear error:
```csharp
if (authoring.prefab.GetComponent<RequiredAuthoring>() == null)
{
    Debug.LogError($"[{nameof(MyAuthoring)}] prefab '{authoring.prefab.name}' missing RequiredAuthoring.", authoring);
    // bake with Entity.Null so runtime systems skip gracefully
}
```
The `authoring` second arg makes the log clickable in the editor console — links straight to the GameObject. `PearlSpawnerAuthoring.Baker` uses this pattern.

## ECB Errors Cascade — Trust the First, Discard the Rest
When one ECB op throws during playback, subsequent ECB playbacks in the same `EndSimulationEntityCommandBufferSystem.FlushPendingBuffers()` flush often fire follow-on assertions like `AssertNoQueuedManagedDeferredCommands` ("Expected: True; Value was False"). These name *other* systems (e.g. `DeathSystem`) but they're noise — the root cause is the first thrown exception. Read the trace top-down; debug the first ECB error.

## Pattern — RequireForUpdate vs HasSingleton Fallback
`RequireForUpdate<T>` is "this system can't run without T". Use when the system is meaningless absent the singleton (e.g. `PearlSpawnOnDeathSystem` truly needs `PearlSpawnPrefab` to spawn anything).
**Avoid** `RequireForUpdate` when the system has independent responsibilities and the singleton is *optional*. Example: `EnemyEscapeSystem` runs unconditionally to destroy escaped enemies; it only optionally drops pearls if a `PearlSpawnPrefab` singleton exists. Hard-requiring the singleton there would silently break escape behavior in scenes without a pearl spawner. Use `HasSingleton<T>()` + fallback to `Entity.Null` instead.

## Pattern — Capture Boundary-Crossing Position via Per-Frame Field Write
When a system needs to remember "the position where the entity crossed back into region X", don't track a `bool TransitionedThisFrame` + position pair. Instead, write the current position into the struct field **every frame while outside**:
```csharp
if (!insideBase) { data.LastOutsidePosition = currentPos; data.DwellTimer = 0; return; }
// inside: data.LastOutsidePosition holds the last write — i.e. position just before re-entry
```
On the frame the entity transitions to inside, the field already holds the boundary-crossing position; no extra state machine needed. Used in `HasLeftBase.LastOutsidePosition` to anchor scared-escape pearl drops at the base boundary instead of deep inside the inaccessible enemy base. See [[steering-and-ai]].

## Pattern — Defer Counter Increment to Animation Completion
When an ECS event triggers a UI counter bump *and* a fly-to-counter animation, raise the event from ECS but let the **animation's `OnComplete` callback** drive the counter increment, not the event. The counter ticks up exactly when the visual lands, instead of bumping early while pearls are still mid-flight. Implementation: `PearlPickupSystem` raises `PearlPickedUpEvent`; `PearlMagnetController` tween `OnComplete` calls `WorldCurrency.Add(value)`. `WorldCurrency` does **not** subscribe to `PearlPickedUpEvent` directly — it would race with the tween.

## ECS-to-Managed: EventBus from a SystemBase
A managed `SystemBase` in `GameLoopSystemGroup` can call `EventBus.Raise<T>(in evt)` directly from `OnUpdate` — `EventBus` is static, no DI needed, and managed subscribers (MonoBehaviours, POCOs) listen normally. This sidesteps having to bridge through a dedicated "ECS event reader" managed system when the only need is to notify managed code. `PearlPickupSystem` uses this. Don't try this from an `ISystem` (struct, Burst) — `EventBus.Raise` accesses managed types and won't compile in Burst.

## Unity Physics — Post-Physics System Placement (PearlFloatSystem)
**Context:** `PearlFloatSystem` initially used `[UpdateInGroup(typeof(PhysicsSystemGroup))] [UpdateAfter(typeof(PhysicsSimulationGroup))]` (same as `InAirCollisionSystem`). Pearls fell to the ground but never transitioned to floating state — rest timer never accumulated, Y override never showed.
**Finding:** `PhysicsSystemGroup` contains, in order: `PhysicsInitializeGroup` → `PhysicsSimulationGroup` → `ExportPhysicsWorld` → `AfterPhysicsSystemGroup`. `ExportPhysicsWorld` is the system that syncs internal sim state back into `PhysicsVelocity` and `LocalTransform` component buffers. `[UpdateAfter(PhysicsSimulationGroup)]` only constrains "after step 2" — the scheduler is free to place the system before OR after `ExportPhysicsWorld`. If before:
- `PhysicsVelocity.Linear` reads return the PRE-simulation value (whatever was there at frame start). A falling body can show > threshold velocity indefinitely → no rest detection.
- Writes to `LocalTransform.Position` get clobbered by `ExportPhysicsWorld` running after.

**Fix:** Use `[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]` for any system that needs POST-physics `PhysicsVelocity` or that writes `LocalTransform` for rendering.

**Why `InAirCollisionSystem`'s placement works**: it consumes `CollisionEvent` from `SimulationSingleton` (populated DURING `PhysicsSimulationGroup`, available after it) and writes velocity via ECB (which plays back at `EndSimulationEntityCommandBufferSystem`, end of frame — well after `ExportPhysicsWorld`). Reading velocity via `_velocityLookup` for the reflection math gives the velocity AT collision-record time, which is what bounce reflection actually wants.

**Rule of thumb:**
- Reading collision events / writing via ECB → `[UpdateInGroup(PhysicsSystemGroup)] [UpdateAfter(PhysicsSimulationGroup)]` is fine.
- Reading post-sim velocity directly, OR writing `LocalTransform` directly → `[UpdateInGroup(AfterPhysicsSystemGroup)]`.

## Unity Physics — `PhysicsGravityFactor` Is Not Auto-Baked
Unity's `RigidbodyBaker` only adds `PhysicsGravityFactor` for non-default cases (e.g., `useGravity = false` bakes Value=0). A dynamic Rigidbody with `useGravity = true` won't get the component unless you add it explicitly. For runtime gravity toggling, add it in your authoring `Baker`:
```csharp
AddComponent(entity, new PhysicsGravityFactor { Value = 1f });
```
Without this, an `IJobEntity` with `ref PhysicsGravityFactor` parameter silently doesn't match the entity, and the system appears not to run on those entities.

## Unity Physics — Hybrid Kinematic-via-Dynamic Pattern (Pearls)
To make a dynamic body behave like it's hovering in place while still being knockable by other bodies:
1. **Don't** mark it kinematic. Keep it dynamic with its collider.
2. Each post-physics tick: set `PhysicsVelocity.Linear = 0`, `Angular = 0`, set `PhysicsGravityFactor.Value = 0`, write the desired `LocalTransform.Position.y` (sine bob).
3. To detect "I was pushed", check post-physics `lengthsq(velocity.Linear) > thresholdSq` — when a unit overlaps the hover-body, the solver applies impulse to resolve penetration, producing nonzero velocity. That's the wake signal.
4. On wake: clear the velocity-zeroing flag, restore `gravity.Value = 1`. Physics integrates naturally next frame.

**Why not `ICollisionEventsJob` for wake?** Collision events fire every frame for stable resting contacts (pearl on ground, pearl touching pearl). The post-physics velocity check naturally filters: stable contacts produce ~0 velocity (solver fully resolves), real impacts produce > threshold. Simpler and no spurious wakes.

## Unity Physics — Exempting a Layer from Existing Collision-Response Systems
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

## PostTransformMatrix — Bake-Time Identity Strip
**Context:** Hit-feedback squash system wrote `PostTransformMatrix` on a baked body entity via baker `AddComponent(entity, new PostTransformMatrix { Value = float4x4.identity })`. At runtime, `ComponentLookup<PostTransformMatrix>.HasComponent(body)` returned false; squash silently no-op'd.
**Finding:** Unity's post-bake transform reconciliation **removes `PostTransformMatrix` when its baked value is identity**, on the assumption that `LocalTransform`'s uniform scale already covers the case. User-added identity-valued bake components don't survive. Other components (URPMaterialPropertyBaseColor, tags) on the same entity were preserved fine — the strip is specific to transform reconciliation.
**Fix:** Add `PostTransformMatrix` at runtime instead. A one-shot init system in `InitializationSystemGroup` works:
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BodyPostTransformInitSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (_, entity) in SystemAPI.Query<RefRO<BodyVisualTag>>()
                     .WithNone<PostTransformMatrix>().WithEntityAccess())
            ecb.AddComponent(entity, new PostTransformMatrix { Value = float4x4.identity });
    }
}
```
The `WithNone<PostTransformMatrix>` filter makes it idempotent — the query stops matching after the add, so it's effectively one-shot per entity.
**Why it matters:** Any non-uniform scale animation (squash, breathe, dynamic stretch) needs PostTransformMatrix on identity-scale entities. The bake-time path is unreliable; the runtime init pattern is the workaround. Likely also affects other transform components that have "identity = redundant" semantics.

## Cross-Baker `AddComponent` on Child Entities — Unreliable
**Context:** Initially put `AddComponent(bodyEntity, new PostTransformMatrix {...})` inside the **parent's** baker (`HitFeedbackAuthoring.Baker`), resolving the body via `GetEntity(body.gameObject, TransformUsageFlags.Dynamic)`. Component never showed up on the body entity at runtime.
**Finding:** In Entities 1.4, cross-baker `AddComponent` on a child entity (where another Baker owns that entity's primary baking) is inconsistent. The child's own Baker should add components targeting it. Moving the `AddComponent` to `BodyVisualAuthoring.Baker` (the child's own baker) was the right call. (Still didn't help in this case — the bake-time identity-strip got it — but is the correct architectural choice.)
**Why it matters:** Default to "each baker owns its own primary entity's components." Use `GetEntity(otherGO)` for *reading* and dependency tracking; reach for cross-baker writes only when there's no alternative, and verify the result in the Entity Debugger.

## URPMaterialPropertyBaseColor With Built-In URP Shaders — No Setup Needed
**Context:** Considered whether unit body materials needed a Shader Graph with "Allow Material Override" enabled before per-instance `_BaseColor` overrides would work.
**Finding:** Not needed for built-in URP shaders (`URP/Lit`, `URP/SimpleLit`, `URP/Unlit`) — they're DOTS-instanced by default and pick up `URPMaterialPropertyBaseColor` automatically. The "Allow Material Override" toggle is a **Shader Graph-only** mechanism for opting individual properties into DOTS instancing. Built-in shaders take a different path (via the URP shader includes' DOTS instancing block).
**Why it matters:** Adding `URPMaterialPropertyBaseColor` in a baker is enough to drive per-instance tint on URP/SimpleLit bodies — no material/shader changes required. Used by `BodyVisualAuthoring` for damage-flash. The same applies to other shipping components in `Unity.Rendering` (`URPMaterialPropertyEmissionColor`, etc.).

## Damage Feedback — Composable Block Pattern (Hit Feedback)
**Context:** Adding visual juice to damage events (color flash, squash, push) with each effect independently configurable per unit.
**Finding:** Pattern — separate buffer for visual-feedback signals from gameplay damage; one dispatcher fans out to per-effect "blocks"; each block is a `Config + State` component pair (`State` is `IEnableableComponent`), gated by `[Optional]` semantics.
- `HitFeedbackBufferElement` buffer (parallel to `DamageBufferElement`) carries `float3 HitDirection`. Damage producers (`AttackSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`) append to it alongside their damage write.
- `HitFeedbackDispatchSystem` drains the buffer, aggregates direction, and conditionally enables whichever `*State` components the entity has via `SystemAPI.HasComponent<TConfig>()` checks. Runs before `ApplyDamageSystem` so killing blows still get juice.
- Each block: `DamageFlashSystem`, `DamageSquashSystem`, `DamagePushSystem` — each reads `*Config` + `*State` (enableable filter), advances state, writes target component (`URPMaterialPropertyBaseColor` for flash on body entity, `PostTransformMatrix` for squash, `PhysicsVelocity` for push). Each disables its own `*State` when finished.
- Blocks are added by a single `HitFeedbackAuthoring` with three optional SO refs (one per block); a null ref → block absent. Designer composes per-unit by SO assignment.
**Why it matters:** "Effect = component pair + dedicated system" scales linearly: adding a fourth block (e.g. screen shake hint, particle burst trigger) means one new pair + one new system, zero changes to existing blocks or dispatch. Buffer-and-fanout decouples damage producers from visual consumers — producers don't grow per-effect.

## Component File Organization — Don't Pile Unrelated Singletons in One File
**Context:** Initial draft put `Pearl`, `PearlLifetime`, `PearlSettings`, `PearlSpawnPrefab`, AND `CursorWorldPosition` in `Components/Pearl.cs`. The cursor singleton was lifted into its own `Components/CursorWorldPosition.cs` shortly after.
**Finding:** Group `IComponentData` files by **subject**, not by who-introduced-them. A cursor-position singleton fed by an input bridge belongs in its own file (or with other input-side singletons), even if it's first consumed by the pearl pickup system. Keeps file moves cheap when the consumer changes.

## ECB-vs-Lookup Timing Trap (Cross-Group)
**Context:** `InAirCollisionSystem` runs in `PhysicsSystemGroup` and queues `Ecb.SetComponentEnabled<InAir>(entity, true/false)` against `EndSimulationEntityCommandBufferSystem`. That ECB plays at the **end of `SimulationSystemGroup`** — *after* `GameLoopSystemGroup`. Systems in `GameLoopSystemGroup` (e.g. `HitFeedbackDispatchSystem`, `DamagePushSystem`) read `ComponentLookup<InAir>.IsComponentEnabled(entity)` and see the **pre-ECB-playback** state, not the just-queued change.

**Finding:** When a producer queues an enableable flip via `EndSimulation` ECB and a consumer in the same `SimulationSystemGroup` tick reads the flag via lookup, the consumer always sees stale state. This produced the *"stunned unit lies on the ground forever"* bug: `Landed()` queued `InAir = false` *and* added `HitFeedbackBufferElement`; same tick, `DamagePushSystem` saw `InAir == enabled` (stale) and took the in-air bail-out, which previously left `UnitMover` disabled with no countdown to revive it.

**Why it matters:**
- Don't reason about post-ECB state inside the same `SimulationSystemGroup` tick. If you must, either (a) use a `BeginSimulation` ECB on the producer so the change lands at the top of the next tick, (b) write the flag directly (`EntityManager.SetComponentEnabled` from a main-thread system, or `EnabledRefRW` inside the producer's own query) instead of via ECB, or (c) design the consumer so the stale read is harmless (e.g., make side effects idempotent / self-healing, see "Single-Tick Signal Pattern" below).
- The trap is invisible without tracing system-group order against ECB playback. When debugging "the flag I just set isn't showing up," check which ECB system that flag lives on and where it sits relative to your reader.

## Single-Tick Signal Pattern
**Context:** Refactor of `DamagePushState` after the stranded-unit bug above.

**Pattern:** When System A wants to *signal* System B to do something on the next tick, model the signal as `MyEvent : IComponentData, IEnableableComponent` with the minimum payload (e.g. `float3 HitDirWorld`). A enables the bit and writes payload; B reads payload, **disables the bit at the top of its loop body**, then does its work — regardless of any conditional early-exit. The signal is consumed exactly once per dispatch. Any persistent side effect (timer, stun, cooldown) lives on a **separate** data component owned by its own dedicated tick-down system.

**Concrete example (push-and-stun):**
- `DamagePushState { float3 HitDirWorld }` — single-tick signal. `HitFeedbackDispatchSystem` enables; `DamagePushSystem` disables on entry and applies impulse (or bails in air).
- `Stun { float Remaining } : IEnableableComponent` — persistent state. `DamagePushSystem` writes `Remaining = StunDuration`; `StunSystem` decrements and disables at ≤ 0; `AbleToActEvaluationSystem` ORs it into `UnableToAct`.

**Why it matters:**
- A signal component that's also a *state machine* (the old `DamagePushState` carried `Applied: byte` + `Elapsed: float`) is fragile: re-dispatch during an active state resets the state machine, and any early-exit path becomes a potential strand. Single-tick consumption + separate timer eliminates that whole bug class.
- Dispatch becomes idempotent: System A can fire on every hit; the side effect (stun) just refreshes naturally on follow-up hits.
- Each system has one responsibility (signal, impulse, timer, aggregation), each component has one shape (signal payload, persistent state, configuration).
