# Project Overview — Bust and Build

High-level orientation map so future Claude doesn't need to re-derive folder structure or naming.

## Game Concept
"Bust and Build" — physics-based throw-em-up by BarkingBird Studio. Player grabs units/objects (cursor), flings them with mouse velocity → they fly, bounce off screen edges, damage enemies on impact. Battle is a unit RTS where allies and enemies path toward each other; player intervenes by physically throwing pieces.

## Project Root
- `Assets/!_Game/` — all studio-authored content (note `!_` prefix sorts to top). Two asmdefs: `Runtime/Runtime.asmdef` (moved from `!_Game/` root during v0.1.0) and `Editor/Editor.asmdef`.
- `Assets/!_Game/Editor/` — editor-only C#, namespace `BarkingBird.Editor` (`EditorConstants`, `EditorSceneUtils`, `SceneChainEditor`, `ToolBox`, `Formulas/{FormulaDrawer, FormulaReflectionResolver}`, `SceneWorkflow/{EditorSceneCollectionRunner, SceneWorkflowHandoff, SceneWorkflowToolbox}`). *(`ConfigGenerator` was deleted in the ConfigHub rework, 2026-06-14.)*
- `Assets/!_Game/Runtime/Gameplay/!_Scripts/` — gameplay C#. `Components/` (ECS authoring + structs, root files in **global namespace**; `Components/AI/` subfolder in `Gameplay.AI`), `Systems/` (ISystem, root files in **global namespace**; `Systems/AI/` subfolder in `Gameplay.AI`), `_MonoWorld/` (non-ECS — `Camera/`, `Cursor/`, `DaylightCycle/`, `Input/` (+`GrabAndThrow/`), `Scenes/` (ISceneFlow + installers), `Settings/` (+`StateOverrides/`)).
- `Assets/!_Game/Runtime/Infrastructure/` — framework services. Root files (`InputManager`, `ReflexExtensions`, `StateOverride`, `StringExtensions`) are `BarkingBird.Runtime.Infrastructure`; subfolders `Commands/`, `EventBus/`, `Formulas/`, `GameLoop/`, `Pooling/`, `Save/`, `SceneWorkflow/`, `Settings/`, `Utilities/` get their own subnamespaces. *(`EventManager` was retired — replaced by `EventBus` + `CommandDispatcher`; see [[events-and-services]].)*
- `Assets/!_Game/Runtime/Gameplay/Resources/` — runtime-loadable. `Cursors/Textures/*` + `Cursors/Sprites/*`, plus `Settings/{SceneCollections/*, SceneRunConfigurations/*, UnitProfiles/* + the ConfigHub SO assets (Camera/PowerHit/Throw/Daylight/Beacon/WallSection/Pickup), TrajectoryPredictorSettings.asset}` and asset folders (Audio/Materials/Models/Prefabs/Shaders/Textures). *(`Config.json` deleted with the JSON config, 2026-06-14.)*
- `Assets/!_Game/Runtime/Gameplay/Scenes/` — `0.Bootstrap`, `1.Loading`, `2.World`, `3.Battleground`, `4.City` (+ legacy `3.BattleGroundScene.unity`, `WorldECS.unity`, `BattleGroundSceneECS.unity`). Indexed prefixes are load order.

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

## Namespaces (post v0.1.0 reorg)
All runtime code lives under `BarkingBird.Runtime.*` (except root-level ECS files, see below); all editor code under `BarkingBird.Editor`. The split mirrors folder layout — `Gameplay.AI` (only `Components/AI/` and `Systems/AI/` subfolders), `Gameplay.{Camera|Cursor|Daylight|Input|Input.GrabAndThrow|Scenes|Settings}`, and `Infrastructure` root + `Infrastructure.{GameLoop|SceneWorkflow|Settings|Utilities}`. There are no longer any `Game.*` / `GameEngine.*` / `GameManagement` / `Game.Configs` namespaces — those were collapsed during the v0.1.0 reorganization.

**ECS root-namespace convention:** files directly in `Components/` and `Systems/` (root, not the `AI/` subfolder) have **no namespace** — they live in the global/root namespace. This is intentional: it follows Unity DOTS norms so `Health`, `Castle`, `Beacon`, `WallSection`, `ApplyDamageSystem`, etc. stay short and unqualified. AI-pipeline scripts (brain, steering, targeting, pathfinding, unit movement & registration) sit in `Components/AI/` + `Systems/AI/` under `BarkingBird.Runtime.Gameplay.AI`. When adding a new ECS file, decide which bucket it belongs to — if it's part of the AI pipeline, place it in `AI/` and namespace it accordingly; otherwise leave it in root with no namespace.

## Designed-But-Not-Started Refactors (agreed 2026-06-10)
Full specs live next to this folder — read them before touching the affected systems:
- ~~`Claude/ConfigTask.md`~~ — **DONE & removed (2026-06-14).** The SO-based ConfigHub (promoted from `PrototypeConfigSetter`, per-unit-type profile SOs tied to enums, `*Config` SOs for tunables vs `*Constants` for invariants, JSON config fully retired) shipped across Phases 1/2/3/5. Full reference: [[config-system]].
- `Claude/CurrencyTask.md` — `Wallet` (multi-currency) + per-world `WorldSaveData` saves; wallet stays World-scoped, `SaveRepository` at Bootstrap.

## Code Style Quirks Observed
- `#nullable enable` used in newer files but inconsistently across the codebase.
- Tab indentation in some older files (e.g. `GameLoopManager`, `DayNightCycle`); spaces elsewhere. Match the file you're editing.
- Private serialized fields use `_camelCase`; public fields use plain `camelCase`/`PascalCase`. ECS struct fields use `PascalCase` (newer) or `camelCase` (older — `moveSpeed`, `turnSpeed`).
