# CLAUDE.md

This is a game project by BarkingBird studio. For more information, load the [Project.md](Claude/Project.md) file.

## Folder Structure
```
Bust and Build/
├── CLAUDE.md                                       ← You are here (always loaded)
├── `Claude/`                                       ← Folder for claude related files
│   ├── `learnings/`                                ← Your knowledge database (see "Learnings Routing" below)
│   ├── Project.md                                  ← Task router
│   ├── Input.md                                    ← User inputed prompt
│   ├── ConfigTask.md                               ← Agreed design: SO config hub refactor (read before touching config/balance)
│   └── CurrencyTask.md                             ← Agreed design: Wallet + per-world saves (read before touching currency)
│
├── `Assets/`                                       ← Assets for Unity
│   ├── `!_Game/`                                   ← Assets made in BarkingBird studio
│   │   ├── ProjectScope.prefab                     ← Reflex ProjectScope (ProjectInstaller lives here)
│   │   ├── `Editor/`                               ← Editor-only code, assembly `Editor`, namespace `BarkingBird.Editor`
│   │   │   ├── ConfigGenerator.cs / EditorConstants.cs / EditorSceneUtils.cs / SceneChainEditor.cs / ToolBox.cs
│   │   │   ├── `Formulas/`                         ← FormulaDrawer + FormulaReflectionResolver (inspector for `[Formula]` fields)
│   │   │   └── `SceneWorkflow/`                    ← EditorSceneCollectionRunner, SceneWorkflowHandoff, SceneWorkflowToolbox
│   │   └── `Runtime/`                              ← Runtime code split into Gameplay/ and Infrastructure/
│   │       ├── Runtime.asmdef                      ← Assembly: `Runtime` (covers everything under Runtime/)
│   │       ├── `Gameplay/`                         ← namespace `BarkingBird.Runtime.Gameplay.*`
│   │       │   ├── Data/                           ← InputActions.inputactions, PostProcessing Profile
│   │       │   ├── Resources/                      ← Audio, Cursors, Materials, Models, Prefabs, Shaders, Textures
│   │       │   │   └── Settings/                   ← Config.json, SceneCollections/, SceneRunConfigurations/, TrajectoryPredictorSettings.asset
│   │       │   ├── Scenes/                         ← 0.Bootstrap, 1.Loading, 2.World, 3.Battleground, 4.City (+ ECS scenes)
│   │       │   └── !_Scripts/                      ← All gameplay C#
│   │       │       ├── Components/                 ← ECS component structs + Authoring (root files are in global namespace)
│   │       │       │   └── AI/                     ← AI-specific authoring (`...Gameplay.AI`)
│   │       │       ├── Systems/                    ← ECS systems (root files are in global namespace)
│   │       │       │   └── AI/                     ← AI-pipeline systems (`...Gameplay.AI`)
│   │       │       └── _MonoWorld/                 ← Non-ECS gameplay (MonoBehaviours)
│   │       │           ├── Camera/                 ← `...Gameplay.Camera`
│   │       │           ├── Cursor/                 ← `...Gameplay.Cursor`
│   │       │           ├── DaylightCycle/          ← `...Gameplay.Daylight`
│   │       │           ├── Input/                  ← `...Gameplay.Input`, with `GrabAndThrow/` subnamespace
│   │       │           ├── Scenes/                 ← `...Gameplay.Scenes` (flows + installers)
│   │       │           └── Settings/               ← `...Gameplay.Settings` (incl. StateOverrides/)
│   │       └── `Infrastructure/`                   ← namespace `BarkingBird.Runtime.Infrastructure.*`
│   │           ├── InputManager.cs / ReflexExtensions.cs / StateOverride.cs / StringExtensions.cs   ← root `...Infrastructure`
│   │           ├── Commands/                       ← `...Infrastructure.Commands` (CommandDispatcher)
│   │           ├── EventBus/                       ← `...Infrastructure` (static EventBus + IEvent)
│   │           ├── Formulas/                       ← `...Infrastructure.Formulas` (`[Formula]` strings, FormulaParser/FormulaEvaluator)
│   │           ├── GameLoop/                       ← `...Infrastructure.GameLoop`
│   │           ├── Pooling/                        ← `...Infrastructure.Pooling` (UiPool/UiPoolItem — data-keyed UI list rows)
│   │           ├── Save/                           ← `...Infrastructure.Save` (ISaveSystem, DummySaveSystem, ActiveSlot)
│   │           ├── SceneWorkflow/                  ← `...Infrastructure.SceneWorkflow`
│   │           ├── Settings/                       ← `...Infrastructure.Settings` (configs, blob container, constants)
│   │           └── Utilities/                      ← `...Infrastructure.Utilities` (Log, AssetService, MathHelper, etc.)
│   └── Tasks.md                                    ← Task management memos (lives under !_Game/)
```

## Learnings Routing (read BEFORE coding, not after getting stuck)
`Claude/learnings/` holds verified project knowledge from past sessions — gotchas, system maps, design decisions with rationale. **Do not re-derive from source what is already written there.** Before non-trivial work, load the matching file:

| Task touches | Load |
|---|---|
| Anything (orientation) | `learnings/README.md` (index) + `learnings/project-overview.md` |
| ECS systems/components/pipeline | `learnings/ecs-architecture.md`, `learnings/ecs-patterns.md` |
| Damage, death, collisions, hit feedback, throwing | `learnings/ecs-combat-and-collisions.md` |
| Unity Physics placement/baking/AABB/layers | `learnings/unity-physics-gotchas.md` |
| AI, targeting, steering, escape/retreat | `learnings/steering-and-ai.md` |
| DI, installers, new services | `learnings/di-architecture.md` |
| Currency, wallet, saves, persistence, slots | `learnings/currency-and-saves.md` |
| Events, commands, messaging decisions | `learnings/events-and-services.md` |
| Game loop, pause, listeners | `learnings/game-loop-listeners.md` |
| Input, grab/throw, cursor, camera drag | `learnings/input-system.md` |
| Scenes, RunConfigurations, editor workflow | `learnings/scene-flow-system.md` |
| Static state, play mode, Hot Reload limits | `learnings/play-mode-and-hot-reload.md` |
| Inspectors, SO assets, editor windows | `learnings/{custom-inspector,scriptable-object,editor-window}-patterns.md` |

After completing work that produced non-obvious discoveries, offer to run `/knowledge-save`.

## Namespace Conventions
- Two assemblies: `Runtime` (everything under `Assets/!_Game/Runtime/`) and `Editor` (`Assets/!_Game/Editor/`).
- All editor code: `namespace BarkingBird.Editor`.
- Runtime code is split into two top-level branches under `BarkingBird.Runtime.*`:
  - `Gameplay.{AI|Camera|Cursor|Daylight|Input|Input.GrabAndThrow|Scenes|Settings}` — scene/gameplay-bound code.
  - `Infrastructure` (root for `InputManager`, `ReflexExtensions`, `StateOverride`, plus `EventBus/` which stays in the root namespace) + `Infrastructure.{Commands|GameLoop|SceneWorkflow|Settings|Utilities}` — framework-level services.
- ECS Components & Systems are split: root-level files (`Components/*.cs`, `Systems/*.cs`) live in the **global namespace** (Unity-DOTS-friendly short names like `Health`, `Castle`, `ApplyDamageSystem`); the AI subfolders (`Components/AI/`, `Systems/AI/`) live in `BarkingBird.Runtime.Gameplay.AI`.
