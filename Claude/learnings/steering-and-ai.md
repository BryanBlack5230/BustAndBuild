# Steering & AI

## Brain → Steering → Mover Pipeline
1. **`BattleBrainSystem`** decides `ActionState` (Stunned/Moving/Attacking/Evading) and writes `FinalDestination`. Also toggles `SteeringEnabled` (off when stunned/attacking).
2. **`PathfindingDummySystem`** raycasts from current pos to `FinalDestination` against `Obstacle` layer. If blocked → `PathTarget = FakePathfinderPoint.GatePosition` (single hard-coded gate entity). If clear → `PathTarget = FinalDestination`. Placeholder for real pathfinding.
3. **`Steer_SeekSystem`** maps `PathTarget` direction onto 8-dir `ContextMap.Interest` weighted by `SteerBehavior_Seek.Weight`.
4. **`Steer_ObstacleAvoidanceSystem`** runs one broadphase `CalculateDistance` point-query (Obstacle layer), fills `ContextMap.Danger` by smearing each hit's danger across the 8 dirs. Caches scan results in `ObstacleShadow` (intervaled, default 0.5s) to avoid per-frame querying. Apply step adjusts cached danger by entity motion since last scan. (Pre-Step-3 this was an 8-way `SphereCast` fan — see "Broadphase Surface-Distance Targeting" §Step 3.)
5. **`Steer_ResolveSystem`** picks best direction: `score = interest - danger * DangerMultiplier` (default 1.0). Writes `Destination = position + bestDir * LookAheadDistance` (default 2.0).
6. **`UnitMoverSystem`** sets `PhysicsVelocity.Linear` toward `Destination`, slerps rotation. Skips if Attacking/Stunned.

## ContextMap (8 directions, N/NE/E/SE/S/SW/W/NW)
Directions are accessed via `SteeringConstants.GetDirection(int index)` — a switch-return method, **not** a static array.  
`private const float DiagonalComponent = 0.70710678118f;` (precomputed 1/√2) is used for diagonal entries.  
This avoids the managed heap allocation that a `static readonly float3[]` field causes.  
NOTE: diagonal values are already unit-length — don't normalize again.

## Brain Decision Tree
Branches in evaluation order — first match wins, all set `CanAttack = false` unless noted:
- **`UnableToAct` enabled** → Stunned. `SteeringEnabled = false`. Brain returns early.
- **`!IsDayPhaseActive && faction == Enemy`** → Moving toward closest point of `EnemyBaseBounds`. Retreat home at night. See "Day Phase Retreat" below.
- **`emotion.Value == Scared || IsInvulnerable enabled`** → Moving toward closest point of own faction's `baseBounds` (retreat).
- **No target** → Moving toward closest point of own `baseBounds`.
- **Has target + on cooldown (Evading)** → If `distSq <= attackRangeSq * 32` (TODO: temporary), retreat 3m from target. Else move toward target.
- **Has target + cooldown ready (Attacking)** → If in range, stop (`finalDest = myPos`, `steerEnabled = false`, `CanAttack = true`). Else move toward target.

## Day Phase Retreat (Belt-and-Suspenders)
When `DayEndedEvent` fires, `DaylightEcsBridge` flips `BattleCoordinator.IsDayPhaseActive = false`. Two independent paths route enemies home, so a single failure (stale Burst, missed singleton write) doesn't break the behavior:

1. **`BattleBrainSystem` reads the singleton** in `OnUpdate`, passes `IsDayPhaseActive` into `BrainDecisionJob`. Branch fires per-frame, no cooldown — immediate retreat.
2. **`TargetSearchSystem` passes the flag into `TargetScorerJob`.** At the top of `Execute`, before the cooldown gate: `if (!IsDayPhaseActive && faction == Enemy) { target.TargetEntity = Null; target.Type = None; return; }`. Per-frame clearing — also defangs `AttackSystem` since `Target.TargetEntity == Null`.

**Why both:** brain branch ensures correct destination even with a stale target; scorer short-circuit ensures correct behavior even if the brain branch fails AND prevents target re-acquisition. The brain's "no target" branch is the natural fallback.

**Only enemies retreat.** Allies are not gated by `IsDayPhaseActive`. Pattern: only one writer of `FinalDestination` exists (the brain) — confirmed by grep — so adding new global behavior gates here is safe; no downstream system overrides.

**Enemies that successfully reach the base are removed by `EnemyEscapeSystem`** (see ecs-architecture pipeline §4a). It gates on `HasLeftBase` so just-spawned enemies aren't culled, and waits 2s of continuous dwell inside `EnemyBaseBounds` under (`!IsDayPhaseActive` OR `Scared`) before flipping `Escaped` + destroying. Uses `LocalToWorld.Position` against `Aabb.Contains` — center-based is correct here because it matches the brain's `baseBounds.ClosestPoint(myWorldPos)` retreat goal (the screen-frustum edge-check rule from `feedback_boundary_checks` does **not** apply to navigation-goal AABB tests).

`HasLeftBase` carries `LastOutsidePosition` (written each frame while outside the base). Used by the **Scared-escape pearl drop** branch: when an enemy escapes via `EmotionalState == Scared`, half the rolled pearl count is spawned at `LastOutsidePosition` (= the position just before crossing back into the enemy base), not at the unit's current pos. Otherwise the drop would happen deep inside the inaccessible enemy base. Day-end escapes (non-Scared) drop nothing. See [[ecs-patterns]] "Capture Boundary-Crossing Position via Per-Frame Field Write" for the general pattern.

**`Emotion.Scared` currently has NO writer** — `EmotionalState` is only set to `Normal` at bake time, so the scared-flee brain branch and the scared pearl drop are unreachable today. Bryan confirmed (2026-06-10) this is **work in progress** — an emotion-evaluation system is planned. Don't strip the branches or report them as dead code.

## AbleToActEvaluationSystem
Centralized "should I be active?" check: flips `UnableToAct` enabled whenever `Grabbed || InAir || IsDead` changes. Other systems just check `UnableToAct` enabled state. Runs `[WithOptions(IgnoreComponentEnabledState)]` to see all entities, regardless of their UnableToAct state.

## Target Profiles (Blob) & TargetScorerJob
`TargetProfilesBlob` is a `BlobAssetReference<TargetProfilesBlob>` containing two `BlobArray<TargetProfileBlob>` — enemy and ally profile lookups indexed by `EnemyType`/`AllyType` enum. Built by `BlobContainer.Initialize()` from the `ConfigHub`'s `EnemyProfiles`/`AllyProfiles` (lists of `EnemyUnitProfile`/`AllyUnitProfile` SO assets) on bootstrap — see [[config-system]]. **As of 2026-06-16 (Step 2) units + walls are discovered by a broadphase distance query, not curated lists; the beacon is scored separately. See "Broadphase Surface-Distance Targeting" below — it supersedes the old `WallEntities`/per-list flow.** `LowHealthBonus` was **dropped** (was baked-but-never-read) in the 2026-06-14 config refactor.

- Per `EnemyType` and `AllyType` slot. Each profile SO carries its own typed enum; the baker writes it to `array[(int)Type]` (enum-slot contract — list order is irrelevant). Built from `ConfigHub` SO assets (the old inline `List<TargetProfile>` POCO + `ConfigContainer.Battle.*` paths are gone).
- `WeightEnemy / WeightAlly / WeightWall / WeightBeacon` — positive = pursue, set to 0 to skip the category entirely.
- `DistanceWeight` scales `(1 - distSq/detectionRadiusSq)` — used as a "prefer closer" bias.
- `AggroBonus` applies only to hostile candidates: if the other unit's target is me, score bumps. Read from a `NativeParallelHashMap<Entity, Entity>` snapshot built by `SnapshotTargetsJob` before the scorer runs (replaced the racy `[NativeDisableContainerSafetyRestriction]` lookup, 2026-06-10 — see [[ecs-patterns]] "Snapshot Job" pattern). The snapshot **still needs the curated enemy/ally lists** even though discovery is now broadphase — the query tells you *who's near*, not *who's aiming at you*. That's why `TargetSearchSystem` keeps the `EnemyUnitReference`/`AllyUnitReference` gather.
- `LineOfSightBonus` applies when `dot(myForward, dirToTarget) >= ViewAngleCos`. The blob stores derived `ViewAngleCos`/`DetectionRadiusSq` **and** (since Step 2) the un-squared `DetectionRadius` (meters) — the broadphase query's `PointDistanceInput.MaxDistance` needs it un-squared. **authoring** fields are `ViewAngleDegrees`/`DetectionRadius`; `BlobContainer.ConvertToStruct` is the only place that derives them — `cos(radians(deg*0.5))` and `r*r` (both 2026-06-14 bug fixes: degrees were treated as cos, radius was squared twice). Authored `90`/`8` are unchanged in behavior.
- `CheckInterval` puts a per-unit cooldown on re-targeting via `TargetSearchCooldownExpirationTimestamp`.

## Broadphase Surface-Distance Targeting (Distance-Calc refactor, 2026-06-16)
**Problem solved:** AI measured **center-to-center** distance. A big structure's transform pivot sits deep inside its collider, so a unit pressed against the surface was still beyond `AttackRange`/`DetectionRadius` of the *center* → it could never attack (or even detect) the beacon/walls. Units-vs-units were fine (small bodies, center≈surface) and must stay unchanged. Full design + decisions: `Claude/DistanceCalculationsRefactorTask.md`.

**Shared geometry cache (Step 1):** `Structure` marker (added in `WallSectionAuthoring.Bake` + `BeaconAuthoring.Bake` — code, no scene edits) + `TargetBounds { Aabb World }` (`Components/StructureBounds.cs`, global ns). `StructureBoundsSystem` (InitializationSystemGroup, `WithAll<Structure,PhysicsCollider,LocalToWorld>().WithNone<TargetBounds>`) computes the world AABB **once** via `CalculateAabb(RigidTransform(ltw.Rotation, ltw.Position))` — local-vs-world gotcha, see [[unity-physics-gotchas]] — and is self-maintaining for build/bust. Structures are static so a one-shot AABB is correct.

**Discriminate structure-vs-unit by `HasComponent<TargetBounds>`, not `Target.Type`** (D4) — new structure types need zero brain/scorer edits.

**Brain (Step 1, `BattleBrainSystem`/`BrainDecisionJob`):** computes `effectiveTargetPos` = `BoundsLookup[target].World.ClosestPoint(myWorldPos)` for structures, plain center for units. It feeds the range gate **and** the move destination **and** the evade `dirAway`. `AttackSystem` adds no range check — the brain is the single range-gate site.

**Scorer (Step 2, `TargetScorerJob`/`TargetSearchSystem`):** the all-pairs unit loops + the wall loop are replaced by **one** `PhysicsWorld.CollisionWorld.CalculateDistance(PointDistanceInput, ref TargetScoringCollector)`. The query returns each in-range collider's **exact surface distance** (`DistanceHit.Distance`), so the center-distance bug dissolves with no AABB approximation on the discovery path (D2). Collector folds hits into a single best-score pick (self-exclusion, wall-vs-unit via `WallLookup`/`UnitLookup`, hostile/ally via `Unit.faction != MyFaction`). See [[unity-physics-gotchas]] "Broadphase Distance Queries" for the collector/query semantics.
- **Targeting filter** built in `TargetSearchSystem.OnCreate` (now non-Burst — `LayerMask.NameToLayer` is managed): `CollidesWith = (1<<Unit 6)|(1<<Grabbable 8)|(1<<Obstacle 9)`, `BelongsTo = ~0u`. Walls live on **Obstacle (9)**; the **beacon is on Default (0)** so the query *naturally excludes it*.
  - ⚠️ **Units live on the GRABBABLE layer (8), NOT the "Unit" layer (6)** — verified 2026-06-17 against `EnemyTest.prefab`/`AllyTest.prefab` (both `{layer:Grabbable}`); they're on Grabbable so the grab/throw raycast can pick them up, and **no unit prefab uses the "Unit" layer**. The Step-2 filter originally used only `Unit|Obstacle`, so the broadphase query returned **walls but never units** → only structures were targeted. Fix: add the Grabbable bit (Unit bit kept defensively). This **resolves** the Step-2 "watch item" below — units were dropping out, not lingering. The old curated-list scorer was layer-agnostic, which is why this only surfaced once discovery moved to the physics query. Non-unit grabbables (none today; pickups are on PickUps 10) would be rejected by `UnitLookup` in the collector anyway.
- **Beacon = always a candidate, NO range gate** (D6): scored outside the query, unconditionally when `WeightBeacon > 0`, using its cached `TargetBounds.World.ClosestPoint` for surface distance (center fallback for the one tick before `StructureBoundsSystem` populates it). A range-bounded query would otherwise drop it past `DetectionRadius`.
- Ordering: `TargetSearchSystem` runs in `GameLoopSystemGroup` (after `PhysicsSystemGroup` builds the `CollisionWorld`) and `RequireForUpdate<PhysicsWorldSingleton>()`. One-frame position lag is harmless for targeting.
- **Watch item (not yet play-verified):** the query trusts **physics-layer membership**, not the coordinator's curated lists. If a dead/dying unit briefly retains its `Unit` component + collider on the Unit layer, the query can surface it where the old list path wouldn't. Confirm dead units don't linger as targets.

**Step 3 (steering, DONE 2026-06-16):** `Steer_ObstacleAvoidanceSystem`'s 8 `SphereCast`s are replaced by **one** `PhysicsWorld.CollisionWorld.CalculateDistance(PointDistanceInput, ref ObstacleDangerCollector)`; `shadow.CachedDanger = collector.Danger` after. Interval/`Timer`/`MyLastPos` + `ObstacleApplyJob` (motion compensation) unchanged; no system-plumbing change (already had `PhysicsWorldSingleton`).
- **Filter is Obstacle-only** (`CollidesWith = (uint)config.ObstacleLayer.value`, reused verbatim) — NOT `Unit|Obstacle` like the scorer. The **beacon (Default layer) is not an obstacle to steering** (pre-existing, out of scope).
- **`ObstacleShadow.CachedDanger` is a `ContextMap`** (8 named floats, `int` indexer), NOT a `DangerSet` — the task §6 sketch was wrong. The collector accumulates a `ContextMap Danger`; the job copies it out.
- **Two-tier proximity + `Curve` switch preserved** (Bryan's call, not the sketch's single `prox²`): `physicalProximity = saturate(1 - d/SurroundRadius)` maxed against front-only `visionProximity = saturate(1 - d/range)*0.3`, curved (`Linear..Quintuple`), ×`DangerWeight`. Then **smeared** across all 8 dirs: `Danger[k] = max(Danger[k], danger * max(0, dot(dirToHit, GetDirection(k))))` — one obstacle bleeds into adjacent bins (smoother than the old one-bin-per-cast write).
- **Front-vision asymmetry** preserved per-hit: `range = front ? SurroundRadius+VisionDistance : SurroundRadius` (`front = dot(dirToHit, forward) > 0.5`); `MaxDistance = SurroundRadius+VisionDistance`, reject when `hit.Distance > range`.
- **Two wall lookups** for skip-when-targeting-walls: `WallSection` (root sections) **and** `WallReference` (detail child colliders) — both needed here, where the scorer only used `WallSection`.
- **Semantic shifts → re-tune in play:** swept first-hit-per-ray → point-distance + smear, AND distance is now from `transform.Position` center (the old swept cast offset by `AgentRadius` is dropped). Expect `DangerWeight`/`Curve`/`SurroundRadius`/`VisionDistance` magnitudes to need adjustment. See [[unity-physics-gotchas]] "Broadphase Distance Queries".

## Override From Coordinator
Whenever a unit set changes (registration / cleanup / castle breach), `BattleCoordinator.ForceGlobalReevaluation = true`. `TargetSearchSystem` reads it once, resets it after dispatching the job, and every unit re-evaluates that tick regardless of its own cooldown.

## Attack Pipeline
- `AttackData { Damage, CooldownTime, AttackRange }` on unit.
- `BattleBrain.CanAttack` is set by brain when in range.
- `AttackSystem` checks `CanAttack && !cooldownEnabled && target != Null`; bumps cooldown, appends damage to target's `DamageBufferElement` (parallel writer).
- Cooldown is timestamp-based (`expirationTime = elapsed + CooldownTime`); a tiny loop at the top of `AttackSystem.OnUpdate` re-enables cooldown components whose timestamps lapsed (`if expirationTime <= elapsed → enabled = false`).

## Common Gotchas
- `IJobEntity` parameters: `EnabledRefRW<T>` (writable enabled state), `EnabledRefRO<T>` (read), `RefRW<T>`/`RefRO<T>` for component data. Mixing `in T` and `RefRO<T>` is fine but pick one per parameter.
- `state.Dependency = job.ScheduleParallel(state.Dependency)` — always thread through state.Dependency to chain jobs correctly.
- Component lookups require `.Update(ref state)` at the top of each `OnUpdate` if cached.
- `[WithDisabled(typeof(UnableToAct))]` filters in entities where the component is disabled (used by `UnitMoverJob` — only move alive/non-grabbed units).
- `[WithPresent(typeof(SteeringEnabled))]` ensures the component is present regardless of enabled state.
