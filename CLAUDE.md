# CLAUDE.md

When reporting information to me, be extremely concise and sacrifice grammar for the sake of concision.
This is a game project by BarkingBird studio. For more information, load the [Project.md](Claude/Project.md) file.

## Folder Structure
```
Bust and Build/
├── CLAUDE.md                                       ← You are here (always loaded)
├── `Claude/`                                       ← Folder for claude related files
│   ├── `learnings/`                                ← Your knowledge database (see "Learnings Routing" below)
│   ├── Project.md                                  ← Task router
│   ├── Input.md                                    ← User inputed prompt
│   └── DistanceCalculationsRefactorTask.md         ← Agreed design: surface-distance (AABB/broadphase) for brain/scorer/steering (read before touching attack-range/target-detection/obstacle-avoidance)
│
├── `Assets/`                                       ← Assets for Unity
│   ├── `!_Game/`                                   ← Assets made in BarkingBird studio
│   │   ├── ProjectScope.prefab                     ← Reflex ProjectScope (ProjectInstaller lives here)
│   │   ├── `Editor/`                               ← Editor-only code, assembly `Editor`, namespace `BarkingBird.Editor`
│   │   │   ├── EditorConstants.cs / EditorSceneUtils.cs / MissingScriptsFinder.cs / SceneChainEditor.cs / ToolBox.cs
│   │   │   ├── `Formulas/`                         ← FormulaDrawer + FormulaReflectionResolver (inspector for `[Formula]` fields)
│   │   │   └── `SceneWorkflow/`                    ← EditorSceneCollectionRunner, SceneWorkflowHandoff, SceneWorkflowToolbox
│   │   └── `Runtime/`                              ← Runtime code split into Gameplay/ and Infrastructure/
│   │       ├── Runtime.asmdef                      ← Assembly: `Runtime` (covers everything under Runtime/)
│   │       ├── `Gameplay/`                         ← namespace `BarkingBird.Runtime.Gameplay.*`
│   │       │   ├── Data/                           ← InputActions.inputactions, PostProcessing Profile
│   │       │   ├── Resources/                      ← Audio, Cursors, Materials, Models, Prefabs, Shaders, Textures
│   │       │   │   └── Settings/                   ← `*Config.asset` SO profiles (ConfigHub) + UnitProfiles/ + HitFeedback/, SceneCollections/, SceneRunConfigurations/, TrajectoryPredictorSettings.asset
│   │       │   ├── Scenes/                         ← 0.Bootstrap, 1.Loading, 2.World, 3.Battleground, 4.City (+ ECS scenes)
│   │       │   └── !_Scripts/                      ← All gameplay C#
│   │       │       ├── Components/                 ← ECS component structs + Authoring (root files are in global namespace)
│   │       │       │   └── AI/                     ← AI-specific authoring (`...Gameplay.AI`)
│   │       │       ├── Systems/                    ← ECS systems (root files are in global namespace)
│   │       │       │   └── AI/                     ← AI-pipeline systems (`...Gameplay.AI`)
│   │       │       └── _MonoWorld/                 ← Non-ECS gameplay (MonoBehaviours)
│   │       │           ├── Camera/                 ← Camera controlls
│   │       │           ├── Cursor/                 ← Cursor movement and representation
│   │       │           ├── DaylightCycle/          ← Day and night cycle
│   │       │           ├── Input/                  ← User inputs, with `GrabAndThrow/` subnamespace
│   │       │           ├── Scenes/                 ← Scene flows and installers
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
│   │           ├── Settings/                       ← `...Infrastructure.Settings` (ConfigHub, BlobContainer, constants)
│   │           └── Utilities/                      ← `...Infrastructure.Utilities` (Log, AssetService, MathHelper, etc.)
│   └── Tasks.md                                    ← Task management memos (lives under !_Game/)
```

## Learnings Routing (read BEFORE coding, not after getting stuck)
`Claude/learnings/` holds verified project knowledge from past sessions — **how-it-works, gotchas, and system maps** (one of the three knowledge sinks; glossary terms live in `CONTEXT.md`, decisions in `Claude/docs/adr/` — see Agent skills › Domain docs). **Do not re-derive from source what is already written there.** Before non-trivial work, load the matching file:

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
| Writing/running tests, TDD, test asmdefs | `learnings/testing-methodology.md` |
| Merging Unity files, `.unity`/`.prefab` conflicts | `learnings/merge-conflicts.md` |

After completing work that produced non-obvious discoveries, offer to run `/knowledge-save`.

## Namespace Conventions
- Two assemblies: `Runtime` (everything under `Assets/!_Game/Runtime/`) and `Editor` (`Assets/!_Game/Editor/`).
- All editor code: `namespace BarkingBird.Editor`.
- Runtime code is split into two top-level branches under `BarkingBird.Runtime.*`:
  - `Gameplay.{AI|Camera|Cursor|Daylight|Input|Input.GrabAndThrow|Scenes|Settings}` — scene/gameplay-bound code.
  - `Infrastructure` (root for `InputManager`, `ReflexExtensions`, `StateOverride`, plus `EventBus/` which stays in the root namespace) + `Infrastructure.{Commands|GameLoop|SceneWorkflow|Settings|Utilities}` — framework-level services.
- ECS Components & Systems are split: root-level files (`Components/*.cs`, `Systems/*.cs`) live in the **global namespace** (Unity-DOTS-friendly short names like `Health`, `Castle`, `ApplyDamageSystem`); the AI subfolders (`Components/AI/`, `Systems/AI/`) live in `BarkingBird.Runtime.Gameplay.AI`.

## Agent skills

### Issue tracker
GitHub Issues via the `gh` CLI; external PRs are not a triage surface. See `Claude/docs/agents/issue-tracker.md`.

### Triage labels
Default vocabulary (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `Claude/docs/agents/triage-labels.md`.

### Domain docs — the three knowledge sinks
Knowledge splits three ways by **shape** (full rationale: `Claude/docs/adr/0001-adopting-mattpocock-skills.md`):
- **glossary** — what a term *means* → `CONTEXT.md` (repo root); written by `/domain-modeling`
- **decisions** — real trade-offs, hard to reverse → `Claude/docs/adr/`; written by `/domain-modeling`
- **how-it-works / gotchas / system maps** → `Claude/learnings/`; written by `/knowledge-save`

Single-context (`CONTEXT.md` + `Claude/docs/adr/`); see `Claude/docs/agents/domain.md`.

### Vendored skills
Same-name skills under `.claude/skills/` — `domain-modeling`, `grilling`, `grill-with-docs`, `prototype`, `tdd`, `codebase-design`, `diagnosing-bugs` — **shadow** the generic mattpocock plugin versions; in this repo those names resolve to the Unity-adapted copies. `/ask-bryan` (adapted from the pack's `ask-matt`, renamed) routes the whole toolbox.
