# ECS / DOTS Architecture

## System Groups
- **`GameLoopSystemGroup`** (custom, in `SimulationSystemGroup`, after `BeginSimulationECB`): Gameplay systems that should pause with the game. Enabled flag toggled by `DotsGameLoopBridge`.
- **`SteeringSystemGroup`** (custom, inside `GameLoopSystemGroup`, after `BattleBrainSystem`, before `UnitMoverSystem`): Steering reset → seek/obstacle behaviors → resolve. `Steer_ResetSystem` is `OrderFirst = true`; `Steer_ResolveSystem` is `OrderLast = true`.
- **`FixedStepSimulationSystemGroup`**: `ScreenBounceSystem` runs here, after `PhysicsSystemGroup` — physics step has already integrated velocity by then.
- **`PhysicsSystemGroup`**: `InAirCollisionSystem` runs here, after `PhysicsSimulationGroup` (consumes `CollisionEvent`s).
- **`SimulationSystemGroup, OrderLast = true`**: `ApplyDamageSystem`, `DeathSystem` — run after everything in the frame, including paused state.
- **`InitializationSystemGroup`**: `WallSectionInitSystem` (one-shot setup of `WallCleanupTag`).

## Pipeline (Battle Frame)
1. `SpawningSystem` — timers tick down, instantiate prefabs.
2. `BattleUnitRegistrationSystem` — newly-spawned units get added to `BattleCoordinator`'s `EnemyUnitReference`/`AllyUnitReference` buffers.
3. `BattleDirectorCleanupSystem` — removes dead entities from those buffers.
4. `BattleCoordinatorSystem` — caches faction base AABBs, tracks `IsBattleActive` and `WasCastleBreached`.
5. `TargetSearchSystem` → schedules `TargetScorerJob` (parallel, blob-config-driven scoring).
6. `BattleBrainSystem` → schedules `BrainDecisionJob`: decides `ActionState` (Stunned/Moving/Attacking/Evading) + `FinalDestination`.
7. `PathfindingDummySystem` — raycasts toward `FinalDestination` through `Obstacle` layer; falls back to fixed gate point if blocked.
8. `Steer_ResetSystem` → `Steer_SeekSystem` + `Steer_ObstacleAvoidanceSystem` → `Steer_ResolveSystem` (8-direction context map).
9. `AbleToActEvaluationSystem` — flips `UnableToAct` enabled based on `Grabbed | InAir | IsDead`.
10. `UnitMoverSystem` — applies `PhysicsVelocity` from `Destination`; skips if attacking/stunned.
11. `AttackSystem` — when in range and cooldown elapsed, appends to target's `DamageBufferElement` buffer (parallel writer).
12. After physics step: `InAirCollisionSystem` → bounces/landings.
13. After PhysicsSystemGroup: `ScreenBounceSystem` reflects off camera frustum edges using 4-corner check.
14. Last: `ApplyDamageSystem` (sums DamageBuffer, decrements Health) → `DeathSystem` (marks IsDead, destroys).

## Enableable Components Pattern
Used as flags whose state changes frequently without restructuring chunks:
- `Grabbed`, `InAir`, `IsDead`, `UnableToAct`, `SteeringEnabled`, `UnitRegisteredTag`, `UnitMover`
- `AttackCooldownExpirationTimestamp`, `TargetSearchCooldownExpirationTimestamp` — combine timestamp data + enabled bit. System checks `IsComponentEnabled` to skip ready-to-act entities; if `Value > elapsedTime` keep enabled, else disable.

To query while ignoring the enabled flag: `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` on `IJobEntity` or in `WithOptions()` on the iterator.

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

## TargetScorerJob (Blob-Driven Scoring)
`TargetProfilesBlob` is a `BlobAssetReference<TargetProfilesBlob>` containing two `BlobArray<TargetProfileBlob>` — enemy and ally profile lookups indexed by `EnemyType`/`AllyType` enum. Built by `BlobContainer.Initialize()` from `PrototypeConfigSetter.EnemyProfiles`/`AllyProfiles` (List<TargetProfile>) on bootstrap. Walls and beacon scored separately by reading `WallEntities`/`BeaconEntity` from system.
**`NativeDisableContainerSafetyRestriction`** on `TargetLookup` is used so the job can read other entities' Target component (for `AggroBonus` cross-check).

## Spawning Strategies
`SpawnAuthoring.strategy`: Point / Area / Radius / Attached → adds one of `SpawnByPoint`/`SpawnByArea`/`SpawnByRadius`/`SpawnByAttached`. `SpawningSystem.OnUpdate` has 4 parallel `foreach`es, one per type. `Spawn()` is declared as a **local method inside `OnUpdate`** because Burst sometimes complains about non-inlined helpers — see the inline comment.

## Wall System
- `WallSectionAuthoring` bakes `WallSection { CastleEntity }` + `Health` + `IsDead`. Child wall pieces use `WallChildAuthoring` → `WallReference { ParentWallEntity }` (parent lookup).
- `WallCleanupTag : ICleanupComponentData` is added by `WallSectionInitSystem` on init. When the entity is destroyed, the cleanup tag remains; `CastleBreachSystem` sees `WithNone<WallSection>` + `WallCleanupTag` and flips `Castle.hasBeenBreached = true`, then removes the tag.

## Castle Breach → Targeting Switch
`BattleCoordinator.WasCastleBreached` triggers `ForceGlobalReevaluation`. `TargetScorerJob.CastleIsBreached` skips wall scoring once breached so units re-target. Pattern: anything that should force every unit to re-evaluate targets should set `coordinator.ForceGlobalReevaluation = true` on the coordinator entity.

## Camera Frustum Singleton
`CameraFrustumData` is a single entity created by `BattleCameraBorderSyncBridge.Initialize()`. Bridge runs as `IGameUpdateListener` and writes `WorldToCameraMatrix / Fov / Aspect / IsLive` each frame from Cinemachine main camera. ECS systems (`ScreenBounceSystem`, `ThrowTrajectoryPredictor`) read it as singleton.
`BattleScreenCenter` (`BattleCenterAuthoring`) provides `HalfWidthOffset` / `HalfHeightOffset` that **shrink** the effective play area inside the camera frustum.

## Damage Pipeline
- `DamageBufferElement` (capacity 8) on any health-bearing entity.
- Producers append: `Ecb.AppendToBuffer(sortKey, targetEntity, new DamageBufferElement{Value=dmg})` from parallel jobs.
- `ApplyDamageSystem` uses `[WithChangeFilter]` on the buffer, sums and zeroes it, decrements `Health.Value`.
- `DeathSystem` runs immediately after: flips `IsDead` enabled flag, then destroys.

## ScreenBounceSystem Specifics (see [[input-system]])
- Checks all four entity corners against camera frustum (not center).
- Vertical extent compressed by camera tilt: `ehh = halfHeight * abs(camUp.y)`.
- `vel.z` is preserved across reflections (depth velocity untouched).
- Damage on bounce = `0.5 * BaseDamage * velocityPower` (velocity-curve-based 0..1 scaling from `ThrowVelocitySettings`).

## ThrowVelocitySettings
`FixedList512Bytes<float>` of curve samples used by `ScreenBounceSystem` and `InAirCollisionSystem` for unified velocity-power calculation. Built each frame from `_velocityPowerCurve.Evaluate(t)` in `ThrowSettingsSetter.SyncVelocitySettings()` (64 samples). Min/Max velocity bounds also synced.

## Gravity Sync
`ThrowSettingsSetter.SyncGravity()` writes `PhysicsStep.Gravity = (0, -Gravity, 0)` every frame. Lets you tune throw arcs from a `MonoBehaviour` inspector field.

## Random in Jobs
`Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime * 10007)` is the pattern used in `SpawningSystem`. Multiplier is a magic prime to spread the seed.
