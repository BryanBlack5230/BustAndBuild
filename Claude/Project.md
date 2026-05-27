# Project.md — Bust and Build

## Project Overview

"Bust and Build" is a Unity 6 (Unity DOTS / Entities 1.4.2) game in god-game genre. Main loop consists of player grabbing and throwing units, influencing combat, and interacts with the environment to defend the Castle and Beacon inside of it. Meta loop consists of a colony simulator. With resourses player builds houses, upgrades, prepares for next battle.

## Architecture
The project uses a **hybrid MonoBehaviour + ECS (DOTS)** architecture. The game loop is controlled by `GameLoopManager`, All services are wired with [Reflex](https://github.com/gustavopsantos/Reflex). Each scene has a scoped installer and a flow that controls initialisation.
Scenes are loaded additevly, in the following order: Bootstrap → World → Battle || City

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

### Dependency Injection (Reflex)

| Installer | Scope | Binds |
|---|---|---|
| `ProjectInstaller` | Global | `InputManager`, `LoadingService`, `ConfigContainer`, `CursorSetter` |
| `BootstrapInstaller` | Bootstrap scene | `GameLoopManager`, `GameManager`, `BlobContainer` |
| `WorldSceneInstaller` | World scene | `WorldFlow`, `DayNightCycle`, `ScrollController` |
| `BattleGroundSceneInstaller` | Battle scene | Camera handlers, input controllers, `InteractController`, `PowerHitController` |

Containers are hierarchical: Bootstrap → World → Battle. Use `AddInterfacesAndSelf<T>()` and `NonLazy<T>()` extensions defined in `Core/DI/`.

### Game Loop State Machine

`GameLoopManager` owns game state (Start / Pause / Resume / Finish). Classes opt in by implementing listener interfaces from `Core/GameLoop/GameListeners.cs`:

- `IGameStartListener`, `IGamePauseListener`, `IGameResumeListener`, `IGameFinishListener`
- `IGameUpdateListener`, `IGameFixedUpdateListener`, `IGameLateUpdateListener`

`DotsGameLoopBridge` enables/disables `GameLoopSystemGroup` when game state changes, keeping ECS and MonoBehaviour in sync.

### ECS Systems (DOTS)

All simulation runs inside `GameLoopSystemGroup` (extends `SimulationSystemGroup`).

### Configuration Pipeline

JSON configs are loaded at bootstrap via `AssetService` (Resources), parsed with Newtonsoft.Json into `ConfigContainer`, then baked into DOTS Blob Assets by `BlobContainer` / `BlobConfigConverter` for Burst-safe access inside systems.

### Authoring / Baker Pattern

Every ECS component has a corresponding `*Authoring` MonoBehaviour with a nested `Baker` class. Authoring lives in `Components/`. The baker converts inspector-configured data into component data at bake time.

Enableable components (`IEnableableComponent`) are used extensively for conditional behavior: `IsDead`, `UnableToAct`, `Grabbed`, `SteeringEnabled`, `AttackCooldownExpirationTimestamp`, `TargetSearchCooldownExpirationTimestamp`.

### MonoWorld Systems

**Input** (`MonoWorld/Input/`): `InteractController` raycasts for grabbable units → `GrabbingInteractor` freezes physics and moves the entity → `PowerHitController` releases with force. `CursorMovementCalculations` tracks velocity for throw power.

**Camera** (`MonoWorld/Camera/`): `BattleCameraMovement` handles drag with border constraints. `CameraInputHandler` → `CameraDragHandler` → `CameraBorderHandler` chain. `BattleCameraBorderSyncBridge` syncs camera frustum bounds into a physics collider for ECS border queries.

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
| **write code for Unity**     | /unity-coding-standards | Enforce BarkingBierd studio development standards                        |
| **review**                   | /code-review-unity      | Review Unity C# code against Unity's official style guide                |
| **write code for inspector** | /odin-visual-designer   | Configure Unity inspectors, attributes, layout, and Odin Visual Designer |