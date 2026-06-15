# ECS Combat & Collisions

Damage/death pipeline, collision-response systems, throw settings, hit feedback. Split out of [[ecs-architecture]] (system groups + battle pipeline live there); generic DOTS patterns are in [[ecs-patterns]]; Unity Physics engine gotchas in [[unity-physics-gotchas]].

## Damage Pipeline (HealthAspect-driven)
- `Health` + `DamageBufferElement` (capacity 8) + `IsDead` (enableable) on any health-bearing entity. Optional `IsInvulnerable` (enableable) opts an entity into death-save behavior.
- Producers append damage via `Ecb.AppendToBuffer(sortKey, targetEntity, new DamageBufferElement{Value=dmg})` from parallel jobs (`AttackSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`).
- `ApplyDamageJob` iterates `HealthAspect` with `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` and calls `hp.DrainBufferedDamage()`. The aspect owns the full invariant: sums the buffer, clamps `Health.Value`, flips `IsDead` on death — OR clamps at 1 and flips `IsInvulnerable` (trip-wire) if the entity has that component and damage would have killed it.
- **Healing is supported via negative `DamageBufferElement` values** (2026-06-10): `DrainBufferedDamage` clamps the result to `[0, Health.Max]` (`math.clamp`), so heals can't overshoot max. No separate heal buffer exists or is planned for now.
- `DeathSystem.DestroyDeadJob` runs after (`UpdateAfter(ApplyDamageSystem)`), queries `WithAll(IsDead)`, destroys via ECB. The aspect's direct enableable write means destroy happens same-frame as death (old ECB-based flip deferred this by one frame).
- **`Health` no longer carries a value-based `IsDead` getter.** The enableable is the single source of truth. Consumers read via `EnabledRefRO<IsDead>` in a query (preferred) or `ComponentLookup<IsDead>.IsComponentEnabled(entity)` (only when not also writing concurrently — see [[ecs-patterns]] "Aspects").
- **IsInvulnerable opt-in:** Currently baked only by `AllyAuthoring` (disabled by default). `BattleBrainSystem` reads `ComponentLookup<IsInvulnerable>` and routes the unit to its base when enabled (`Emotion.Scared || isInvulnerable` branch). **No off-switch system exists yet** — once flipped, an ally permanently flees. Recovery condition (timer? heal threshold? cinematic?) is intentionally unspecified; the comment on `IsInvulnerable` declares "Cleared by an external recovery system" as a placeholder.
- **Design intent confirmed by Bryan (2026-06-10):** `DeathSystem` destroying *every* `IsDead` entity is intentional. `IsInvulnerable` is THE planned mechanism for entities that must not die (walls, beacon, allies, future barracks) — it will be added to all of them, and reaching the 1-HP trip-wire will raise an event (e.g. broken-wall visuals, beacon stops daylight) instead of destruction. The missing recovery system and the missing `Emotion.Scared` writer are **work in progress, not forgotten** — don't strip those "dead" branches, and don't re-report them as bugs.
- **Pause intent (Bryan, 2026-06-10):** damage/death/physics continuing during pause is an open question because the plan is a **"soft" pause** — time scaled to a very small value rather than fully stopped. Keep this in mind before "fixing" pause-exempt systems.

## Mark-Then-Destroy Pattern (Alternative to IsDead → DeathSystem)
When a destruction path is semantically distinct from "killed by damage" (e.g., escape), use a dedicated enableable tag + same-frame `EndSimulationECB.DestroyEntity`. Single system handles both: `escaped.ValueRW = true; ECB.DestroyEntity(sortKey, entity);` — the tag exists for the rest of the frame so other systems (analytics, events) can observe before playback. Reuses the IsDead model without conflating semantics. `EnemyEscapeSystem` is the reference: tag is `Escaped`, lives in the same component file (`Components/AI/Escape.cs`) as the state component that drives it (`HasLeftBase`).

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
`FixedList512Bytes<float>` of curve samples used by `ScreenBounceSystem` and `InAirCollisionSystem` for unified velocity-power calculation. Baked **once** (64 samples + min/max bounds) by `BlobContainer.BuildThrowVelocitySettings` at bootstrap + Rebake, reading the hub's `ThrowConfigSO` velocity-power curve. The old per-frame `ThrowSettingsSetter.SyncVelocitySettings()` re-sync was dropped in the ConfigHub rework (2026-06-14) — see [[config-system]].

## ThrowDebugTracker Entity-Tracking Pitfall
`ThrowDebugTracker` (renamed from `ThrowSettingsSetter` in the ConfigHub rework, GUID-preserving) holds a `_trackedEntity` across throws to display live debug stats. The cleanup check **must** be `Exists && HasComponent<InAir> && IsComponentEnabled<InAir>` — `EntityManager.IsComponentEnabled<T>` throws `ArgumentException("A component with type:T has not been added to the entity")` if the entity exists but lacks T. This trips when a non-unit grabbable (only `InAirAuthoring`-less prefab) becomes the tracked entity, or generally whenever the assumed archetype is not guaranteed.

Pattern: any cross-frame tracked `Entity` that's enabled-state-checked needs `HasComponent` guards because the archetype contract isn't enforced by the field type.

## Where `InAir` Comes From (Baking)
`InAir : IComponentData, IEnableableComponent` is added by **three** authoring scripts: `InAirAuthoring` (standalone, for non-unit grabbables), `AllyAuthoring`, `EnemyAuthoring`. All three add it **disabled**. Anything thrown via `GrabbingInteractor.Release` has its `InAir` enabled there. **A grabbable that uses neither `InAirAuthoring` nor a unit-authoring will never have `InAir`** — `InAirCollisionSystem` and `ScreenBounceSystem` won't process it, and `ThrowDebugTracker` will throw without the `HasComponent` guard above.

## Gravity Sync
`ThrowDebugTracker.SyncGravity()` writes `PhysicsStep.Gravity = (0, -Gravity, 0)` every frame, reading `Gravity` from the hub's `ThrowConfigSO`. Kept as a per-frame apply (not a BlobContainer bootstrap bake) because `PhysicsStep` is authored in the battle subscene and only appears once that scene loads. See [[config-system]] / [[input-system]].

## Damage Feedback — Composable Block Pattern (Hit Feedback)
**Context:** Adding visual juice to damage events (color flash, squash, push) with each effect independently configurable per unit.
**Finding:** Pattern — separate buffer for visual-feedback signals from gameplay damage; one dispatcher fans out to per-effect "blocks"; each block is a `Config + State` component pair (`State` is `IEnableableComponent`), gated by `[Optional]` semantics.
- `HitFeedbackBufferElement` buffer (parallel to `DamageBufferElement`) carries `float3 HitDirection`. Damage producers (`AttackSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`) append to it alongside their damage write.
- `HitFeedbackDispatchSystem` drains the buffer, aggregates direction, and conditionally enables whichever `*State` components the entity has via `SystemAPI.HasComponent<TConfig>()` checks. Runs before `ApplyDamageSystem` so killing blows still get juice.
- Each block: `DamageFlashSystem`, `DamageSquashSystem`, `DamagePushSystem` — each reads `*Config` + `*State` (enableable filter), advances state, writes target component (`URPMaterialPropertyBaseColor` for flash on body entity, `PostTransformMatrix` for squash, `PhysicsVelocity` for push). Each disables its own `*State` when finished.
- Blocks are added by a single `HitFeedbackAuthoring` with three optional SO refs (one per block); a null ref → block absent. Designer composes per-unit by SO assignment.
**Why it matters:** "Effect = component pair + dedicated system" scales linearly: adding a fourth block (e.g. screen shake hint, particle burst trigger) means one new pair + one new system, zero changes to existing blocks or dispatch. Buffer-and-fanout decouples damage producers from visual consumers — producers don't grow per-effect.

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
