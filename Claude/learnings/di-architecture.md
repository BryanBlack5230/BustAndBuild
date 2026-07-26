# Reflex DI Architecture

## Container Hierarchy & Installer Scopes
Reflex uses scene-scoped containers stitched into a parent chain via `SceneScope.OnSceneContainerBuilding`. The chain is **`Project → Bootstrap → World → Battle`**: each scene's flow re-parents the *next* scene's container onto its own (`BootstrapFlow` parents World onto Bootstrap; `WorldFlow` parents Battle onto World). Both chains that include the battle scene (`BattleChain`, `NormalChain`) load **World before Battle**, so a Battle-scoped service can resolve World-scoped ones (`Wallet`, `BeaconCoreState`) up the chain — but **not the reverse** (a World-scoped service cannot inject a Battle-scoped one). *(Earlier note said "Bootstrap is parent for both World and Battle" — that was stale; Battle nests under World.)*

- **ProjectInstaller** (Project scope, lives across all scenes): `InputManager`, `LoadingService`, `CursorSetter`, `CommandDispatcher`. (`ConfigContainer` removed 2026-06-14 — JSON config retired.)
- **BootstrapInstaller** (Bootstrap scene): `GameLoopManager`, `GameManagerUIController`, `BootstrapFlow`, `ConfigHub` (was `PrototypeConfigSetter`), the hub's plain SO configs bound as their own types via `InstallHubConfigs` (`CameraConfigSO`, `PowerHitConfigSO`, `ThrowConfigSO`, `DaylightConfigSO` — each null-guarded), `BlobContainer` (`IDisposable` — disposes its persistent blob on teardown), `ThrowDebugTracker` (was `ThrowSettingsSetter`; `IGameListener`), `DotsGameLoopBridge`, `GameManager` (NonLazy), `ActiveSlot`, `DummySaveSystem` (as `ISaveSystem`). **These SO configs are bootstrap-scoped, so down-scene consumers (Camera/PowerHit in Battle, Daylight in World) resolve them via the SetParent chain.**
- **WorldSceneInstaller**: `WorldFlow`, `DaylightHandler`, `DayNightCycle` (now takes `DaylightConfigSO` from the bootstrap-scoped hub, not a scene `DayNightSetting`), `DaylightEcsBridge` (NonLazy + IDisposable), `WorldSceneData`, `ScrollController`, `WorldCameraHandler`, `Wallet` (bare singleton), `BeaconCoreState` (bare singleton — World-scoped holder for the Beacon Core free-countdown, shared with the Battle-scoped `BeaconCoreController`; registered before `WorldSaveService`), `WorldSaveService` (NonLazy + IDisposable — hydrates the wallet + `BeaconCoreState`, flushes on day end / scene unload). `Wallet` + save seam covered in [[currency-and-saves]]; `ActiveSlot` + `ISaveSystem` (`DummySaveSystem`) are Bootstrap-scoped and inherited here.
- **BattleGroundSceneInstaller**: `BattleSceneData`, `BattleGroundSceneFlow`, all battle-side input/camera (`MousePositionProvider`, `CursorMovementCalculations`, `GrabbedEntityMover`, `OverlapResolver`, `OverlapEjector`, `ReleaseCoordinator`, `TrajectoryPredictorSettings`, `ThrowTrajectoryPredictor`, `GrabbingInteractor`, `InteractController`, `PowerHitController`, `BattleCameraMovement`, `BattleCameraBorderSyncBridge`, `CursorEcsBridge`), plus the Beacon Core trio (`InstallBeacon`, all `IGameListener` + `IDisposable`): `PlacementController` (registered first — `BeaconCoreController` injects it), `BeaconCoreController` (reads `Wallet`/`BeaconCoreState` from World scope), `BeaconEcsBridge`. `BeaconCoreConfigSO` is bootstrap-scoped (a hub config, via `InstallHubConfigs`).

## Parent Hookup Trick (Cross-Scene Containers)
`BootstrapFlow.Construct()` subscribes to `SceneScope.OnSceneContainerBuilding` and inside the callback calls `builder.SetParent(_bootSceneContainer)`. `WorldFlow` does the same. Pattern: each scene flow can stitch its container under the boot container so deps registered in Bootstrap (e.g. `GameLoopManager`) resolve down-scene.
**Why it matters:** New scenes added must do the same `SetParent` dance if they need bootstrap-scoped services.

## Registration Order Matters
`AddSingleton` registration order determines constructor injection resolution order. The classic gotcha — `BattleGroundSceneInstaller` registers `ThrowTrajectoryPredictor` **before** `GrabbingInteractor` because the latter consumes the former by ctor injection. Silent failure if reversed. See [[input-system]].

## Factory Bindings & Container Disposal
- `builder.AddSingleton<T>(Func<Container, T> factory, params Type[] contracts)` resolves at first demand and hands you the built container — use it when a binding must pull other (incl. parent-scope) services to construct itself. (We considered it for hydrating `Wallet` but moved hydration into `WorldSaveService` instead — see [[currency-and-saves]].)
- **Scene containers dispose on scene unload.** Reflex's `UnityInjector` subscribes to `SceneManager.sceneUnloaded` and calls `container.Dispose()`, which disposes every `IDisposable`-contracted binding. So a World-scope service bound `typeof(IDisposable)` gets `Dispose()` called when the World scene unloads — a reliable flush/teardown hook. The **project** container disposes on `Application.quitting` (not scene containers), so app-quit teardown of scene-scoped services rides on scene unload, which is not guaranteed on quit — add an explicit `OnApplicationQuit` hook if quit-time persistence matters.

## ReflexExtensions Helpers
```csharp
builder.AddInterfacesAndSelf(instance);  // registers under concrete type + all implemented interfaces
builder.NonLazy<T>();                    // forces resolution on container built
```
`NonLazy` is used for `GameManager` — without it `GameManager` would never be instantiated (no one injects it explicitly).

## NonLazy Required for Self-Sufficient Singletons
**Rule:** Any singleton that is **not injected into anything else** (no other class takes it as a constructor parameter) must be registered with `.NonLazy<T>()`, otherwise Reflex never constructs it and it stays dormant.  
**Example:** `DaylightEcsBridge` subscribes to `EventManager` events in its constructor and is never injected anywhere, so without `NonLazy` it would never be created and spawning would never toggle.
```csharp
builder.AddSingleton(typeof(DaylightEcsBridge), typeof(DaylightEcsBridge), typeof(IDisposable))
       .NonLazy<DaylightEcsBridge>();
```
**How to spot the pattern:** If a class's only job is to subscribe to events or hook into external systems at construction time, it needs `NonLazy`. Classes that are injected will be constructed on first demand; classes that construct themselves for side-effects will not.

## Listener Auto-Collection
Any singleton tagged with `typeof(IGameListener)` in its registration list is picked up by `GameLoopManager.Construct(IEnumerable<IGameListener>)`. Pattern is `builder.AddSingleton(typeof(X), typeof(X), typeof(IGameListener), typeof(IDisposable))`.
**Why it matters:** To make a new service tick or pause/resume, register it as `IGameListener` in the appropriate installer — no manual wiring needed.

## Scene-Local Listener Injection
`WorldFlow` / `BattleGroundSceneFlow` inject `IEnumerable<IGameListener>` themselves and call `_gameLoopManager.AddListeners(_listeners)` in `Start()`. They also call `RemoveListeners` in `OnDestroy()`. **Why:** Scene-scoped listeners live shorter than `GameLoopManager`. The `RemoveListeners` path also calls `Dispose()` on `IDisposable` listeners — see `GameLoopManager.RemoveListeners`.

## Initialize() Convention
A few **world-free** services still defer setup to a no-arg `Initialize()` their flow calls in `Start()` — e.g. `_scrollController.Initialize()` in `WorldFlow` (input-action registration). Reflex ctors run before scene objects are ready, so deferring is the idiom. Anything that needs the **ECS world** uses `IWorldInitializable` instead (below): as of ADR-0008 **no runtime service grabs `World.DefaultGameObjectInjectionWorld` itself** — each scene flow is the single tap for its scope and hands out the `EntityManager`.

## IWorldInitializable — the project-wide Mono↔ECS world-access seam (ADR-0008)
`IWorldInitializable { void Initialize(EntityManager em); }` (`Infrastructure/GameLoop`, beside `IGameListener`). **The World stays OUT of the Reflex container** — there is exactly one default ECS world (no `ICustomBootstrap`; subscenes stream as *sections* into it), so DI scope ≠ ECS world; binding a `World` into Reflex would conflate them and capture the ref before content streams. The rule is uniform across all three scopes; the *delivery mechanism* differs by scope because of a Reflex quirk (below).

Per-class mechanics — copy for any new world-touching service:
- Implement `IWorldInitializable`; move `_entityManager =` + every `CreateEntityQuery` + any subscription whose handler touches `em` out of the ctor into `Initialize(em)`. Ctor keeps only world-free work (lists, `LayerMask` filters, the socket register, `em`-free subscriptions, LineRenderer/Material instantiation). Make `_entityManager`/queries non-`readonly`; make disposable tokens nullable so `Dispose` null-guards a never-initialized instance. A service that needs the **`World`** (not just `em`) recovers it with `em.World` (`DotsGameLoopBridge` → system group; `DaylightEcsBridge` → `GetOrCreateSystemManaged`).
- Lazy self-init (`if (!_initialized) Initialize()` inside `OnUpdate` + a per-frame `World.Default` re-grab) is **replaced** by the one-shot flow-driven `Initialize(em)` + an `if (!_initialized) return;` guard (`CursorEcsBridge`, `ThrowDebugTracker`).

### Reflex `All<T>()` is cumulative up the parent chain — so the *contract* is Battle-only
`ContainerBuilder.Build()` copies every parent resolver into the child's dictionary, so resolving `IEnumerable<T>` in a child scope returns **the current scope's bindings *plus* all ancestors'**. Hence the `typeof(IWorldInitializable)` **contract + enumerable loop is used only at the deepest scope (Battle)**: registering it in World/Bootstrap would make those services *also* surface in `BattleGroundSceneFlow`'s `IEnumerable<IWorldInitializable>` and get `Initialize`d twice (re-subscribing `DaylightEcsBridge`, leaking `ThrowDebugTracker`'s query…). (Same mechanic means the scene flows' injected `IEnumerable<IGameListener>` also inherits Bootstrap listeners — a standing property of this DI.) So, per scope:
- **Battle → contract + loop.** All Battle world-touchers register `typeof(IWorldInitializable)` (**one `AddSingleton` per class listing every contract** — one instance, many contracts; never split). `BattleGroundSceneFlow` injects its **own** `IEnumerable<IWorldInitializable>` (NOT `OfType` over `_listeners`) and loops `Initialize(em)` in `Start()` **before** `AddListeners`; future Battle bridges self-enroll with zero flow edits. Participants: Beacon trio (`PlacementController`, `BeaconCoreController`, `BeaconEcsBridge`) + `BeaconCoreHintView` (instance-registered via `[SerializeField]`, null-guarded — the `BattleSceneData` pattern) + grab/throw + input/camera (`GrabbedEntityMover`, `OverlapResolver`, `OverlapEjector`, `ReleaseCoordinator`, `ThrowTrajectoryPredictor`, `GrabbingInteractor`, `InteractController`, `PowerHitController`, `BattleCameraMovement`, `BattleCameraBorderSyncBridge`, `CursorEcsBridge`).
- **World → explicit call, no contract.** `WorldFlow` injects `DaylightEcsBridge` by concrete type and calls `Initialize(em)` in `Start()`. (`NonLazy` kept — harmless; it still self-subscribes in its ctor.)
- **Bootstrap → explicit, *ordered* calls, no contract.** `BootstrapFlow` grabs `em` once and calls in sequence: `DotsGameLoopBridge.Initialize(em)` + `ThrowDebugTracker.Initialize(em)` **before** `await BeginLoading`, then `BlobContainer.Initialize(em)` **after** (a single loop can't express this ordering — why Bootstrap stays explicit). `BlobContainer.Initialize(em)` caches `em` + bakes; its `Rebake()` (ConfigHub button) re-bakes reusing the cached `em` — no re-tap.

**Edge cases / exemptions:**
- `BattleCameraMovement` implements the interface but ignores `em` (touches no ECS) — it rides the hook only to defer its EventBus wiring out of the ctor; the interface doubles as the flow's post-construction init seam.
- **Teardown liveness probes are exempt.** `BeaconCoreHintView.OnDestroy` still reads `World.DefaultGameObjectInjectionWorld` to check the world is alive before disposing its query — an existence probe at teardown (Unity nulls the static on world disposal), not an access-for-work tap.

Failure mode: a Battle participant missing the contract, or any flow's `Initialize` call skipped → first-frame `NRE` from a `default` `EntityQuery`. See [[testing-methodology]] for the test win (no more default-world swap).
