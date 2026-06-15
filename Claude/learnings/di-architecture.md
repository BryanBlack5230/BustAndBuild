# Reflex DI Architecture

## Container Hierarchy & Installer Scopes
Reflex uses scene-scoped containers stitched into a parent chain via `SceneScope.OnSceneContainerBuilding`. The Bootstrap scene container becomes the parent for `WorldScene` and `BattleGroundScene`.

- **ProjectInstaller** (Project scope, lives across all scenes): `InputManager`, `LoadingService`, `CursorSetter`, `CommandDispatcher`. (`ConfigContainer` removed 2026-06-14 — JSON config retired.)
- **BootstrapInstaller** (Bootstrap scene): `GameLoopManager`, `GameManagerUIController`, `BootstrapFlow`, `ConfigHub` (was `PrototypeConfigSetter`), the hub's plain SO configs bound as their own types via `InstallHubConfigs` (`CameraConfigSO`, `PowerHitConfigSO`, `ThrowConfigSO`, `DaylightConfigSO` — each null-guarded), `BlobContainer` (`IDisposable` — disposes its persistent blob on teardown), `ThrowDebugTracker` (was `ThrowSettingsSetter`; `IGameListener`), `DotsGameLoopBridge`, `GameManager` (NonLazy), `ActiveSlot`, `DummySaveSystem` (as `ISaveSystem`). **These SO configs are bootstrap-scoped, so down-scene consumers (Camera/PowerHit in Battle, Daylight in World) resolve them via the SetParent chain.**
- **WorldSceneInstaller**: `WorldFlow`, `DaylightHandler`, `DayNightCycle` (now takes `DaylightConfigSO` from the bootstrap-scoped hub, not a scene `DayNightSetting`), `DaylightEcsBridge` (NonLazy + IDisposable), `WorldSceneData`, `ScrollController`, `WorldCameraHandler`, `Wallet` (bare singleton), `WorldSaveService` (NonLazy + IDisposable — hydrates the wallet, flushes on day end / scene unload). `Wallet` + save seam covered in [[currency-and-saves]]; `ActiveSlot` + `ISaveSystem` (`DummySaveSystem`) are Bootstrap-scoped and inherited here.
- **BattleGroundSceneInstaller**: `BattleSceneData`, `BattleGroundSceneFlow`, all battle-side input/camera (`MousePositionProvider`, `CursorMovementCalculations`, `GrabbedEntityMover`, `OverlapResolver`, `TunnelTeleporter`, `ReleaseCoordinator`, `TrajectoryPredictorSettings`, `ThrowTrajectoryPredictor`, `GrabbingInteractor`, `InteractController`, `PowerHitController`, `BattleCameraMovement`, `BattleCameraBorderSyncBridge`, `CursorEcsBridge`).

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
Many services have a separate `Initialize()` method (not an interface) called explicitly by their scene flow — e.g. `_interactController.Initialize()` in `BattleGroundSceneFlow.Start()`. This is because Reflex constructors run before the ECS world / scene objects are ready; `Initialize()` defers grabbing `World.DefaultGameObjectInjectionWorld.EntityManager` until safe.
