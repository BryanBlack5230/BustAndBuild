# Distance Calculations Refactor — Agreed Design

**Status:** Steps 1 + 2 + 3 IMPLEMENTED (2026-06-16) — Step 1: `Structure`/`TargetBounds`/`StructureBoundsSystem` + brain surface-distance fix. Step 2: broadphase scorer rewrite (`TargetScorerJob` now runs a `PointDistanceInput` query + `TargetScoringCollector`; beacon scored separately, no gate). Step 3: broadphase steering (`Steer_ObstacleAvoidanceSystem`'s 8 `SphereCast`s → one `CalculateDistance` + `ObstacleDangerCollector`; two-tier proximity + `Curve` preserved per Bryan, danger smeared across 8 `ContextMap` bins). Step 1b is now moot (Step 2 supersedes it). Whole refactor implemented; **not yet play-verified in-editor** (re-tune danger curve/weights — see Step 3 note). See **§2.5 Verified facts** for everything confirmed (resolves the doc's open "verify" items).
**Read before touching:** `BattleBrainSystem`, `TargetScorerJob`/`TargetSearchSystem`, `Steer_ObstacleAvoidanceSystem`, or anything about attack range / target detection / structure geometry.
**Related learnings:** `learnings/steering-and-ai.md`, `learnings/ecs-combat-and-collisions.md`, `learnings/unity-physics-gotchas.md`, `learnings/design-heuristics.md`.
**Skills to invoke when coding:** `dots-new-system` (new components/systems), `unity-coding-standards`. Honor the feedback memories: one authoring class per file; **Claude is read-only on .unity/.prefab** — all entity changes go through bakers (code), never scene/prefab edits.

> Line numbers below are as-of the design session and may have drifted — re-grep before editing.

---

## 1. The problem

Enemies can't get close enough to **attack the beacon** (and the same will hit **walls** / any large building). The beacon's collider/visual is big, so its transform pivot sits far inside the shape. The AI measures **center-to-center** distance, so a unit pressed against the surface is still beyond `AttackRange` of the *center* → it never enters attack range.

Units-vs-units are fine and must stay unchanged: small bodies, center-to-center ≈ surface-to-surface. The attacker is always treated as a **point** (no attacker-radius term) — confirmed acceptable by Bryan; no plans for very large units.

Two independent systems have the center-distance flaw:
1. **`BattleBrainSystem`** — the attack-range gate + move destination (the *reported* bug).
2. **`TargetScorerJob`** — detection-radius gate + prefer-closer weighting (the *upstream* bug: a unit can fail to even **detect** a big structure).

---

## 2. Root-cause facts (verified this session)

### Brain — `Systems/AI/BattleBrainSystem.cs`, `BrainDecisionJob.Execute` (~L106–155)
- `var targetWorldPos = LocalToWorldLookup[target.TargetEntity].Position;` (~L116) = **center**.
- `distToTargetSq = math.distancesq(myWorldPos, targetWorldPos)` (~L118); gate `distToTargetSq <= attackRangeSq` (~L125).
- `targetWorldPos` is reused as the **move destination** (~L135, L151) and the **evade direction** `dirAway = normalize(myWorldPos - targetWorldPos)` (~L144).
- So the fix must replace `targetWorldPos` with an **effective target point** (surface for structures, center for units) used by all three.

### Attack — `Systems/AttackSystem.cs`
- `AttackJob` does **no range check** — it trusts `battleBrain.CanAttack` (~L56). **The brain is the single range-gate site.** Don't add a check here.

### Scorer — `Systems/AI/TargetScorerJob.cs`
- Walls (~L104–133): `distSq = distancesq(myWorldPos, wallWorldPos)` center; **hard gate** `if (distSq > settings.DetectionRadiusSq) continue;` (~L110); weight `(1 - distSq/DetectionRadiusSq) * DistanceWeight` (~L112).
- Beacon (~L135–146): center distance; **NO detection gate** — it's always a candidate when `WeightBeacon > 0` (only a score comparison). Keep this (see decisions).
- Units via `ProcessUnitList` (~L162) use `myPos` = `LocalTransform.Position`, while walls/beacon use `myWorldPos` = `LocalToWorld.Position`. Minor existing inconsistency; the broadphase rewrite uses `myWorldPos` everywhere.
- Still needed regardless of rewrite: cooldown gate, day-phase short-circuit (`!IsDayPhaseActive && Enemy → clear target`), profile selection by `EnemyType`/`AllyType`, `AggroBonus` via the `SnapshotTargetsJob` hashmap (broadphase tells you *who's near*, not *who's aiming at you*).

### Scorer driver — `Systems/AI/TargetSearchSystem.cs`
- Gathers `WallEntities`/`WallTransforms` (~L51–52), beacon singleton (~L54–55), builds `SnapshotTargetsJob` (~L57–64), schedules `TargetScorerJob.ScheduleParallel`, disposes arrays (~L100–102).
- Broadphase rewrite **deletes the wall arrays**; **keeps** the unit-list gather (the snapshot still needs it).

### Steering — `Systems/AI/Steer_ObstacleAvoidanceSystem.cs`
- Already a broadphase consumer: `state.RequireForUpdate<PhysicsWorldSingleton>()` (~L15); `ObstacleOverlapJob` does **8 `PhysicsWorld.SphereCast`s** per scan (~L65–101), intervaled via `ObstacleShadow.Timer`, then `ObstacleApplyJob` does motion-compensation into `SteeringContext.Danger`. Skips walls when the unit is targeting walls (~L73–80).
- Opportunity: replace the 8 casts with **one** `CalculateDistance` query + a custom collector that bins danger into the 8 `ContextMap` directions. Keep the interval/shadow/apply structure.

### Reusable building blocks already in the codebase
- **World-AABB pattern** — `BattleCoordinatorSystem.SetBases` (~L43–79): `colliders[i].Value.Value.CalculateAabb(new RigidTransform(transforms[i].Rotation, transforms[i].Position))`. **Gotcha (unity-physics-gotchas.md): `CalculateAabb()` with no arg returns LOCAL space — always pass the `RigidTransform`.**
- **`Unity.Physics.Aabb` has `.ClosestPoint(float3)`** — used on `FactionBases.{Ally,Enemy}BaseBounds` in the brain (~L88/101/111). `FactionBases` fields are `Aabb` (`BattleCoordinatorAuthoring.cs` ~L36–41).
- **Init-system template** — `WallSectionInitSystem` (InitializationSystemGroup, `WithNone<...>` gate, ECB add). Self-maintaining as walls are built/busted.
- **Layer-bit-in-OnCreate (managed)** pattern — `LayerMask.NameToLayer(...)` used in `Steer_ObstacleAvoidanceSystem` and the pickup-bit code. Layers (unity-physics-gotchas.md): Default 0, **Unit 6**, Ground 7, Grabbable 8, **Obstacle 9**, PickUps 10. **VERIFY the wall layer** (likely Obstacle 9) for the targeting filter.
- **Beacon** (`BeaconAuthoring.cs`): entity has `BeaconTag`, `Health`, `IsDead`, `DamageBufferElement`; its **PhysicsCollider comes from a Unity Physics shape on the GameObject**, not the baker. Walls similarly. So both already have `PhysicsCollider` + `LocalToWorld`.

### Blob caveat
- `TargetProfileBlob` stores **`DetectionRadiusSq`** and `ViewAngleCos` (derived in `BlobContainer.ConvertToStruct`); authoring fields are `DetectionRadius`(m)/`ViewAngleDegrees`. The broadphase query needs the **un-squared `DetectionRadius`** for `MaxDistance` → add it to the blob, or `sqrt` at the call site.

---

## 2.5 Verified facts (2026-06-16, Step 1 session) — resolves the open "verify" items

Confirmed against the live code, `BattleGreyboxProps.prefab`, and the installed `com.unity.physics@1.4.2` source. Trust these over the §7 "verify" list.

- **Wall layer = Obstacle (9).** `UnitStats.cs` sets steering `ObstacleLayer = 512` (= bit 9) and detects walls today; in `BattleGreyboxProps.prefab` every `WallSection`/`WallChild` and the arena `Bounds`/`Inner side` cubes are `{layer:Obstacle}`. → **Step-2 targeting filter `CollidesWith = (1<<6)|(1<<9) = 576`** (Unit 6 + Obstacle 9). No scene edit needed.
  - ⚠️ **CORRECTION (2026-06-17, play-verified):** this "Unit 6" assumption was WRONG. `EnemyTest.prefab`/`AllyTest.prefab` are both `{layer:Grabbable}` (8) — units live on Grabbable (grab/throw), and **no unit prefab uses the Unit layer (6)**. With the original `Unit|Obstacle` filter the broadphase query returned **walls but never units**, so only structures got targeted. Fixed filter: `CollidesWith = (1<<6)|(1<<8)|(1<<9)` (Unit defensively + **Grabbable 8** + Obstacle 9). Collector's `UnitLookup`/`WallLookup` reject any other grabbable. This is the resolution of the Step-2 watch item.
- **Beacon layer = Default (0), NOT Obstacle.** So the `Unit|Obstacle` query *naturally excludes* the beacon — which is exactly what D6 wants (beacon scored separately, no gate). It also means steering never treats the beacon as an obstacle today (pre-existing, out of scope). Beacon has its own `BoxCollider`+`BeaconAuthoring` on one GameObject → bakes `PhysicsCollider`+`LocalToWorld`.
- **`WallSection` root carries its own full-size `BoxCollider`** (the `FrontWall`/`Wall` objects, scl ~12×2.6×1), separate from the small `WallChild`/`WallReference` detail colliders. So `StructureBoundsSystem` (`WithAll<Structure,PhysicsCollider,LocalToWorld>`) matches the wall root and caches the whole-wall AABB. The query will also return `WallChild`/`Bounds`/`Inner` colliders on layer 9 — the Step-2 collector rejects them (`else return false`; `WallLookup` = `WallSection` only, not `WallReference`).
- **Structures bake through their authoring bakers.** Real runtime Castle/walls/Beacon come from `BattleGreyboxProps.prefab` (referenced from `WorldECS.unity`); the `[STRUCTURES]` cubes inside `BattleGroundSceneECS.unity` are **inactive greybox placeholders** (no authoring, don't bake). So adding `Structure` in `WallSectionAuthoring.Bake`/`BeaconAuthoring.Bake` (done in Step 1) is the correct seam.
- **Physics 1.4.2 query API verified — the §6 sketches are CORRECT as written.** `DistanceHit.Distance => Fraction`, and for *distance* queries `Fraction` is the **absolute** surface distance (not a normalized 0..1 raycast fraction). So a collector returning `MaxFraction => DetectionRadius` (constant, never shrinks) is right — `MaxFraction` is in absolute metres here, and `PointDistanceInput.MaxDistance = DetectionRadius` does the cull. Fields confirmed: `.Distance`, `.Position` (world-space closest point on the hit surface), `.Entity`, `.SurfaceNormal`, `.Fraction`. The `CalculateDistance(PointDistanceInput, ref collector)` generic overload exists. `hit.Distance*hit.Distance` for `distSq` and `hit.Position - MyPos` for direction are both valid.
- **Steering sketch type fix (Step 3):** `ObstacleShadow.CachedDanger` is a **`ContextMap`** (8 named floats `N..NW` with an `int` indexer), not a `DangerSet`. The `ObstacleDangerCollector` should fold danger into a `ContextMap Danger` field and copy it into `shadow.CachedDanger`. Also note `SteerBehavior_Obstacle.ObstacleLayer` is a `LayerMask` already plumbed from `UnitStats` (=512); reuse it for the steering query's `CollidesWith`.
- **Line drift:** brain/scorer line numbers have shifted from §2/§5; re-grep (the brain center read is now the `effectiveTargetPos` block ~L121).

### What Step 1 already delivered that Step 2 can lean on
- `Structure` marker + `TargetBounds { Aabb World }` (`Components/StructureBounds.cs`, global ns) now exist on **both** walls and beacon.
- `StructureBoundsSystem` (`Systems/StructureBoundsSystem.cs`, `InitializationSystemGroup`, idempotent `WithNone<TargetBounds>`) caches the world AABB once.
- → **The Step-2 "beacon scored separately" block can read `BeaconBoundsLookup[BeaconEntity].World.ClosestPoint(myWorldPos)` directly** (the beacon `TargetBounds` is populated). Walls also have `TargetBounds`, but per D2 the broadphase path uses the query's live surface distance, not the cached AABB.

### Step 2 prereqs still to do (none blocked)
1. **Blob:** `TargetProfileBlob` stores only `DetectionRadiusSq` (`BlobContainer.ConvertToStruct`, `BlobContainer.cs`). Add an un-squared `DetectionRadius` field for the query `MaxDistance` (or `sqrt(DetectionRadiusSq)` at the call site).
2. **Filter:** build the `Unit|Obstacle` (`576`) `CollisionFilter` in `TargetSearchSystem.OnCreate` (managed `LayerMask.NameToLayer`, like the existing layer-bit code). `BelongsTo = ~0u`.
3. **Self-exclusion:** the query returns the unit's own collider at distance 0 — `if (e == Self) return false` in the collector (already in the sketch).
4. **Plumbing:** pass `PhysicsWorldSingleton.PhysicsWorld`, the filter, `UnitLookup`/`WallLookup`, `BeaconEntity` + `ComponentLookup<TargetBounds>` into `TargetScorerJob`; **delete** the `WallEntities`/`WallTransforms` gather+dispose in `TargetSearchSystem`; **keep** the unit-list gather for `SnapshotTargetsJob`. `TargetSearchSystem` runs in `GameLoopSystemGroup` (after `PhysicsSystemGroup` builds the `CollisionWorld`) → reads a current world; one-frame lag is harmless.

✅ **All four Step-2 prereqs above are now DONE (2026-06-16).** API verified against `com.unity.physics@1.4.2` (`CalculateDistance(PointDistanceInput, ref T collector)`, `DistanceHit.{Distance=Fraction,Position,Entity}`, `PointDistanceInput.{Position,MaxDistance,Filter}`, `ICollector` member set). Collector/query semantics captured in `learnings/unity-physics-gotchas.md` → "Broadphase Distance Queries" and `learnings/steering-and-ai.md` → "Broadphase Surface-Distance Targeting".

### What Steps 1–2 delivered that Step 3 can lean on
- **The broadphase pattern is now proven in-repo.** Copy the scorer's shape: get `PhysicsWorldSingleton`, build a `CollisionFilter`, `CalculateDistance(PointDistanceInput, ref collector)` inside a Burst `IJobEntity`, fold hits in the collector. `learnings/unity-physics-gotchas.md` "Broadphase Distance Queries" is the cheat-sheet (Fraction==absolute distance, fixed `MaxFraction`, self-exclusion, compound-collider child hits, no `[ReadOnly]` needed on collector fields).
- `Steer_ObstacleAvoidanceSystem` **already** does `RequireForUpdate<PhysicsWorldSingleton>()` + `GetSingleton` → **no system-plumbing change**; Step 3 is purely swapping `ObstacleOverlapJob`'s body (the 8-cast loop → one query + collector). Keep `ObstacleShadow.Timer` interval, `shadow.MyLastPos`, and `ObstacleApplyJob` (motion compensation into `context.Danger`) untouched.

### Step 3 prep — verified facts & open tuning calls
1. **Type fix (confirmed live):** `ObstacleShadow.CachedDanger` is a **`ContextMap`** (8 named floats `N..NW`, `int` indexer), NOT a `DangerSet`. The §6 steering sketch's `DangerSet Danger` is **wrong** — use a `ContextMap Danger` field in the collector and copy it into `shadow.CachedDanger` after the query. (The collector can't write `shadow` directly — it accumulates into its own `ContextMap`, then the job assigns `shadow.CachedDanger = collector.Danger`.)
2. **Filter — reuse the config LayerMask, no NameToLayer here:** `SteerBehavior_Obstacle.ObstacleLayer` is a `LayerMask` already plumbed from `UnitStats` (=512 = Obstacle bit 9); the current job builds `CollidesWith = (uint)config.ObstacleLayer.value`. Reuse it verbatim for the query filter. ⚠️ This means steering's filter is **Obstacle-only** (NOT `Unit|Obstacle` like the scorer) and the **beacon (Default layer) is not an obstacle to steering today** — pre-existing, out of scope; don't "fix" it as part of Step 3.
3. **Two wall lookups, not one:** the skip-walls-when-targeting-walls branch checks `WallLookup.HasComponent || WallReferenceLookup.HasComponent` (`WallSection` **and** `WallReference`). The collector needs **both** lookups — the scorer only needed `WallSection`. Preserve: `if (SkipWalls && (Wall||WallRef)) return false;` where `SkipWalls = target.TargetEntity != Null && target.Type == Wall`.
4. **Per-hit front/back range (preserve the asymmetry):** front dirs (`dot(dir, forward) > 0.5`) scan `SurroundRadius + VisionDistance`; others just `SurroundRadius`. Set the collector `MaxFraction = SurroundRadius + VisionDistance` (the max), then reject per-hit when `hit.Distance > range(thisHit)`.
5. **Tuning decision to make (flag for Bryan):** the old job has a **two-tier proximity** — `physicalProximity` (within `SurroundRadius`) maxed against a weaker `visionProximity` (front-only, beyond `SurroundRadius`, ×0.3) — plus a `config.Curve` switch (Linear..Quintuple) and `config.DangerWeight`. The §6 sketch collapses this to one `prox²·DangerWeight`. Decide whether to **preserve the two-tier + curve** (faithful, more knobs) or **adopt the simpler single-curve** (re-tune from scratch). Either way the smear-across-8-dirs (`Danger[k] = max(Danger[k], danger·max(0,dot(dir,GetDir(k)))))`) replaces the old one-bin-per-cast write — expect to re-tune `DangerWeight`/curve regardless (point-distance + smear ≠ swept first-hit).
6. **Shared filter helper (§141) probably NOT worth it:** scorer filter = `Unit|Obstacle` via `NameToLayer`; steering filter = `Obstacle` via config `LayerMask`. Different sources, different masks → forcing a shared helper adds indirection for ~2 lines. Keep them local. Collectors definitely stay separate (best-score fold vs 8-bin danger fold) — that was always the plan (D8).

## 3. Decisions (with rationale)

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Shape model = AABB closest-point** for the brain & beacon (not a per-structure radius, not exact `CalculateDistance`). | Exact for the beacon and axis-aligned/90° walls (a 90° box's AABB == itself). Only diagonal walls slightly overestimate (stop a hair early). Radius is isotropic → bad for long walls. Exact `CalculateDistance` is overkill for boxes. |
| D2 | **Broadphase scorer uses the query's exact surface distance** (`DistanceHit.Distance`/`.Position`). | The narrowphase already computes surface distance — the original bug *dissolves*, no AABB approximation needed on the discovery path. |
| D3 | **Cache structure world-AABB in a `TargetBounds` component** (don't recompute per frame, don't make a "distance system"). | Structures are static → compute once. A *distance system* is the wrong seam: distance is trivial arithmetic, the scorer's distances are N×M & ephemeral, and precomputed distance goes stale (units move, targets change). Extract **geometry**, not distance. The brain (single target, per-frame) reads the cache; arithmetic stays where the data is hot. |
| D4 | **Discriminate structure-vs-unit by `HasComponent<TargetBounds>`, not `target.Type`.** | New structure types need zero brain/scorer edits. |
| D5 | **DetectionRadius stays as authored — no retune.** (Bryan) | Surface-distance widens effective aggro by ~½ the structure size; Bryan is fine with that, values unchanged. |
| D6 | **Beacon = always a candidate, NO range gate.** (Bryan) | A range-bounded broadphase query would drop the beacon past `DetectionRadius`. So the beacon is scored **outside** the query, unconditionally, using its cached `TargetBounds` for surface distance. Keeps today's "always available, distance-penalized" behavior, now measured correctly. |
| D7 | **Attacker is a point** (no attacker-radius term). (Bryan) | Units are fine today; no large units planned. |
| D8 | **Broadphase is the shared spatial backbone for targeting AND steering.** | Two named consumers now exist → a thin shared "build query `CollisionFilter`" helper passes the design-heuristics polymorphism test. Collectors stay separate (different folds: best-score vs 8-bin danger). |
| D9 | **Sequence: surgical brain fix first, then broadphase scorer, then steering.** | Each independently testable; step 1 alone fixes the reported beacon bug with minimal blast radius. |

Open choice carried forward: build the brain/beacon helper on **AABB** (D1) now; the exact `CalculateDistance` path is the documented upgrade if non-box/round/diagonal buildings appear.

---

## 4. New types

- **`Structure`** — marker `IComponentData` on every large attackable building (walls, beacon, future barracks). Added in bakers: `WallSectionAuthoring.Bake`, `BeaconAuthoring.Bake` (code — no scene edits).
- **`TargetBounds { Aabb World }`** — cached world AABB. Added at runtime by the init system (NOT baked: world transform isn't reliably available at bake; see the local-vs-world gotcha).
- **`StructureBoundsSystem`** — InitializationSystemGroup, `WithAll<Structure, PhysicsCollider, LocalToWorld>().WithNone<TargetBounds>()` → compute world AABB via `CalculateAabb(RigidTransform(ltw.Rotation, ltw.Position))`, `ECB.AddComponent`. Runs once per structure; self-maintaining for build/bust. (Structures don't move post-placement; if that ever changes, add a dirty path.)
- ECS struct grouping: `Structure` + `TargetBounds` may share one file (global namespace, like the Escape components). The init system is its own file. Follow `dots-new-system`.

---

## 5. Implementation steps

### Step 1 — Surgical brain fix (`Structure` + `TargetBounds` + init system). **Solves the reported bug.** ✅ DONE 2026-06-16
**Delivered:** `Components/StructureBounds.cs` (`Structure` + `TargetBounds`), `Systems/StructureBoundsSystem.cs`, `Structure` added in `WallSectionAuthoring.Bake`/`BeaconAuthoring.Bake`, and `BrainDecisionJob` now computes `effectiveTargetPos` (surface for `TargetBounds` holders, center for units) feeding the range gate + both move-destinations + the evade `dirAway`. Not yet play-verified in-editor — see test note below.
1. Add `Structure` marker in `WallSectionAuthoring.Bake` and `BeaconAuthoring.Bake`.
2. Add `TargetBounds` component + `StructureBoundsSystem` (template: `WallSectionInitSystem`).
3. In `BrainDecisionJob`: add `[ReadOnly] ComponentLookup<TargetBounds> BoundsLookup;` (populate in `OnUpdate`). Replace the center read (~L116) with:
   ```csharp
   var effectiveTargetPos = LocalToWorldLookup[target.TargetEntity].Position; // unit center, unchanged
   if (BoundsLookup.HasComponent(target.TargetEntity))
       effectiveTargetPos = BoundsLookup[target.TargetEntity].World.ClosestPoint(myWorldPos);
   var distToTargetSq = math.distancesq(myWorldPos, effectiveTargetPos);
   ```
   Then swap the three `targetWorldPos` uses (range gate distance already done; move dest ~L135/L151; evade `dirAway` ~L144) to `effectiveTargetPos`. Branch structure / `CanAttack` / `steerEnabled` unchanged. (No `using Unity.Physics;` needed if `TargetBounds.World` is `Aabb` — but the component file needs it.)
4. **Test:** enemies walk up to the beacon/wall face and attack.

### Step 1b (optional, low-risk) — fix the *existing* brute-force scorer with `TargetBounds`
- In `TargetScorerJob` wall loop + beacon block, replace center distance with `bounds.World.ClosestPoint(myWorldPos)`. Fixes detection gate + weighting correctly **without** the broadphase rewrite. Good intermediate if we want correctness before committing to Step 2. (Skippable if going straight to broadphase.)

### Step 2 — Broadphase scorer rewrite. ✅ DONE 2026-06-16
**Delivered:** `TargetProfileBlob` gained un-squared `DetectionRadius` (`BlobContainer.ConvertToStruct`). `TargetSearchSystem.OnCreate` (now non-Burst) builds `_targetFilter` = Unit(6)|Obstacle(9) via `LayerMask.NameToLayer`, requires `PhysicsWorldSingleton`; `OnUpdate` passes `PhysicsWorld`/filter/`WallLookup`/`BeaconBoundsLookup`, deletes the wall-array gather+dispose, keeps the unit-list gather. `TargetScorerJob` replaced both `ProcessUnitList` calls + the wall loop with one `CollisionWorld.CalculateDistance(PointDistanceInput, ref TargetScoringCollector)`; beacon scored separately via cached `TargetBounds` (no gate, D6). API verified against com.unity.physics@1.4.2. **Play-verified 2026-06-17:** exposed the units-on-Grabbable filter bug — units sit on layer **Grabbable (8)**, not Unit (6), so the original `Unit|Obstacle` filter returned only walls. Fixed by adding the Grabbable bit (see §2.5 correction). Remaining watch item: query trusts physics-layer membership, so confirm dead/dying units don't linger as targets.
1. ~~Prereqs: add un-squared `DetectionRadius`; build targeting `CollisionFilter` in `OnCreate`.~~ Done.
2. ~~`TargetSearchSystem` plumbing; delete wall gather; keep unit-list gather.~~ Done.
3. ~~Replace all-pairs body with the collector; score beacon separately, no gate (D6).~~ Done.
- **Test:** units detect/score nearby hostiles + walls via the query; beacon still chosen from any distance when weighted; perf sane with crowds.

### Step 3 — Broadphase steering. ✅ DONE 2026-06-16
**Delivered:** `ObstacleOverlapJob` now runs **one** `PhysicsWorld.CollisionWorld.CalculateDistance(PointDistanceInput, ref ObstacleDangerCollector)` instead of the 8 `SphereCast`s; `shadow.CachedDanger = collector.Danger` after. Interval/`Timer`/`MyLastPos` + `ObstacleApplyJob` (motion compensation) untouched; no system-plumbing change (already had `PhysicsWorldSingleton`). Filter stays **Obstacle-only** (`(uint)config.ObstacleLayer.value`, reused verbatim) — beacon (Default layer) is still not an obstacle to steering (pre-existing, out of scope). **Two-tier proximity + `Curve` switch preserved** (Bryan's call): `physicalProximity` (within `SurroundRadius`) maxed against front-only `visionProximity` (×0.3), curved, ×`DangerWeight`; then **smeared** across the 8 `ContextMap` bins via `max(0, dot(dirToHit, GetDirection(k)))`. Per-hit front/back `range` preserves the vision asymmetry (`MaxDistance = SurroundRadius + VisionDistance`, reject when `hit.Distance > range`). Collector accumulates a `ContextMap Danger` (not `DangerSet` — §6 sketch was wrong); both `WallSection`+`WallReference` lookups drive the skip-when-targeting-walls branch; self-exclusion defensive. Query is point-distance from `transform.Position` (the old swept cast offset by `AgentRadius` is dropped → `SteeringContext` param removed, kept as `[WithAll]`).
- Semantic shift: swept first-hit-per-ray → point-distance + directional smear (smoother, no 8-way aliasing) **and** center-point distance vs swept-from-`AgentRadius`-offset. **Re-tune `DangerWeight`/`Curve`/`SurroundRadius`/`VisionDistance` in play** — magnitudes will differ.
- **Test:** units flow around walls/arena bounds without sticking; a wall-targeting unit still pushes into its wall; no jitter at chokepoints.

### Shared (once Step 2 + Step 3 both exist)
- Extract a small helper to build the query `CollisionFilter` from layer names in `OnCreate`. Don't over-abstract the collectors (different folds).

---

## 6. Code sketches (illustrative — verify `DistanceHit` fields & the `CalculateDistance(input, ref collector)` overload against the installed Unity.Physics)

### Scorer collector
```csharp
struct TargetScoringCollector : ICollector<DistanceHit>
{
    public bool  EarlyOutOnFirstHit => false;
    public float MaxFraction { get; }          // == DetectionRadius, never shrinks → all in-range hits delivered
    public int   NumHits { get; private set; }

    public Entity  Self;
    public Faction MyFaction;
    public float3  MyPos, MyForward;

    public float WeightEnemy, WeightAlly, WeightWall;
    public float DistanceWeight, LineOfSightBonus, AggroBonus, ViewAngleCos, DetectionRadiusSq;
    public bool  CastleIsBreached;

    [ReadOnly] public ComponentLookup<Unit>        UnitLookup;
    [ReadOnly] public ComponentLookup<WallSection> WallLookup;
    [ReadOnly] public NativeParallelHashMap<Entity, Entity> TargetSnapshot;

    public float BestScore; public Entity BestEntity; public TargetType BestType; public float BestDistSq;

    public TargetScoringCollector(float maxDistance) : this() { MaxFraction = maxDistance; BestScore = float.MinValue; }

    public bool AddHit(DistanceHit hit)
    {
        var e = hit.Entity;
        if (e == Self) return false;                       // query returns our own collider at dist 0

        float baseWeight; TargetType type; bool isHostile = false;
        if (WallLookup.HasComponent(e))
        {
            if (CastleIsBreached || WeightWall <= 0f) return false;
            baseWeight = WeightWall; type = TargetType.Wall;
        }
        else if (UnitLookup.HasComponent(e))
        {
            isHostile  = UnitLookup[e].faction != MyFaction;
            baseWeight = isHostile ? WeightEnemy : WeightAlly;
            if (baseWeight <= 0f) return false;
            type = TargetType.Unit;
        }
        else return false;                                 // debris/pickups/beacon — not scored here

        var distSq = hit.Distance * hit.Distance;          // SURFACE distance, from the query
        var score  = baseWeight + (1f - distSq / DetectionRadiusSq) * DistanceWeight;

        var dir = math.normalizesafe(hit.Position - MyPos);
        if (math.dot(MyForward, dir) >= ViewAngleCos) score += LineOfSightBonus;

        if (isHostile && AggroBonus > 0f &&
            TargetSnapshot.TryGetValue(e, out var theirTarget) && theirTarget == Self) score += AggroBonus;

        NumHits++;
        if (score > BestScore) { BestScore = score; BestEntity = e; BestType = type; BestDistSq = distSq; }
        return true;
    }
}
```

### Scorer Execute (inner body; everything before the loop stays)
```csharp
var collector = new TargetScoringCollector(settings.DetectionRadius) {
    Self = entity, MyFaction = faction, MyPos = myWorldPos, MyForward = myForward,
    WeightEnemy = settings.WeightEnemy, WeightAlly = settings.WeightAlly, WeightWall = settings.WeightWall,
    DistanceWeight = settings.DistanceWeight, LineOfSightBonus = settings.LineOfSightBonus,
    AggroBonus = settings.AggroBonus, ViewAngleCos = settings.ViewAngleCos,
    DetectionRadiusSq = settings.DetectionRadiusSq, CastleIsBreached = CastleIsBreached,
    UnitLookup = UnitLookup, WallLookup = WallLookup, TargetSnapshot = TargetSnapshot,
};

PhysicsWorld.CollisionWorld.CalculateDistance(
    new PointDistanceInput { Position = myWorldPos, MaxDistance = settings.DetectionRadius, Filter = TargetFilter },
    ref collector);

// Beacon: unconditional candidate, NO range gate (D6). Surface distance via cached bounds.
if (settings.WeightBeacon > 0f && BeaconEntity != Entity.Null)
{
    var bp    = BeaconBoundsLookup[BeaconEntity].World.ClosestPoint(myWorldPos);
    var dSq   = math.distancesq(myWorldPos, bp);
    var score = settings.WeightBeacon + (1f - dSq / settings.DetectionRadiusSq) * settings.DistanceWeight;
    if (score > collector.BestScore)
    {
        collector.BestScore = score; collector.BestEntity = BeaconEntity;
        collector.BestType = TargetType.Beacon; collector.BestDistSq = dSq;
    }
}

if (collector.BestType != TargetType.None) { /* write Target from collector.Best* */ }
else { target.TargetEntity = Entity.Null; target.Type = TargetType.None; }
```

### Steering collector (replaces the 8-cast loop)
```csharp
struct ObstacleDangerCollector : ICollector<DistanceHit>
{
    public bool  EarlyOutOnFirstHit => false;
    public float MaxFraction { get; }                 // == SurroundRadius + VisionDistance
    public int   NumHits { get; private set; }

    public Entity Self;
    public float3 MyPos, MyForward;
    public float  SurroundRadius, VisionDistance, DangerWeight;
    public bool   SkipWalls;                           // unit is targeting walls
    [ReadOnly] public ComponentLookup<WallSection>   WallLookup;
    [ReadOnly] public ComponentLookup<WallReference> WallRefLookup;

    public DangerSet Danger;                           // 8 floats; copied into ObstacleShadow.CachedDanger after

    public bool AddHit(DistanceHit hit)
    {
        if (hit.Entity == Self) return false;
        if (SkipWalls && (WallLookup.HasComponent(hit.Entity) || WallRefLookup.HasComponent(hit.Entity))) return false;

        var dir   = math.normalizesafe(hit.Position - MyPos);
        var front = math.dot(dir, MyForward) > 0.5f;
        var range = front ? SurroundRadius + VisionDistance : SurroundRadius;
        if (hit.Distance > range) return false;

        var prox   = math.saturate(1f - hit.Distance / range);
        var danger = prox * prox * DangerWeight;        // curve

        for (int k = 0; k < 8; k++)                     // smear by alignment; max ≈ "nearest blocker"
        {
            var w = math.max(0f, math.dot(dir, SteeringConstants.GetDirection(k)));
            Danger[k] = math.max(Danger[k], danger * w);
        }
        NumHits++;
        return true;
    }
}
```

---

## 7. Verify before/while coding
1. **Blob:** add un-squared `DetectionRadius` (or `sqrt(DetectionRadiusSq)` at use) for the query `MaxDistance`.
2. **Wall layer** for the targeting `CollisionFilter` (probably Obstacle 9 — confirm); filter must include units + walls, exclude beacon/pickups/ground/debris. Beacon excluded because it's scored separately.
3. **System ordering:** `CollisionWorld` is built in `PhysicsSystemGroup`; scorer runs in `GameLoopSystemGroup` (after) → reads a current world; one-frame position lag is harmless for targeting.
4. **`DistanceHit` field names** (`.Distance` / `.Position` / `.Entity`) and the `CalculateDistance(input, ref collector)` generic overload in the installed Unity.Physics.
5. **Self-exclusion** in both collectors (own collider returns at dist 0).
6. **No scene/prefab edits:** `Structure` marker is added in bakers; collider + layers are already authored. If a wall turns out NOT to be on a queryable layer, that's a USER fix — flag it, don't edit the scene.
7. Run the `dots-new-system` checklist for `StructureBoundsSystem` and any new component (group choice, namespace bucket, ECB timing, query attrs).

## 8. Done = 
- Brain: units attack the beacon/walls from the surface (Step 1). 
- Scorer: detection + weighting measured to surfaces; beacon always selectable regardless of distance (Step 2). 
- Steering: one query instead of 8 casts, danger field re-tuned (Step 3). 
- Units-vs-units behavior unchanged throughout. Offer `/knowledge-save` after, for any non-obvious discoveries (collector semantics, fraction-vs-distance, wall layer).
