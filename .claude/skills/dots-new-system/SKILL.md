---
name: dots-new-system
description: Checklist for creating new ECS/DOTS systems, components, jobs, or authoring scripts in this project. Triggers whenever adding an ISystem/SystemBase, IComponentData, IJobEntity, Baker/Authoring MonoBehaviour, or extending the battle simulation pipeline. Encodes project-specific decisions - system group choice, ordering, namespace bucket, query attributes, ECB timing, config plumbing - that are easy to get silently wrong.
---

# New ECS System / Component Checklist

Walk this top to bottom whenever creating ECS code. Each step has a wrong default that compiles fine and fails silently. Deep dives: `Claude/learnings/ecs-architecture.md` (map), `ecs-patterns.md` (query/ECB/aspect gotchas), `ecs-combat-and-collisions.md` (damage/death), `unity-physics-gotchas.md` (physics placement).

## 1. Which system group?

| Need | Group | Note |
|---|---|---|
| Normal gameplay logic | `[UpdateInGroup(typeof(GameLoopSystemGroup))]` | **Default.** Pauses with the game (`DotsGameLoopBridge` toggles `Enabled`). |
| Steering pipeline | `SteeringSystemGroup` (inside GameLoop) | Between `BattleBrainSystem` and `UnitMoverSystem`. |
| Consumes `CollisionEvent`s | `PhysicsSystemGroup` + `[UpdateAfter(PhysicsSimulationGroup)]` | Write results via ECB only. |
| Reads POST-physics velocity or writes `LocalTransform` | `AfterPhysicsSystemGroup` | `[UpdateAfter(PhysicsSimulationGroup)]` alone is NOT enough — `ExportPhysicsWorld` may run after you and clobber/stale-read. |
| Must run even while paused (damage/death tier) | `SimulationSystemGroup, OrderLast = true` | Deliberate decision — current pause is a hard toggle, but a "soft pause" (time-scale) is planned; justify pause-exemption in the PR. |
| One-shot init | `InitializationSystemGroup` | Use `WithNone<AddedComponent>` to make it idempotent. |

**Ordering:** if your system reads what another system wrote this frame, add explicit `[UpdateAfter(typeof(Producer))]`. Creation order is NOT a contract (this bit `AttackSystem` vs `BattleBrainSystem`).

## 2. Which folder + namespace?

- AI pipeline (brain/steering/targeting/pathfinding/unit movement) → `!_Scripts/{Components|Systems}/AI/`, namespace `BarkingBird.Runtime.Gameplay.AI`.
- Everything else ECS → root `!_Scripts/{Components|Systems}/`, **global namespace** (no namespace declaration) — DOTS convention here.
- One authoring per file; component structs may live under their authoring. Unrelated singletons get their own file. Multiple Unity-serialized classes (MonoBehaviour/SO) per file silently break inspector wiring — never.

## 3. Component design

- Frequently toggled boolean state → `IEnableableComponent` flag, baked **disabled** unless stated otherwise. Pair an enableable bit with its data fields in ONE struct when they're coupled (`HasLeftBase { DwellTimer, LastOutsidePosition }`).
- Cooldowns → timestamp component + enabled bit (`AttackCooldownExpirationTimestamp` pattern).
- Cross-system one-shot signal → single-tick signal component: producer enables + writes payload; consumer **disables at the top of its loop body**, then acts. Persistent side effects (timers) live on a separate component with its own tick-down system.
- New SO-driven per-unit behavior → follow the hit-feedback block pattern: `Config` (plain) + `State` (enableable) pair, optional SO ref on the authoring, dedicated system per block.

## 4. Query / job attributes (silent-failure zone)

- Default queries **exclude disabled enableables**. To match regardless of state: `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` (whole query) or `[WithPresent(typeof(A), typeof(B))]` (per component — required alongside `EnabledRefRW<T>` parameters).
- `EnabledRefRW<T>` parameter auto-registers T as *enabled-only matching* — the #1 cause of "system runs but matches nothing".
- Aspects with `EnabledRefRW<T>` cannot also use `[WithAll/WithDisabled(typeof(T))]` on the job → `EntityQueryDescValidationException`. Move the check inside the aspect.
- Need to read OTHER entities' component X while the same parallel job writes X? **Snapshot job first** (`NativeParallelHashMap` filled by a pre-scheduled `IJob`), never `[NativeDisableContainerSafetyRestriction]`.

## 5. Structural changes / ECB

- Standard: `EndSimulationEntityCommandBufferSystem.Singleton` → `CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()`.
- **Timing trap:** EndSim ECB plays at end of `SimulationSystemGroup` — a consumer later in the SAME tick reading via `ComponentLookup` sees pre-playback state. If a same-tick reader exists: use a `BeginSimulation` ECB, write directly, or make the consumer stale-tolerant.
- `ECB.SetComponent` on an instantiated prefab requires the prefab to actually bake that component — validate prefabs in the Baker (`GetComponent<RequiredAuthoring>() == null → Debug.LogError(msg, authoring)`), or you get `AssertEntityHasComponent` at playback.
- Repeated spawn/configure blocks → static Burst-friendly helper (see `PearlSpawnUtility`): pass ECB and `Random` by `ref`, settings by `in`.

## 6. Singleton dependencies

- System meaningless without T → `state.RequireForUpdate<T>()`.
- T is optional garnish → `SystemAPI.HasSingleton<T>()` + fallback; do NOT hard-require (it silently disables the system's other duties).
- **No `default` fallbacks for settings structs** — a zeroed settings struct produces silently-wrong behavior; prefer skipping the optional feature.

## 7. Values & config

- **No magic numbers in jobs/systems.** Tunables → config (see `Claude/ConfigTask.md`: `*Config` SOs → blob for per-unit-type arrays, `IComponentData` singleton for flat scalar groups, `ThrowSettingsSetter`/`PearlSettings` as the reference pattern). Per-unit-type values (damage multipliers, body extents) belong in unit profiles, not system globals.
- `Random` in jobs: do NOT seed from `(uint)ElapsedTime` (truncates to seconds → shared seeds). Seed from a stored per-component `Random` state, or at minimum mix entity index AND a frame-unique value.

## 8. Baking

- `PhysicsGravityFactor` is NOT auto-baked for gravity-enabled bodies — add explicitly if any job queries it.
- `PostTransformMatrix` baked as identity gets stripped — add at runtime via an init system instead.
- Each Baker adds components only to its OWN entity; cross-baker `AddComponent` on children is unreliable.
- Subscenes bake async: managed code must not assume entities exist at first event — use the wake-on-demand `SystemBase` pattern (`Enabled = false`, retry until query non-empty).

## 9. Mono ↔ ECS boundary

- Mono → ECS: bridge class (`*EcsBridge`), registered in the scene installer, writes singleton components / calls managed systems.
- ECS → Mono: `EventBus.Raise` from a managed `SystemBase` only — never from Burst `ISystem`.
- MonoWorld code caches `EntityQuery`s in `Initialize()` and disposes them in `Dispose()` — never `CreateEntityQuery` per frame/click.

## 10. Before declaring done

- New tunables documented? Stale docs (CLAUDE.md tree, `Claude/learnings/`) updated if structure moved?
- If the system spawns/destroys entities: does anything else hold references to them (coordinator buffers, tracked entities)? Buffers are cleaned by `BattleDirectorCleanupSystem`; tracked `Entity` fields need `Exists && HasComponent` guards.
- If touching death/health: `DeathSystem` destroys EVERY `IsDead` entity — entities that must survive use `IsInvulnerable` (design decision, see `Claude/learnings/ecs-combat-and-collisions.md`).
