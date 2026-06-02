# Events, Commands, Services, Utilities

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

**Not Reflex-injected** (intentional). Static access lets ECS systems and MonoBehaviours raise/subscribe with no DI plumbing or per-scope bridge. Consumers: `CursorSetter`, `DummyCursorSetter`, `BattleCameraMovement`, `CameraInputHandler`, `InteractController`, `DayNightCycle`, `DaylightSpawningBridge`.

**Implementation invariants** (why we built our own instead of `GenericEventBus`):
- *Allocation-free Raise.* Listener lists live in per-type `static class Listeners<T>` (C# static-generic-class trick) — no dictionary lookup, no per-Raise list copy. Designed for 100+ units raising events per frame.
- *Re-entrant raise is queued via depth counter.* Calling `Raise(B)` from a handler of `A` defers `B` until `A`'s dispatch loop unwinds. No nested-dispatch stack blowups.
- *Subscribe/Unsubscribe during dispatch is queued, not applied mid-iteration.* When `_depth > 0`, ops append to a per-type `PendingOp` list and apply in the depth-0 drain. New subscribers join *after* the in-flight event. Allocation-free (no closure capture, no list-copy).
- *Cross-play-mode-reload safety.* `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` clears every `Listeners<T>` on play mode enter — works even with "Disable Domain Reload" enabled. Each `Listeners<T>` registers itself with the bus's clearer list via its static ctor.
- *Stable equal-priority ordering.* `FindInsertIndex` inserts new equal-priority listeners *after* older ones (binary search with `>=` on the left half).
- *No consume / stop-propagation feature.* Deliberately omitted — add when input layering needs it.
- *Per-handler `try/catch` with `Log.Default.E(e)`.* One bad listener doesn't kill the rest.

**Pattern:** subscribe in ctor or `Register()`, unsubscribe in `Dispose()`. Either retain the `IDisposable` from `Subscribe` or call `Unsubscribe` with the same method reference (delegate equality matches target+method, so `obj.Method` re-resolved later still removes correctly).

## CommandDispatcher — Reflex-Injected Request Hub
`BarkingBird.Runtime.Infrastructure.Commands.CommandDispatcher` (at `Runtime/Infrastructure/Commands/`) is the **request side**: imperative "do X" calls. Bound as a project-scoped concrete singleton in `ProjectInstaller` and **injected via Reflex** (no static access from runtime code).

- `Send<T>(in T command)` — dispatches synchronously, exceptions per-handler logged via `Log.Default.E`.
- `Register<T>(Action<T> handler)` → returns `IDisposable` token; dispose to unsubscribe (no need to keep the delegate reference).

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

## Notifications vs. Commands — When to Use Which
The split is intentional: each tool fits one half of the request/notify pair.
- **"X happened" → `EventBus.Raise(new XEvent(...))`.** Broadcasting facts after the producer's own logic runs. Past tense. Multiple subscribers may care. Example: `DayStartedEvent` fires after `DayNightCycle` has set up its loop.
- **"Do X" → `CommandDispatcher.Send(new XCommand(...))`.** External code asking a service to act. Imperative. Typically one handler. Example: `ChangeSceneCommand` from scroll input → handled by `WorldCameraHandler`.

**Why it matters:** lets external code drive a service without holding a reference — important for pure-C# Reflex singletons that aren't `FindObjectByType`-able — while keeping the service's public surface narrow. Both halves give you type-safe payloads and `IDisposable` subscription tokens that avoid the "must hold exact delegate reference to unsubscribe" footgun.

**Migration history:** `Input.SceneChangeRequest`, `Daylight.StartDayRequest`, `Daylight.ForceFinishRequest` were originally `Action`s in the old `EventManager`. They moved to `ChangeSceneCommand`, `StartDayCommand`, `ForceFinishDayCommand` when the split was formalized. Notification `Action`s (`DayStarted`, `ObjectGrabbed`, etc.) later moved to the typed `EventBus` (struct events + priorities) when `EventManager` was retired.

## Log Tags
`Log.Default`, `Log.Loading`, `Log.Battle`, `Log.World`, `Log.City`, `Log.Boot` — each a `TagLog` instance with category prefix. Methods: `D` (debug, stripped under `PROD` define via `[Conditional("DUMMY_UNUSED_DEFINE")]` trick), `W` (warning), `E` (error/exception), `ThrowException`. All marked `[HideInCallstack]` so the stack frame skips the TagLog layer.

**Pattern:** prefer `Log.Battle.D("...")` over raw `Debug.Log`. Compile-out semantics for production come for free.

## LoadingService
Wraps `ILoadUnit.Load()` and `ILoadUnit<T>.Load(param)` with stopwatch timing + main-thread switch + exception logging. Has a `CompositeDisposable Disposable` field for collecting `IDisposableLoadUnit`. Used by `BootstrapFlow` to load `ConfigContainer` (JSON from `Runtime/Gameplay/Resources/Settings/Config.json`) and `CursorSetter` (cursor textures).

## AssetService — Required Wrapper For All Resources Loads
**Convention:** every `Resources.Load*` call in the project goes through `AssetService.R.Load<T>(path)` / `AssetService.R.LoadAll<T>(path)`. Direct `UnityEngine.Resources.Load*` calls are only allowed inside `AssetService.cs` itself.
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
- `GetRandomPointInsideCollider(em, entity, ref Random)` — samples a random point inside an AABB of a `PhysicsCollider` (transformed to world). Used by `SpawningSystem` area spawner.

## Config Pipeline
1. `BarkingBird/Generate Configs` (editor menu) — `ConfigGenerator.Generate()` writes a hardcoded `ConfigContainer` to `Assets/!_Game/Runtime/Gameplay/Resources/Settings/Config.json` via Newtonsoft.
2. At runtime, `ConfigContainer : ILoadUnit` is registered as singleton; `BootstrapFlow` calls `LoadingService.BeginLoading(_configContainer)` → `Resources.Load<TextAsset>("Config")` → `JsonConvert.PopulateObject`.
3. `BlobContainer.Initialize()` (called after configs loaded) converts `TargetProfile` lists into a `BlobAssetReference<TargetProfilesBlob>` stored on a singleton entity named `Global_Target_Profiles`.
4. Currently `BlobContainer` reads from `PrototypeConfigSetter` (MonoBehaviour) not `ConfigContainer.Battle.EnemyProfiles` — that path is commented out. **Don't be surprised** if config-JSON edits don't change AI behaviour; edit the Bootstrap scene's `PrototypeConfigSetter` instead.

## BlobConfigConverter (Reflection)
`BlobConfigConverter.CreateBlob<T>(source)` walks public instance fields by name match and copies values into a blob root struct via `__makeref` / `SetValueDirect`. Skips fields whose types differ. Used as generic converter for `[BlobConfig]`-annotated config classes. Currently not invoked anywhere — `BlobContainer` builds its profile blobs manually. Keep in mind if you see `[BlobConfig]` on a class but no allocation site.

## RuntimeConstants
At `Runtime/Infrastructure/Settings/RuntimeConstants.cs`, namespace `BarkingBird.Runtime.Infrastructure.Settings`. **All Resources paths and Assets-relative paths must live here** — no string literals at call sites.
- `Scenes.Bootstrap/Loading/World/Battle/City` — int build indices, resolved at static init via `SceneUtility.GetBuildIndexByScenePath`.
- `PhysicLayers.Unit/Grabbable/Ground/Obstacle` — string names; resolved via `LayerMask.NameToLayer` at use sites.
- `Configs.ConfigFileName = "Settings/Config"` — Resources-relative path consumed by `ConfigContainer.Load()`.
- `Configs.AssetsResourcesFolder = "!_Game/Runtime/Gameplay/Resources"` — Assets-relative path for editor-side writes (`ConfigGenerator` uses it with `Application.dataPath`). Separate constant because `Path.Combine(Application.dataPath, …)` is not a Resources.Load.
- `Cursors.Open/ObjectHold/GroundHold/Dummy` and `Cursors.All` — texture filenames (just the basename).
- `Cursors.RootPath = "Cursors/"`, `Cursors.SpritesPath`, `Cursors.TexturesPath`, `Cursors.DummyPrefabPath` — composed from `RootPath`. Pattern: define the folder once, build subpaths via `const` concatenation, so a folder rename is a one-line change.
- `SceneWorkflow.RunConfigurationsPath = "Settings/SceneRunConfigurations"` — Resources-relative; consumed by `SceneWorkflowRunner` in build mode via `AssetService.R.LoadAll<RunConfiguration>`.

**Naming-collision gotcha:** `RuntimeConstants.SceneWorkflow` (nested class) shadows the `BarkingBird.Runtime.Infrastructure.SceneWorkflow` namespace inside that namespace's own files. Always go through `RuntimeConstants.SceneWorkflow.X` so the compiler routes via the type, not the namespace.
