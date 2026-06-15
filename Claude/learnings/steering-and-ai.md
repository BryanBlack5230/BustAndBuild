# Steering & AI

## Brain → Steering → Mover Pipeline
1. **`BattleBrainSystem`** decides `ActionState` (Stunned/Moving/Attacking/Evading) and writes `FinalDestination`. Also toggles `SteeringEnabled` (off when stunned/attacking).
2. **`PathfindingDummySystem`** raycasts from current pos to `FinalDestination` against `Obstacle` layer. If blocked → `PathTarget = FakePathfinderPoint.GatePosition` (single hard-coded gate entity). If clear → `PathTarget = FinalDestination`. Placeholder for real pathfinding.
3. **`Steer_SeekSystem`** maps `PathTarget` direction onto 8-dir `ContextMap.Interest` weighted by `SteerBehavior_Seek.Weight`.
4. **`Steer_ObstacleAvoidanceSystem`** sphere-casts in 8 directions, fills `ContextMap.Danger`. Caches scan results in `ObstacleShadow` (intervaled, default 0.5s) to avoid per-frame sphere-casting. Apply step adjusts cached danger by entity motion since last scan.
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
`TargetProfilesBlob` is a `BlobAssetReference<TargetProfilesBlob>` containing two `BlobArray<TargetProfileBlob>` — enemy and ally profile lookups indexed by `EnemyType`/`AllyType` enum. Built by `BlobContainer.Initialize()` from the `ConfigHub`'s `EnemyProfiles`/`AllyProfiles` (lists of `EnemyUnitProfile`/`AllyUnitProfile` SO assets) on bootstrap — see [[config-system]]. Walls and beacon scored separately by reading `WallEntities`/`BeaconEntity` from the system. `LowHealthBonus` was **dropped** (was baked-but-never-read) in the 2026-06-14 config refactor.

- Per `EnemyType` and `AllyType` slot. Each profile SO carries its own typed enum; the baker writes it to `array[(int)Type]` (enum-slot contract — list order is irrelevant). Built from `ConfigHub` SO assets (the old inline `List<TargetProfile>` POCO + `ConfigContainer.Battle.*` paths are gone).
- `WeightEnemy / WeightAlly / WeightWall / WeightBeacon` — positive = pursue, set to 0 to skip the category entirely.
- `DistanceWeight` scales `(1 - distSq/detectionRadiusSq)` — used as a "prefer closer" bias.
- `AggroBonus` applies when scanning hostiles list: if the other unit's target is me, score bumps. Read from a `NativeParallelHashMap<Entity, Entity>` snapshot built by `SnapshotTargetsJob` before the scorer runs (replaced the racy `[NativeDisableContainerSafetyRestriction]` lookup, 2026-06-10 — see [[ecs-patterns]] "Snapshot Job" pattern).
- `LineOfSightBonus` applies when `dot(myForward, dirToTarget) >= ViewAngleCos`. The blob still stores the derived `ViewAngleCos`/`DetectionRadiusSq`, but the **authoring** fields are now `ViewAngleDegrees`/`DetectionRadius` (meters), and `BlobContainer.ConvertToStruct` is the only place that derives them — `cos(radians(deg*0.5))` and `r*r` (both 2026-06-14 bug fixes: degrees were treated as cos, radius was squared twice). Authored `90`/`8` are unchanged in behavior.
- `CheckInterval` puts a per-unit cooldown on re-targeting via `TargetSearchCooldownExpirationTimestamp`.

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
