# Events, Commands, Services, Utilities

> **Decision:** the EventBus/CommandDispatcher split + custom-bus rationale is recorded in `Claude/docs/adr/0002-eventbus-vs-commanddispatcher.md`. This file is the how-it-works.

## EventBus — Static Notification Hub With Priorities
`BarkingBird.Runtime.Infrastructure.EventBus` (at `Runtime/Infrastructure/EventBus/EventBus.cs`) is a static, type-keyed event hub for **past-tense notifications** ("X happened"). Replaced the old `EventManager` (which exposed loose `Action`s grouped by nested static classes).

**API:**
- `EventBus.Subscribe<T>(EventHandler<T> handler, int priority = 0) → IDisposable` — higher priority runs earlier; equal-priority listeners run in subscription order. Returns a token that unsubscribes on `Dispose()`.
- `EventBus.Unsubscribe<T>(EventHandler<T> handler)` — for code that prefers `+= / -=` style over IDisposable tokens.
- `EventBus.Raise<T>(in T evt)` — dispatches to all listeners in priority order. `in` to avoid copying struct payloads.
- `EventHandler<T>` is `void(in T evt) where T : IEvent` — handlers take an `in` parameter so struct events don't copy on invocation.

**Events** are `readonly struct ... : IEvent`, marker interface in `BarkingBird.Runtime.Infrastructure`. Live in the owner's namespace. Current events:
- `Gameplay.Input/Input_EventsAndCommands.cs`: `ObjectGrabbedEvent`, `GroundGrabbedEvent { bool ActuallyHolding }`, `ReleaseEvent` (no single owner — consolidated by namespace; see file-grouping convention below).
- `Gameplay.Daylight/DayNightCycle_EventsAndCommands.cs`: `DayStartedEvent`, `DayEndedEvent` (consolidated with related commands).

**Not Reflex-injected** (intentional). Static access lets ECS systems and MonoBehaviours raise/subscribe with no DI plumbing or per-scope bridge. Consumers: `CursorSetter`, `DummyCursorSetter`, `BattleCameraMovement`, `CameraInputHandler`, `InteractController`, `DayNightCycle`, `DaylightEcsBridge`.

**Implementation invariants** (why we built our own instead of `GenericEventBus`):
- *Allocation-free Raise.* Listener lists live in per-type `static class Listeners<T>` (C# static-generic-class trick) — no dictionary lookup, no per-Raise list copy. Designed for 100+ units raising events per frame.
- *Re-entrant raise is queued via depth counter.* Calling `Raise(B)` from a handler of `A` defers `B` until `A`'s dispatch loop unwinds. No nested-dispatch stack blowups.
- *Subscribe/Unsubscribe during dispatch is queued, not applied mid-iteration.* When `_depth > 0`, ops append to a per-type `PendingOp` list and apply in the depth-0 drain. New subscribers join *after* the in-flight event. Allocation-free (no closure capture, no list-copy).
- *Cross-play-mode-reload safety.* `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` clears every `Listeners<T>` on play mode enter — works even with "Disable Domain Reload" enabled. Each `Listeners<T>` registers itself with the bus's clearer list via its static ctor.
- *Stable equal-priority ordering.* `FindInsertIndex` inserts new equal-priority listeners *after* older ones (binary search with `>=` on the left half).
- *No consume / stop-propagation feature.* Deliberately omitted — add when input layering needs it.
- *Per-handler `try/catch` with `Log.Default.E(e)`.* One bad listener doesn't kill the rest.

**Pattern:** subscribe in ctor or `Register()`, unsubscribe in `Dispose()`. Either retain the `IDisposable` from `Subscribe` or call `Unsubscribe` with the same method reference (delegate equality matches target+method, so `obj.Method` re-resolved later still removes correctly).

## CommandDispatcher — Reflex-Injected Request Hub (1-to-1)
`BarkingBird.Runtime.Infrastructure.Commands.CommandDispatcher` (at `Runtime/Infrastructure/Commands/`) is the **request side**: imperative "do X" calls. Bound as a project-scoped concrete singleton in `ProjectInstaller` and **injected via Reflex** (no static access from runtime code).

- `Send<T>(in T command)` — dispatches synchronously to the one registered handler. Exception is caught and logged via `Log.Default.E`. No handler → silent no-op (intentional: removes coupling on registration order).
- `Register<T>(Action<T> handler)` → returns `IDisposable` token; dispose to unsubscribe. **Throws `InvalidOperationException` on duplicate registration for the same command type** — the contract is strictly 1-to-1. For fan-out, raise an event via `EventBus` instead.
- Storage is `Dictionary<Type, object>` holding a single `Action<T>` per type (not a list). The type-system enforces the 1-to-1 invariant; "the rule is the code", not a convention.
- Dispose-only-if-still-mine: `Unregister` re-checks delegate equality before removing, so disposing a stale `IDisposable` won't clobber a newer registration for the same command type.

Commands are `readonly struct ... : ICommand` (marker interface). Live next to their semantic owner (e.g. `Gameplay.Scenes/Commands/ChangeSceneCommand.cs`, `Gameplay.Daylight/DayNightCycle_EventsAndCommands.cs`), not in a central commands folder.

### File-grouping convention for commands and events
Decide where a command/event file lives by counting how many sibling types share its owner:

1. **One command/event for the owner** → its own file, named after the type (e.g. `ChangeSceneCommand.cs`). Placed next to the owner class.
2. **Multiple commands/events for the same owner class** → consolidate into a single file `<OwnerClass>_EventsAndCommands.cs` next to that class.
   - Example: `DayNightCycle` raises `DayStartedEvent`/`DayEndedEvent` and handles `StartDayCommand`/`ForceFinishDayCommand` → `DaylightCycle/DayNightCycle_EventsAndCommands.cs` (namespace `BarkingBird.Runtime.Gameplay.Daylight`).
3. **No single owner class, but the types share a domain** (raised/handled by multiple unrelated classes in the same area) → consolidate into `<NamespaceLastSegment>_EventsAndCommands.cs` at the area-folder root.
   - Example: `ObjectGrabbedEvent`/`GroundGrabbedEvent`/`ReleaseEvent` are raised by both `InteractController` and `CameraInputHandler`, consumed by Cursor/Camera/etc. → `Input/Input_EventsAndCommands.cs` (namespace `BarkingBird.Runtime.Gameplay.Input`).

Don't use `Commands/` or `Events/` subfolders — the consolidated file or sibling file lives flat next to its owner class. Multiple `readonly struct`s per file is fine; these are short marker types.

**Apply this convention to any new commands and events going forward.** When a second sibling shows up next to a single-type file, that's the moment to fold both into `<OwnerClass>_EventsAndCommands.cs`.

**SO-side access (`StateOverride` and other `SerializeReference` types):** these can't be Reflex-injected, so resolve via `Reflex.Core.Container.ProjectContainer.Resolve<CommandDispatcher>()` inside `Apply()`. This is the official Reflex static; see `UnityInjector.cs` in the package. **Do not** use this escape hatch from constructor-injectable code — get the dispatcher via DI.

## Decision Guide — Direct Inject vs. CommandDispatcher vs. EventBus
Three mechanisms for "A wants B to do something / know something." Pick by **who-knows-whom** and **what's the message's lifetime**, not by reflex.

### 1. Direct DI inject (the default)
**Use when** the sender can hold the service via Reflex and just wants the thing done.
- Reads as a normal method call: `_weapon.Fire(target)`.
- Cheapest. Most debuggable (find-references works). No allocation, no indirection.
- **Cost:** sender's class compile-depends on the service's interface — that's *fine* within the same architectural layer.
- **Rule of thumb:** if removing the abstraction wouldn't violate a layering boundary or lose a queue/log/replay/network benefit, inject the class directly. A `CommandDispatcher.Send` that immediately resolves to one handler in the same layer is ceremony — delete it and inject.

### 2. CommandDispatcher.Send (imperative, 1-to-1)
**Use when** at least one of these is true; otherwise prefer direct inject:
- **Layering boundary** — sender lives in a layer that shouldn't reference the handler's layer (Input → Gameplay, UI → Domain, SO/`SerializeReference` → runtime service). The command type is shared, the implementation isn't.
- **Reify intent as data** — you want to queue, defer, log, replay, network-sync, or undo the request. A method call vanishes after it runs; a command is an object you can store.
- **Pure-C# Reflex singleton with no scene reference** — sender can't `FindObjectByType` and shouldn't take a constructor dep (e.g. one-shot input handlers, editor-time invokers).

Past examples in this project: `ChangeSceneCommand` (input layer → scene/camera layer), `StartDayCommand` / `ForceFinishDayCommand` (gameplay code & `StateOverride` SO → `DayNightCycle`). All cross a boundary or come from a non-injectable site.

### 3. EventBus.Raise (notification, 1-to-many)
**Use when** the producer is announcing a *fact* after its own logic ran, and zero-or-more unknown subscribers may care.
- Past tense names: `DayStartedEvent`, `ObjectGrabbedEvent`.
- No required receiver — unhandled is normal, not a bug.
- Subscribers from unrelated systems (UI, cursor, camera, audio) can listen without the producer knowing they exist.

### Quick contrast: Command vs. Event
| | `CommandDispatcher.Send` | `EventBus.Raise` |
|---|---|---|
| Mood | Imperative — *"do X"* | Indicative — *"X happened"* |
| Tense | `FireWeapon`, `ChangeScene` | `WeaponFired`, `DayStarted` |
| Handlers | 1 (enforced — throws on duplicate) | N (priorities, ordering) |
| Unhandled | Silent no-op (debug-log noise if needed) | Normal |
| Sender's expectation | Someone owns this command | Whoever cares can listen |

### Why the split is worth keeping
Both buses give you type-safe payloads and `IDisposable` subscription tokens (no "must hold exact delegate reference" footgun). The *names* in your codebase are the type system for humans — `Send(new Fire())` reads as a request, `Raise(new Fired())` reads as a notification, even though the wires look similar. Mixing the two scrambles that signal.

**Migration history:** `Input.SceneChangeRequest`, `Daylight.StartDayRequest`, `Daylight.ForceFinishRequest` were originally `Action`s in the old `EventManager`. They moved to `ChangeSceneCommand`, `StartDayCommand`, `ForceFinishDayCommand` when the split was formalized. Notification `Action`s (`DayStarted`, `ObjectGrabbed`, etc.) later moved to the typed `EventBus` (struct events + priorities) when `EventManager` was retired.

## Log Tags
`Log.Default`, `Log.Loading`, `Log.Battle`, `Log.World`, `Log.City`, `Log.Boot` — each a `TagLog` instance with category prefix. Methods: `D` (debug, stripped under `PROD` define via `[Conditional("DUMMY_UNUSED_DEFINE")]` trick), `W` (warning), `E` (error/exception), `ThrowException`. All marked `[HideInCallstack]` so the stack frame skips the TagLog layer.

**Pattern:** prefer `Log.Battle.D("...")` over raw `Debug.Log`. Compile-out semantics for production come for free.

## LoadingService
Wraps `ILoadUnit.Load()` and `ILoadUnit<T>.Load(param)` with stopwatch timing + main-thread switch + exception logging. Has a `CompositeDisposable Disposable` field for collecting `IDisposableLoadUnit`. Used by `BootstrapFlow` to load `CursorSetter` (cursor textures); after the load chain it calls `_blobContainer.Initialize()`. (The old `ConfigContainer` JSON load was removed with the ConfigHub rework, 2026-06-14.)

## AssetService — Required Wrapper For All Resources Loads
> **Rule** (`Resources.Load*` → `AssetService.R`) is canonical in `unity-coding-standards` P3 Resources; this section is the *why + gotcha*. Direct `UnityEngine.Resources.Load*` is allowed only inside `AssetService.cs` itself.
**Why it matters:** Centralizes the Resources entry point so future caching/profiling/Addressables migration touches one file. New `LoadAll<T>` was added when `SceneWorkflowRunner` was routed through it — extend `AssetService.Resources` if you need another Resources API.

**Naming-collision gotcha:** the wrapper class is `BarkingBird.Runtime.Infrastructure.Utilities.Resources` (intentionally shadows `UnityEngine.Resources`). Files that need both must alias one (`using Resources = UnityEngine.Resources;`) or fully qualify — but consumer code should never need `UnityEngine.Resources` at all once it uses `AssetService.R`.

## CoreHelper
- `CoreHelper.MainCamera` — cached `Camera.main`. **Camera must be tagged MainCamera.** Cache lazily; reset when nulled.
- `CoreHelper.GetWait(float)` — cached `WaitForSeconds` dictionary to avoid allocation.
- `CoreHelper.TimeNow()` — `"HH:mm:ss.fff"` formatted timestamp.

## MathHelper
- `GetHeading(objPos, targetPos)` — atan2-based heading in radians.
- `GetForwardFromHeading(float heading)` — `(sin, 0, cos)` unit vector.

## PhysicsUtility
Single static class collecting collider-geometry reads and PhysicsWorld overlap queries (previously split across `EntityPhysicsHelper` / `PhysicsOverlapHelper` / `PhysicsUtility` — merged because the dividing lines were too thin for 4 methods).
- `GetRandomPointInsideCollider(em, entity, ref Random)` *(public)* — samples a random point inside the AABB of a `PhysicsCollider` (transformed to world). Used by `SpawningSystem` area spawner.
- `GetEntityHalfExtentsXY(entity, em)` *(internal)* — returns `float2(hw, hh)` from one AABB read with rotation but zero translation; fallback `(0.5, 0.5)` if no `PhysicsCollider`. Used by grab-and-throw consumers (`GrabbedEntityMover`, `OverlapResolver`, `ThrowTrajectoryPredictor`).
- `CollectHitBodies(in PhysicsWorldSingleton, Aabb, CollisionFilter, Entity selfEntity)` *(internal)* — runs `OverlapAabb`, skips self and uncreated colliders, returns a `NativeList<RigidBody>` (caller disposes). Used by `OverlapResolver` and `OverlapEjector`.
- `AabbsOverlapXY(Aabb, Aabb)` *(internal)* — XY-only overlap test; ignores Z because the game plane is XY.

## Config Pipeline
**Balance/AI tuning moved off JSON to the `ConfigHub` system (2026-06-14).** Full reference: [[config-system]]. In short: `ConfigHub` (Bootstrap-scene MonoBehaviour, was `PrototypeConfigSetter`) holds per-unit-type profile SOs + flat config groups; `BlobContainer.Initialize()` bakes them into `Global_Target_Profiles` (blob) + `Config_*` singletons at bootstrap.

**JSON config is fully dead (Phase 3, 2026-06-14).** `ConfigContainer`, `Config.json`, `ConfigGenerator` (BarkingBird → Generate Configs), `BlobConfigConverter`/`[BlobConfig]`, and the `RuntimeConstants.Configs` paths are all **deleted**. The last JSON holdouts — camera and power-hit — moved to plain SOs (`CameraConfigSO`, `PowerHitConfigSO`) referenced from the hub and bound as their own types in `BootstrapInstaller`. Nothing reads JSON for config anymore. Full reference: [[config-system]].

## RuntimeConstants
At `Runtime/Infrastructure/Settings/RuntimeConstants.cs`, namespace `BarkingBird.Runtime.Infrastructure.Settings`. *(The rule — all Resources/Assets path strings live here, no string literals at call sites — is canonical in `unity-coding-standards` P3 Resources.)*
- `Scenes.Bootstrap/Loading/World/Battle/City` — int build indices, resolved at static init via `SceneUtility.GetBuildIndexByScenePath`.
- `PhysicLayers.Unit/Grabbable/Ground/Obstacle/PickUps` — string names; resolved via `LayerMask.NameToLayer` at use sites.
- `SceneWorkflow.RunConfigurationsPath = "Settings/SceneRunConfigurations"` — Resources-relative path for run-config assets.
- `Daylight.MinutesInDay = 1440`.
- *(The `Configs` nested class — `ConfigFileName`/`AssetsResourcesFolder` — was removed with the JSON config in the ConfigHub rework, 2026-06-14.)*
- `Cursors.Open/ObjectHold/GroundHold/Dummy` and `Cursors.All` — texture filenames (just the basename).
- `Cursors.RootPath = "Cursors/"`, `Cursors.SpritesPath`, `Cursors.TexturesPath`, `Cursors.DummyPrefabPath` — composed from `RootPath`. Pattern: define the folder once, build subpaths via `const` concatenation, so a folder rename is a one-line change.
- `SceneWorkflow.RunConfigurationsPath = "Settings/SceneRunConfigurations"` — Resources-relative; consumed by `SceneWorkflowRunner` in build mode via `AssetService.R.LoadAll<RunConfiguration>`.

**Naming-collision gotcha:** `RuntimeConstants.SceneWorkflow` (nested class) shadows the `BarkingBird.Runtime.Infrastructure.SceneWorkflow` namespace inside that namespace's own files. Always go through `RuntimeConstants.SceneWorkflow.X` so the compiler routes via the type, not the namespace.
