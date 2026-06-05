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

## AbleToActEvaluationSystem
Centralized "should I be active?" check: flips `UnableToAct` enabled whenever `Grabbed || InAir || IsDead` changes. Other systems just check `UnableToAct` enabled state. Runs `[WithOptions(IgnoreComponentEnabledState)]` to see all entities, regardless of their UnableToAct state.

## Target Profiles (Blob)
- Per `EnemyType` and `AllyType` slot. Indexed by enum byte. Built from `PrototypeConfigSetter` (designer-tunable MonoBehaviour) — not yet from `ConfigContainer.Battle.EnemyProfiles`, that path is commented out in `BlobContainer`.
- `WeightEnemy / WeightAlly / WeightWall / WeightBeacon` — positive = pursue, set to 0 to skip the category entirely.
- `DistanceWeight` scales `(1 - distSq/detectionRadiusSq)` — used as a "prefer closer" bias.
- `AggroBonus` applies when scanning hostiles list: if `TargetLookup[other].TargetEntity == me` (they're targeting me), score bumps.
- `LineOfSightBonus` applies when `dot(myForward, dirToTarget) >= ViewAngleCos`. View angle is stored as cosine of half-angle: `cos(radians(ViewAngleCos * 0.5))` — note the field name is misleading (`ViewAngleCos` is the source ANGLE in degrees, then converted on blob build).
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
