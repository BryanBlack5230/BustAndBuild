# ECS / DOTS Patterns & Gotchas

Reusable DOTS patterns: queries, aspects, ECB, baking, ECS↔managed bridging. Project map lives in [[ecs-architecture]]; combat-specific systems in [[ecs-combat-and-collisions]]; Unity Physics engine gotchas in [[unity-physics-gotchas]].

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
**Context:** Tried to call `EntityManager.SetComponentEnabled` from a managed bridge on the day-started event.  
**Finding:** Subscenes bake asynchronously. Managed events (e.g. `DayStartedEvent`) can fire *before* any subscene entities exist. A one-shot event handler that calls `SetComponentEnabled` on a freshly built query will silently succeed against an empty result set.  
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

## Aspects (IAspect) — Patterns and Gotchas
First aspect added: `HealthAspect` (Components/HealthAspect.cs). Use sparingly — only when the same multi-component access repeats in 3+ systems, OR there's an invariant between components worth enforcing in one place. Otherwise it's overhead with no payoff.

**Not OOP.** Aspects Burst-inline to the same code as `RefRW<T>` field plumbing. No data, no polymorphism. Components stay pure `IComponentData`.

**Optional fields:** `[Optional] private readonly EnabledRefRW<T> _field;` — entity matches even when T is absent. Check `_field.IsValid` before reading `.ValueRO` / writing `.ValueRW`. Used in `HealthAspect` for `IsInvulnerable` (only Ally bakes it).

**`EnabledRefRW<T>` field auto-registers T in the query with *default enabled-state matching*** — meaning the query only matches entities where T is **enabled**. For a "you-flip-it" enableable like `IsDead`, that's wrong: living entities have it disabled, so they'd be skipped. Symptom in this project: damage queued in buffer but never drained for living units. Fix: `[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]` on the IJobEntity + an early-return guard inside the aspect method (`if (_isDead.ValueRO) return;`).

**Attribute conflict:** Cannot combine an aspect with `EnabledRefRW<T>` and `[WithDisabled(typeof(T))]` / `[WithAll(typeof(T))]` on the same job — Unity throws `EntityQueryDescValidationException: duplicate component type name T`. The aspect already registered T; the attribute tries to re-register. Move the conditional into the aspect method instead.

**IJobEntity Execute signature:** Aspects work directly as Execute parameters: `void Execute(HealthAspect hp) => hp.DoThing();`. Burst-compiled, parallel-safe.

**Aspect writes + main-thread reads of the same component — dependency sync:** `ComponentLookup<T>.Update(ref state)` refreshes caches but does **not** sync against pending writer jobs. If main-thread code reads via lookup while another job writes T (e.g. via aspect's `EnabledRefRW<T>`), you get `InvalidOperationException: writes to the ComponentLookup<T>... must call JobHandle.Complete`. `SystemAPI.Query<EnabledRefRO<T>>` **does** sync via state.Dependency. Fix pattern: pull T into the query as an `EnabledRefRO<T>` field instead of using a lookup. Heavy-handed alternative: `state.CompleteDependency()` before the foreach, but it stalls workers. See `AbleToActEvaluationSystem` for the query-based fix.

## Pattern — Snapshot Job Instead of `NativeDisableContainerSafetyRestriction`
**Context (2026-06-10):** `TargetScorerJob` previously used `[NativeDisableContainerSafetyRestriction]` on a `ComponentLookup<Target>` so each unit could read *other* units' targets (AggroBonus) while the same parallel job wrote `ref Target` — a genuine data race (torn/stale reads). See [[steering-and-ai]] for the scoring details.
**Fix:** `TargetSearchSystem` schedules a small Bursted `IJob` (`SnapshotTargetsJob`) before the scorer that copies `unit → Target.TargetEntity` for all coordinator-registered units into a `NativeParallelHashMap<Entity, Entity>` (`Allocator.TempJob`, disposed via `Dispose(state.Dependency)`). The scorer reads aggro from the snapshot (`TryGetValue`), no safety attribute needed. Both jobs chain through `state.Dependency`, so the scheduler serializes snapshot-read vs scorer-write automatically.
**Why it matters:** the general cure for "parallel job needs to read component X of OTHER entities while writing X" — snapshot first, never disable safety. Aggro values are consistently "as of start of this frame's search" instead of racy.

## ECB Playback Failure: `AssertEntityHasComponent` Almost Always Means a Stale Prefab
**Context:** `PickupSpawnOnDeathSystem` ECB threw `ArgumentException: AssertEntityHasComponent` at playback, with cascade noise from `DeathSystem` after.
**Finding:** When a job does `ECB.Instantiate(prefab)` then `ECB.SetComponent(newEntity, T)`, the assertion fails if the prefab wasn't authored with `T`. Symptom is at ECB **playback** (in `EndSimulationEntityCommandBufferSystem`), not at recording, and the message names the **recording** system. Common cause: an `Authoring` MonoBehaviour was assigned a prefab that's missing its sibling component authoring (e.g. PickupSpawnerAuthoring referencing a plain prefab that doesn't have `PickupAuthoring`).
**Why it matters:** Catch this at bake time rather than runtime — let Bakers reject misconfigured prefabs with a clear error:
```csharp
if (authoring.prefab.GetComponent<RequiredAuthoring>() == null)
{
    Debug.LogError($"[{nameof(MyAuthoring)}] prefab '{authoring.prefab.name}' missing RequiredAuthoring.", authoring);
    // bake with Entity.Null so runtime systems skip gracefully
}
```
The `authoring` second arg makes the log clickable in the editor console — links straight to the GameObject. `PickupSpawnerAuthoring.Baker` uses this pattern (one error per missing-prefab mapping, then bakes that slot as `Entity.Null` so the spawn loop skips it).

## ECB Errors Cascade — Trust the First, Discard the Rest
When one ECB op throws during playback, subsequent ECB playbacks in the same `EndSimulationEntityCommandBufferSystem.FlushPendingBuffers()` flush often fire follow-on assertions like `AssertNoQueuedManagedDeferredCommands` ("Expected: True; Value was False"). These name *other* systems (e.g. `DeathSystem`) but they're noise — the root cause is the first thrown exception. Read the trace top-down; debug the first ECB error.

## Baked `Entity` Refs Inside a FixedList/Blob Aren't Remapped → Invalid-Entity at Instantiate
**Context:** Generalized the single-pearl spawner to a per-resource prefab map. Stored the prefab `Entity`s in a `FixedList512Bytes<…>` field on an `IComponentData` singleton. At runtime `ECB.Instantiate(prefab)` threw `ArgumentException: An EntityManager command is operating on an invalid entity … was never created` from `InstantiateInternalDuringStructuralChange`, surfacing at `EndSimulationEntityCommandBufferSystem` playback.
**Finding:** Unity's bake-time→runtime entity-id remap only walks **directly-typed `Entity` fields** of a component (and `DynamicBuffer` elements). It does **not** traverse `Entity`s buried in a `FixedList`'s internal byte storage (same for blittable blob data) — they keep authoring-space ids and are invalid at runtime. The old `PearlSpawnPrefab { Entity Prefab; }` worked precisely because it was a bare, directly-typed field.
**Fix:** Bake refs into a `DynamicBuffer<T>` where `T : IBufferElementData { Entity Prefab; … }` (buffer elements ARE remapped), indexed by enum value. At runtime copy the now-correct buffer into a `FixedList` for Burst job capture — the remap problem is bake-time only, so the runtime copy is safe:
```csharp
var buf = SystemAPI.GetSingletonBuffer<PickupPrefabRef>(true);
var map = default(PickupPrefabMap);            // plain struct, NOT a component
for (var i = 0; i < buf.Length; i++) map.Entries.Add(buf[i]);
```
`PickupSpawnerAuthoring.Baker` (`AddBuffer`) + `PickupSpawnOnDeathSystem`/`EnemyEscapeSystem` (copy-then-capture). **Distinct** from the stale-prefab `AssertEntityHasComponent` note above — that's a missing component on a *valid* prefab; this is a valid prefab whose id was never patched.
**Why it matters:** Any time you need an array of baked prefab/entity references, use a buffer (or store integer indices and resolve at runtime) — never a FixedList/blob of `Entity`.

## Pattern — Burst-Friendly Static ECB Spawn Helper (PickupSpawnUtility)
A plain `static class` method **is callable from inside Burst-compiled `IJobEntity.Execute`** as long as it touches only unmanaged types. `PickupSpawnUtility.Spawn(ref EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity prefab, float prefabScale, in PickupSettings settings, CurrencyType type, float3 source, int count, float value, ref Random rand)` holds the single instantiate-and-configure block shared by `SpawnPickupsOnDeathJob` and `EnemyEscapeJob`. Notes: pass the job's ECB field by `ref` (it's a struct), pass `Random` by `ref` so state advances in the caller, take config structs by `in`. Enums like `CurrencyType` are blittable and pass straight into Burst. Prefer carrying one settings struct field on the job (`public PickupSettings Settings;`) over copying individual floats. Lives in root `Systems/` (global namespace) because both global-namespace and `Gameplay.AI` jobs call it. (Earlier notes claimed Burst sometimes refuses to inline cross-job static helpers and the loop had to be duplicated — that turned out unnecessary here; the shared helper compiles and runs fine in both jobs.)

## Pattern — RequireForUpdate vs HasSingleton Fallback
`RequireForUpdate<T>` is "this system can't run without T". Use when the system is meaningless absent the singleton (e.g. `PickupSpawnOnDeathSystem` requires `PickupSettings`). Co-location trick: the `PickupPrefabRef` buffer is baked onto the **same** spawner entity as `PickupSettings`, so gating on `PickupSettings` implicitly guarantees the buffer too — no separate require needed.
**Avoid** `RequireForUpdate` when the system has independent responsibilities and the singleton is *optional*. Example: `EnemyEscapeSystem` runs unconditionally to destroy escaped enemies; it only optionally drops loot if a spawner exists. Hard-requiring the spawner there would silently break escape in spawner-less scenes. Use `HasSingleton<PickupSettings>()` + `default` fallbacks (an empty `PickupPrefabMap`, whose zero-length `Entries` makes the drop loop a no-op).

## Pattern — Capture Boundary-Crossing Position via Per-Frame Field Write
When a system needs to remember "the position where the entity crossed back into region X", don't track a `bool TransitionedThisFrame` + position pair. Instead, write the current position into the struct field **every frame while outside**:
```csharp
if (!insideBase) { data.LastOutsidePosition = currentPos; data.DwellTimer = 0; return; }
// inside: data.LastOutsidePosition holds the last write — i.e. position just before re-entry
```
On the frame the entity transitions to inside, the field already holds the boundary-crossing position; no extra state machine needed. Used in `HasLeftBase.LastOutsidePosition` to anchor scared-escape loot drops at the base boundary instead of deep inside the inaccessible enemy base. See [[steering-and-ai]].

## Pattern — Defer Counter Increment to Animation Completion
When an ECS event triggers a UI counter bump *and* a fly-to-counter animation, raise the event from ECS but let the **animation's `OnComplete` callback** drive the counter increment, not the event. The counter ticks up exactly when the visual lands, instead of bumping early while pickups are still mid-flight. Implementation: `PickupCollectSystem` raises `PickupCollectedEvent` (carrying `CurrencyType`); `PickupMagnetController` routes by type to the matching `ResourceCounterView` and the tween `OnComplete` calls `Wallet.Add(type, amount)`. Nothing subscribes to `PickupCollectedEvent` for the total — `ResourceCounterView` (filtered by `CurrencyType`) reacts to the `CurrencyChangedEvent` that `Wallet.Add` raises, which only fires when the tween lands. Resources with no registered counter are credited immediately in the same handler so the pickup isn't lost.

## ECS-to-Managed: EventBus from a SystemBase
A managed `SystemBase` in `GameLoopSystemGroup` can call `EventBus.Raise<T>(in evt)` directly from `OnUpdate` — `EventBus` is static, no DI needed, and managed subscribers (MonoBehaviours, POCOs) listen normally. This sidesteps having to bridge through a dedicated "ECS event reader" managed system when the only need is to notify managed code. `PickupCollectSystem` uses this. Don't try this from an `ISystem` (struct, Burst) — `EventBus.Raise` accesses managed types and won't compile in Burst.

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
