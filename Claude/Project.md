# Project.md — Bust and Build

## Project Overview

"Bust and Build" is a Unity 6 (Unity DOTS / Entities 1.4.2) game in god-game genre. Main loop consists of player grabbing and throwing units, influencing combat, and interacts with the environment to defend the Castle and Beacon inside of it. Meta loop consists of a colony simulator. With resourses player builds houses, upgrades, prepares for next battle.

## Architecture
The project uses a **hybrid MonoBehaviour + ECS (DOTS)** architecture. The game loop is controlled by `GameLoopManager`, All services are wired with [Reflex](https://github.com/gustavopsantos/Reflex). Each scene has a scoped installer and a flow that controls initialisation.
Scenes are loaded additively, in the following order: Bootstrap → Loading → World → Battle || City

Bootstrap scene is a startup scene that loads the game loop and configures the game.
World scene contains the world map, and the shared data for Battle and City scenes.
Battle scene is the core gameplay loop scene. When beacon is active, Sun is out and waves of enemies are attacking the castle to destroy the Beacon inside it.
City scene is a meta loop that simulates a colony.
```
MonoWorld (MonoBehaviour — input, camera, cursor)
    ↓ bridges via
ECS GameLoopSystemGroup (DOTS simulation — AI, combat, movement)
    ↓ controlled by
GameLoopManager (Reflex DI — game state machine)
```

### Assemblies & Namespaces

Two assemblies, both rooted under `Assets/!_Game/`:

| Assembly | asmdef | Root namespace |
|---|---|---|
| `Runtime` | `!_Game/Runtime.asmdef` (covers everything except `Editor/`) | `BarkingBird.Runtime.*` |
| `Editor` | `!_Game/Editor/Editor.asmdef` | `BarkingBird.Editor` |

Runtime code is split into two trees:

| Folder | Namespace |
|---|---|
| `Runtime/Gameplay/!_Scripts/Components/` (root files) | _(global, no namespace)_ |
| `Runtime/Gameplay/!_Scripts/Components/AI/` | `BarkingBird.Runtime.Gameplay.AI` |
| `Runtime/Gameplay/!_Scripts/Systems/` (root files) | _(global, no namespace)_ |
| `Runtime/Gameplay/!_Scripts/Systems/AI/` | `BarkingBird.Runtime.Gameplay.AI` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Camera/` | `BarkingBird.Runtime.Gameplay.Camera` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Cursor/` | `BarkingBird.Runtime.Gameplay.Cursor` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/DaylightCycle/` | `BarkingBird.Runtime.Gameplay.Daylight` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Input/` (root) | `BarkingBird.Runtime.Gameplay.Input` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Input/GrabAndThrow/` | `BarkingBird.Runtime.Gameplay.Input.GrabAndThrow` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Scenes/` | `BarkingBird.Runtime.Gameplay.Scenes` |
| `Runtime/Gameplay/!_Scripts/_MonoWorld/Settings/` (+ `StateOverrides/`) | `BarkingBird.Runtime.Gameplay.Settings` |
| `Runtime/Infrastructure/` (root files only) | `BarkingBird.Runtime.Infrastructure` |
| `Runtime/Infrastructure/GameLoop/` | `BarkingBird.Runtime.Infrastructure.GameLoop` |
| `Runtime/Infrastructure/SceneWorkflow/` | `BarkingBird.Runtime.Infrastructure.SceneWorkflow` |
| `Runtime/Infrastructure/Settings/` | `BarkingBird.Runtime.Infrastructure.Settings` |
| `Runtime/Infrastructure/Utilities/` | `BarkingBird.Runtime.Infrastructure.Utilities` |

**ECS namespace split:** root-level `Components/` and `Systems/` files (shared building blocks — `Health`, `Castle`, `Beacon`, `WallSection`, `Attack`, `InAir`, `Grabbed`, `Spawn`, `BattleCenter`, `BattleCoordinator`, `BounceDamage`, `CameraFrustumData`, `ThrowVelocitySettings`, `ApplyDamageSystem`, `AttackSystem`, `BattleCoordinatorSystem`, `BattleDirectorCleanupSystem`, `CastleBreachSystem`, `DeathSystem`, `GizmoDrawSystem`, `InAirCollisionSystem`, `ScreenBounceSystem`, `SpawningSystem`, `WallSectionInitSystem`) live in the **global namespace** — typical Unity-DOTS convention so component/system type names stay short. AI-pipeline code (Brain/Steering/Targeting/Pathfinding/Movement/Unit registration & ability evaluation) lives in `Components/AI/` and `Systems/AI/` under `BarkingBird.Runtime.Gameplay.AI`.

`Runtime/Infrastructure/` root holds the cross-cutting services that aren't part of any subdomain: `EventBus` (in `EventBus/` subfolder), `InputManager`, `ReflexExtensions`, `StateOverride` (abstract base — concrete overrides live in `Gameplay.Settings`).

### Dependency Injection (Reflex)

| Installer | Scope | Binds |
|---|---|---|
| `ProjectInstaller` | Global | `InputManager`, `LoadingService`, `ConfigContainer`, `CursorSetter` |
| `BootstrapInstaller` | Bootstrap scene | `GameLoopManager`, `GameManagerUIController`, `BootstrapFlow`, `PrototypeConfigSetter`, `BlobContainer`, `ThrowSettingsSetter`, `DotsGameLoopBridge`, `GameManager` (NonLazy) |
| `WorldSceneInstaller` | World scene | `WorldFlow`, `DayNightCycle`, `WorldSceneData`, `ScrollController`, `WorldCameraHandler` |
| `BattleGroundSceneInstaller` | Battle scene | `BattleSceneData`, `BattleGroundSceneFlow`, `MousePositionProvider`, `CursorMovementCalculations`, `GrabbedEntityMover`, `OverlapResolver`, `TunnelTeleporter`, `ReleaseCoordinator`, `TrajectoryPredictorSettings`, `ThrowTrajectoryPredictor`, `GrabbingInteractor`, `InteractController`, `PowerHitController`, `BattleCameraMovement`, `BattleCameraBorderSyncBridge` (registration order matters — `ThrowTrajectoryPredictor` must precede `GrabbingInteractor`) |

Containers are hierarchical: Bootstrap → World → Battle. Use `AddInterfacesAndSelf<T>()` and `NonLazy<T>()` extensions defined in `Runtime/Infrastructure/ReflexExtensions.cs` (namespace `BarkingBird.Runtime.Infrastructure`).

### Game Loop State Machine

`GameLoopManager` owns game state (Start / Pause / Resume / Finish). Classes opt in by implementing listener interfaces from `Runtime/Infrastructure/GameLoop/GameListeners.cs` (namespace `BarkingBird.Runtime.Infrastructure.GameLoop`):

- `IGameStartListener`, `IGamePauseListener`, `IGameResumeListener`, `IGameFinishListener`
- `IGameUpdateListener`, `IGameFixedUpdateListener`, `IGameLateUpdateListener`

`DotsGameLoopBridge` enables/disables `GameLoopSystemGroup` when game state changes, keeping ECS and MonoBehaviour in sync.

### ECS Systems (DOTS)

`GameLoopSystemGroup` is a `ComponentSystemGroup` placed inside `SimulationSystemGroup` via `[UpdateInGroup(typeof(SimulationSystemGroup))]`, after `BeginSimulationEntityCommandBufferSystem`. `DotsGameLoopBridge` toggles its `Enabled` flag so pause-aware gameplay systems freeze with the game. Systems that should keep running regardless live in other groups: `ApplyDamageSystem` and `DeathSystem` (`SimulationSystemGroup, OrderLast = true`), `ScreenBounceSystem` (`FixedStepSimulationSystemGroup` after `PhysicsSystemGroup`), `InAirCollisionSystem` (`PhysicsSystemGroup` after `PhysicsSimulationGroup`), `WallSectionInitSystem` (`InitializationSystemGroup`).

### Scene Workflow System

`SceneWorkflowRunner` bootstraps the full scene chain at runtime via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`.

- **`RunConfiguration`** (SO, `Runtime/Gameplay/Resources/Settings/SceneRunConfigurations/`) — pairs a `SceneChain` with a list of `StateOverride`s. Flags: `IsDefault` (one per chain, editor default) and `IsBuildConfig` (one global, used in builds). Lives in `BarkingBird.Runtime.Infrastructure.SceneWorkflow`.
- **`SceneChain`** (SO, assets in `Resources/Settings/SceneCollections/`) — ordered list of scenes loaded additively. Each element stores name + path (synced from `SceneAsset` in editor).
- **`StateOverride`** — abstract async action applied after all scenes are loaded. Base class at `Runtime/Infrastructure/StateOverride.cs`; concrete subclasses in `Runtime/Gameplay/!_Scripts/_MonoWorld/Settings/StateOverrides/`: `GameLoopStateOverride` (Start / Pause / Finish), `ActiveCameraOverride` (sends `ChangeSceneCommand` via `CommandDispatcher` to toggle bird-view/battle cam), `DayCycleStateOverride` (sends `StartDayCommand` / `ForceFinishDayCommand`).
- **Runtime**: In builds, loads the `IsBuildConfig` config from Resources and loads scenes in order, waiting for each scene's `ISceneFlow.WaitForInit()` before loading the next.
- **Editor**: `SceneWorkflowToolbox` (`BarkingBird/Scenes/Scene Workflow`, `Ctrl+Shift+W`) writes the selected config to `SessionState`; runner picks it up on Play. Single Scene Mode loads Bootstrap + current scene only.

**Configs in `Runtime/Gameplay/Resources/Settings/SceneRunConfigurations/`:**

| Asset | Purpose |
|---|---|
| `BattleDev` | Jump straight to Battle scene |
| `CityDev` | Jump straight to City scene |
| `NormalDev` | Full game flow (dev default) |
| `NormalProd` | Full game flow (build config) |

### Configuration Pipeline

JSON configs are loaded at bootstrap via `AssetService` (Resources), parsed with Newtonsoft.Json into `ConfigContainer`. `BlobContainer.Initialize()` then builds a `BlobAssetReference<TargetProfilesBlob>` on a singleton entity for Burst-safe target scoring.

**Caveat:** `BlobContainer` currently sources `EnemyProfiles`/`AllyProfiles` from `PrototypeConfigSetter` (a MonoBehaviour in the Bootstrap scene) — the `ConfigContainer.Battle.*Profiles` path is commented out. Editing `Config.json` does **not** currently change AI behaviour. The generic `BlobConfigConverter` helper exists but is not invoked anywhere yet.

### Authoring / Baker Pattern

Every ECS component has a corresponding `*Authoring` MonoBehaviour with a nested `Baker` class. Authoring lives in `Runtime/Gameplay/!_Scripts/Components/` (root authoring in global namespace; AI-pipeline authoring in `Components/AI/` under `BarkingBird.Runtime.Gameplay.AI`). The baker converts inspector-configured data into component data at bake time.

Enableable components (`IEnableableComponent`) are used extensively for conditional behavior: `IsDead`, `UnableToAct`, `Grabbed`, `InAir`, `SteeringEnabled`, `UnitMover`, `UnitRegisteredTag`, `AttackCooldownExpirationTimestamp`, `TargetSearchCooldownExpirationTimestamp`.

### MonoWorld Systems

Non-ECS gameplay code lives under `Runtime/Gameplay/!_Scripts/_MonoWorld/`. Each subfolder maps 1:1 to a namespace under `BarkingBird.Runtime.Gameplay.*`.

**Input** (`_MonoWorld/Input/` → `BarkingBird.Runtime.Gameplay.Input`, with `GrabAndThrow/` subnamespace): `InteractController` raycasts (LMB) for grabbable units → `GrabbingInteractor.Grab(entity)` freezes physics (`PhysicsMass.InverseMass = 0`) and enables `Grabbed`. `GrabbedEntityMover` snaps the entity to mouse-world-projection each frame, with ground and viewport clamping. `CursorMovementCalculations` (in `_MonoWorld/Cursor/`) tracks `velocity` and `acceleration` for throw power. On LMB up, `InteractController.OnCanceled` → `GrabbingInteractor.Release()` → `ReleaseCoordinator.HandleRelease(...)` decides between direct launch, `OverlapResolver` (slow-throw displacement) or `TunnelTeleporter` (fast-throw tunneling through obstacles). `ThrowTrajectoryPredictor` (registered before `GrabbingInteractor`) draws a predicted arc + impact circle while held. `PowerHitController` (RMB) is wired but currently a no-op placeholder.

**Camera** (`_MonoWorld/Camera/` → `BarkingBird.Runtime.Gameplay.Camera`): `BattleCameraMovement` handles drag with border constraints. `CameraInputHandler` → `CameraDragHandler` → `CameraBorderHandler` chain. `BattleCameraBorderSyncBridge` writes a `CameraFrustumData` ECS singleton (`worldToCameraMatrix`, FOV, aspect, IsLive) each frame; `ScreenBounceSystem` and `ThrowTrajectoryPredictor` read it for screen-edge bounce math.

### Key Packages

| Package | Purpose |
|---|---|
| `Unity.Entities` 1.4.2 | ECS / DOTS runtime |
| `Unity.Physics` 1.4.2 | DOTS physics |
| `Reflex` | Dependency injection |
| `UniTask` | Async/await without allocations |
| `PrimeTween` | Tweening |
| `Cinemachine` 2.10.5 | Camera rigs |
| `InputSystem` 1.14.2 | New input system |
| `Newtonsoft.Json` | Config parsing |
| `Odin` | Inspector toolset |

## Skills & Tools

| Task                         | Skill                   | Overview                                                                 |
|------------------------------|-------------------------|--------------------------------------------------------------------------|
| **write code for Unity**     | /unity-coding-standards | Enforce BarkingBird studio development standards                         |
| **review**                   | /code-review-unity      | Review Unity C# code against Unity's official style guide                |
| **write code for inspector** | /odin-visual-designer   | Configure Unity inspectors, attributes, layout, and Odin Visual Designer |