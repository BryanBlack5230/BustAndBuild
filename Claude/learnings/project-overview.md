# Project Overview — Bust and Build

High-level orientation map so future Claude doesn't need to re-derive folder structure or naming.

## Game Concept
"Bust and Build" — physics-based throw-em-up by BarkingBird Studio. Player grabs units/objects (cursor), flings them with mouse velocity → they fly, bounce off screen edges, damage enemies on impact. Battle is a unit RTS where allies and enemies path toward each other; player intervenes by physically throwing pieces.

## Project Root
- `Assets/!_Game/` — all studio-authored content (note `!_` prefix sorts to top).
- `Assets/!_Game/!_Scripts/` — all C#. Sub-organization: `Components/` (ECS authoring + structs), `Systems/` (ISystem), `Core/` (DI/GameLoop/Events/Utilities), `MonoWorld/` (non-ECS gameplay), `Scenes/` (ISceneFlow), `Settings/` (configs + state overrides), `Editor/`.
- `Assets/!_Game/Resources/` — runtime-loadable: `Config.json`, `Cursors/Textures/*`, `SceneRunConfigurations/*.asset`, `TrajectoryPredictorSettings.asset`.
- `Assets/!_Game/Scenes/` — `0.Bootstrap`, `1.Loading`, `2.World`, `3.BattleGroundScene`, `4.City`. Indexed prefixes are load order.

## Build/Runtime Stack
- Unity DOTS/ECS (Unity.Entities + Unity.Physics + Burst) for battle simulation.
- **Reflex** DI (scene-scoped containers, IInstaller). See [[di-architecture]].
- **UniTask** for async, **UniRx**/**PrimeTween** sparingly, **Cinemachine** for cameras.
- **Odin Inspector** (Sirenix) — `[Button]`, `[ShowIf]`, `[HorizontalGroup]`, `[GUIColor]`, `[ReadOnly]`, `[AssetList]`, `[InlineEditor]`, `[MinMaxSlider]`.
- **New Input System** (`InputActions.inputactions` → generated `InputActions.cs`).
- **Newtonsoft.Json** for config deserialization.

## Key Singletons / Entry Points
- `World.DefaultGameObjectInjectionWorld` — ECS world; MonoWorld code grabs it directly in ctor.
- `GameManager` — `NonLazy<>()` so Reflex resolves on container build; subscribes UI buttons → start/pause/resume/finish.
- `GameLoopManager` (MonoBehaviour, in Bootstrap scene) — runs `Update`/`FixedUpdate`/`LateUpdate` for all `IGameListener`s. See [[game-loop-listeners]].
- `DotsGameLoopBridge` — gates the `GameLoopSystemGroup` enabled flag from `IGameStart/Pause/Resume/Finish`.

## Naming
- ECS authoring MonoBehaviours: `{Thing}Authoring` with nested `Baker : Baker<{Thing}Authoring>`. Component structs declared in same file.
- Scene flow MonoBehaviours: `{Scene}Flow : ISceneFlow` (e.g. `BootstrapFlow`, `WorldFlow`, `BattleGroundSceneFlow`).
- Reflex installers: `{Scope}Installer : MonoBehaviour, IInstaller` (Project / Bootstrap / WorldScene / BattleGroundScene).
- ECS systems: `{Concern}System : ISystem` (struct, `[BurstCompile]` when possible).
- Job structs: `{Concern}Job : IJobEntity` (or `IJob`/`ICollisionEventsJob`).

## Code Style Quirks Observed
- Mixed namespaces — many root-level types (no namespace) for ECS components; `GameEngine.*`, `Game.*`, `GameManagement`, `Game.Configs`, `Game.Feature.Input`, `Game.Feature.Camera`, `Game.SceneWorkflow`, `Game.Editor`.
- `#nullable enable` used in newer files but inconsistently across the codebase.
- Tab indentation in some older files (e.g. `GameLoopManager`, `DayNightCycle`); spaces elsewhere. Match the file you're editing.
- Private serialized fields use `_camelCase`; public fields use plain `camelCase`/`PascalCase`. ECS struct fields use `PascalCase` (newer) or `camelCase` (older — `moveSpeed`, `turnSpeed`).
