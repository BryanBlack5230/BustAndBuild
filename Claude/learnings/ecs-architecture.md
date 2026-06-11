# ECS / DOTS Architecture

The project map: system groups, battle pipeline, world structure, conventions. Sibling files:
- [[ecs-combat-and-collisions]] — damage/death pipeline, bounce/landing systems, hit feedback, throw settings
- [[ecs-patterns]] — reusable DOTS patterns: queries, aspects, ECB gotchas, baking, ECS↔managed bridging
- [[unity-physics-gotchas]] — Unity Physics engine behaviors (AABB spaces, system placement, collision matrix)
- [[steering-and-ai]] — brain → steering → mover pipeline, targeting/scoring

## System Groups
- **`GameLoopSystemGroup`** (custom, in `SimulationSystemGroup`, after `BeginSimulationECB`): Gameplay systems that should pause with the game. Enabled flag toggled by `DotsGameLoopBridge`.
- **`SteeringSystemGroup`** (custom, inside `GameLoopSystemGroup`, after `BattleBrainSystem`, before `UnitMoverSystem`): Steering reset → seek/obstacle behaviors → resolve. `Steer_ResetSystem` is `OrderFirst = true`; `Steer_ResolveSystem` is `OrderLast = true`.
- **`FixedStepSimulationSystemGroup`**: `ScreenBounceSystem` runs here, after `PhysicsSystemGroup` — physics step has already integrated velocity by then.
- **`PhysicsSystemGroup`**: `InAirCollisionSystem` runs here, after `PhysicsSimulationGroup` (consumes `CollisionEvent`s).
- **`AfterPhysicsSystemGroup`** (Unity.Physics.Systems): `PearlFloatSystem` runs here — see [[unity-physics-gotchas]] "Post-Physics System Placement" for when this matters vs. `[UpdateAfter(PhysicsSimulationGroup)]`.
- **`SimulationSystemGroup, OrderLast = true`**: `ApplyDamageSystem`, `DeathSystem` — run after everything in the frame, including paused state.
- **`InitializationSystemGroup`**: `WallSectionInitSystem` (one-shot setup of `WallCleanupTag`).

## Pipeline (Battle Frame)
1. `SpawningSystem` — timers tick down, instantiate prefabs.
2. `BattleUnitRegistrationSystem` — newly-spawned units get added to `BattleCoordinator`'s `EnemyUnitReference`/`AllyUnitReference` buffers.
3. `BattleDirectorCleanupSystem` — removes dead entities from those buffers.
4. `BattleCoordinatorSystem` — caches faction base AABBs, tracks `IsBattleActive` and `WasCastleBreached`.
4a. `EnemyEscapeSystem` — for each enemy: flips `HasLeftBase` on first frame outside `EnemyBaseBounds`; once left, ticks `HasLeftBase.DwellTimer` while back inside AND (`!IsDayPhaseActive` OR `Scared`); at 2s, enables `Escaped` + `ECB.DestroyEntity`. Just-spawned enemies never escape because `HasLeftBase` starts disabled.
5. `TargetSearchSystem` → schedules `TargetScorerJob` (parallel, blob-config-driven scoring; see [[steering-and-ai]]).
6. `BattleBrainSystem` → schedules `BrainDecisionJob`: decides `ActionState` (Stunned/Moving/Attacking/Evading) + `FinalDestination`.
7. `PathfindingDummySystem` — raycasts toward `FinalDestination` through `Obstacle` layer; falls back to fixed gate point if blocked.
8. `Steer_ResetSystem` → `Steer_SeekSystem` + `Steer_ObstacleAvoidanceSystem` → `Steer_ResolveSystem` (8-direction context map).
9. `AbleToActEvaluationSystem` — flips `UnableToAct` enabled based on `Grabbed | InAir | IsDead`.
10. `UnitMoverSystem` — applies `PhysicsVelocity` from `Destination`; skips if attacking/stunned.
11. `AttackSystem` — when in range and cooldown elapsed, appends to target's `DamageBufferElement` buffer (parallel writer).
12. After physics step: `InAirCollisionSystem` → bounces/landings.
13. After PhysicsSystemGroup: `ScreenBounceSystem` reflects off camera frustum edges using 4-corner check.
14. Last: `ApplyDamageSystem` schedules `ApplyDamageJob` which calls `HealthAspect.DrainBufferedDamage()` (sums DamageBuffer, decrements Health, flips `IsDead` *or* `IsInvulnerable`). `DeathSystem.DestroyDeadJob` follows immediately and destroys entities with `IsDead` enabled — same frame as the flip, not next. Details in [[ecs-combat-and-collisions]].

## Enableable Components Pattern
Used as flags whose state changes frequently without restructuring chunks:
- `Grabbed`, `InAir`, `IsDead`, `IsInvulnerable`, `UnableToAct`, `SteeringEnabled`, `UnitRegisteredTag`, `Stun`, `Escaped`, `HasLeftBase`
- `AttackCooldownExpirationTimestamp`, `TargetSearchCooldownExpirationTimestamp` — combine timestamp data + enabled bit. System checks `IsComponentEnabled` to skip ready-to-act entities; if `Value > elapsedTime` keep enabled, else disable.
- `HasLeftBase` — same pattern: enableable bit ("has the enemy ever been outside its base") + data field `DwellTimer` (seconds accumulated while back inside under escape conditions). When the timer's relevance is gated by the enableable, fold them into one struct instead of adding a sibling component.
- `SpawnEnemies` — gates all spawn queries; toggled by `SpawningStateSystem` driven by `DayStartedEvent`/`DayEndedEvent` (EventBus).

## Component Naming/Layout Conventions
- One file per authoring; struct(s) live in same file below the MonoBehaviour.
- Authoring fields use lowercase (`health`, `damage`); component struct fields use PascalCase (newer) or lowercase (older).
- `Faction` enum: Unknown/Ally/Enemy. `EnemyType: byte` and `AllyType: byte` indexed into blob arrays.
- `Unit.faction` discriminates global allies vs enemies lists held by `BattleCoordinator`.

## Component File Organization — Don't Pile Unrelated Singletons in One File
**Context:** Initial draft put `Pearl`, `PearlLifetime`, `PearlSettings`, `PearlSpawnPrefab`, AND `CursorWorldPosition` in `Components/Pearl.cs`. The cursor singleton was lifted into its own `Components/CursorWorldPosition.cs` shortly after.
**Finding:** Group `IComponentData` files by **subject**, not by who-introduced-them. A cursor-position singleton fed by an input bridge belongs in its own file (or with other input-side singletons), even if it's first consumed by the pearl pickup system. Keeps file moves cheap when the consumer changes.

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

## Spawning Strategies
`SpawnAuthoring.strategy`: Point / Area / Radius / Attached → adds one of `SpawnByPoint`/`SpawnByArea`/`SpawnByRadius`/`SpawnByAttached`. Baker also adds `SpawnEnemies : IComponentData, IEnableableComponent` **disabled by default**. `SpawningSystem.OnUpdate` has 4 parallel `foreach`es, each with `.WithAll<SpawnEnemies>()` so only day-active spawners tick. `Spawn()` is declared as a **local method inside `OnUpdate`** because Burst sometimes complains about non-inlined helpers — see the inline comment.

Day/night toggle flow: `DayNightCycle` → `EventBus.Raise(DayStartedEvent/DayEndedEvent)` → `DaylightEcsBridge` → `SpawningStateSystem.SetDesiredState(bool)` → `EntityManager.SetComponentEnabled<SpawnEnemies>(query, bool)` on next frame once entities exist (see [[ecs-patterns]] "Wake-On-Demand Managed System").

## Random in Jobs
`Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime * 10007)` is the pattern used in `SpawningSystem`. Multiplier is a magic prime to spread the seed. **Known flaw** (2026-06-10 review): the cast truncates to whole seconds, so all spawners within the same second share a seed — replace with a stored per-component `Random` state when touched next.

## Wall System
- `WallSectionAuthoring` bakes `WallSection { CastleEntity }` + `Health` + `IsDead`. Child wall pieces use `WallChildAuthoring` → `WallReference { ParentWallEntity }` (parent lookup).
- `WallCleanupTag : ICleanupComponentData` is added by `WallSectionInitSystem` on init. When the entity is destroyed, the cleanup tag remains; `CastleBreachSystem` sees `WithNone<WallSection>` + `WallCleanupTag` and flips `Castle.hasBeenBreached = true`, then removes the tag.

## Castle Breach → Targeting Switch
`BattleCoordinator.WasCastleBreached` triggers `ForceGlobalReevaluation`. `TargetScorerJob.CastleIsBreached` skips wall scoring once breached so units re-target. Pattern: anything that should force every unit to re-evaluate targets should set `coordinator.ForceGlobalReevaluation = true` on the coordinator entity.

## Camera Frustum Singleton
`CameraFrustumData` is a single entity created by `BattleCameraBorderSyncBridge.Initialize()`. Bridge runs as `IGameUpdateListener` and writes `WorldToCameraMatrix / Fov / Aspect / IsLive` each frame from Cinemachine main camera. ECS systems (`ScreenBounceSystem`, `ThrowTrajectoryPredictor`) read it as singleton.
`BattleScreenCenter` (`BattleCenterAuthoring`) provides `HalfWidthOffset` / `HalfHeightOffset` that **shrink** the effective play area inside the camera frustum.
