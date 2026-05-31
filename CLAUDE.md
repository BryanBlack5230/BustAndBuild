# CLAUDE.md

This is a game project by BarkingBird studio. For more information, load the [Project.md](Claude/Project.md) file.

## Folder Structure
```
Bust and Build/
├── CLAUDE.md                                       ← You are here (always loaded)
├── `Claude/`                                       ← Folder for claude related files
│   ├── `learnings/`                                ← Your knowledge database for internal use
│   ├── Project.md                                  ← Task router
│   └── Input.md                                    ← User inputed prompt
│
├── `Assets/`                                       ← Assets for Unity
│   ├── `!_Game/`                                   ← Assets made in BarkingBird studio
│   │   ├── Runtime.asmdef                          ← Assembly: `Runtime` (covers everything except Editor/)
│   │   ├── ProjectScope.prefab                     ← Reflex ProjectScope (ProjectInstaller lives here)
│   │   ├── `Editor/`                               ← Editor-only code, assembly `Editor`, namespace `BarkingBird.Editor`
│   │   │   ├── ConfigGenerator.cs / EditorConstants.cs / EditorSceneUtils.cs / SceneChainEditor.cs / ToolBox.cs
│   │   │   └── `SceneWorkflow/`                    ← EditorSceneCollectionRunner, SceneWorkflowHandoff, SceneWorkflowToolbox
│   │   └── `Runtime/`                              ← Runtime code split into Gameplay/ and Infrastructure/
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
│   │           ├── EventManager.cs / InputActions.cs / InputManager.cs / ReflexExtensions.cs / StateOverride.cs   ← root `...Infrastructure`
│   │           ├── GameLoop/                       ← `...Infrastructure.GameLoop`
│   │           ├── SceneWorkflow/                  ← `...Infrastructure.SceneWorkflow`
│   │           ├── Settings/                       ← `...Infrastructure.Settings` (configs, blob container, constants)
│   │           └── Utilities/                      ← `...Infrastructure.Utilities` (Log, AssetService, MathHelper, etc.)
│   └── Tasks.md                                    ← Task management memos (lives under !_Game/)
```

## Namespace Conventions
- Two assemblies: `Runtime` (everything under `Assets/!_Game/` minus `Editor/`) and `Editor` (`Assets/!_Game/Editor/`).
- All editor code: `namespace BarkingBird.Editor`.
- Runtime code is split into two top-level branches under `BarkingBird.Runtime.*`:
  - `Gameplay.{AI|Camera|Cursor|Daylight|Input|Input.GrabAndThrow|Scenes|Settings}` — scene/gameplay-bound code.
  - `Infrastructure` (root for `EventManager`, `InputManager`, `ReflexExtensions`, `StateOverride`) + `Infrastructure.{GameLoop|SceneWorkflow|Settings|Utilities}` — framework-level services.
- ECS Components & Systems are split: root-level files (`Components/*.cs`, `Systems/*.cs`) live in the **global namespace** (Unity-DOTS-friendly short names like `Health`, `Castle`, `ApplyDamageSystem`); the AI subfolders (`Components/AI/`, `Systems/AI/`) live in `BarkingBird.Runtime.Gameplay.AI`.
