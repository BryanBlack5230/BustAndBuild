# CLAUDE.md

When reporting information to me, be extremely concise and sacrifice grammar for the sake of concision.
This is a game project by BarkingBird studio. For more information, load the [Project.md](Claude/Project.md) file.

## Folder Structure
Descriptions say **what kind of file belongs** in each folder (a placement guide), not a snapshot of current contents.
```
Bust and Build/
├── CLAUDE.md            ← always loaded: router, conventions, knowledge index (you are here)
├── CONTEXT.md           ← glossary: ubiquitous domain language (term meanings only — no how-it-works)
├── `Claude/`            ← Claude's working dir — knowledge & planning, not shipped in builds
│   ├── Project.md       ← deep architecture reference (orientation; loaded on demand)
│   ├── `learnings/`     ← how-it-works KB: verified discoveries, gotchas, system maps (see "Knowledge Routing")
│   ├── `docs/`
│   │   ├── `adr/`       ← Architecture Decision Records — choices with real trade-offs (`0001`+)
│   │   └── `agents/`    ← agent configs: issue-tracker, triage-labels, domain conventions
│   └── Input.md · TaskBreakdown.md · *Task.md   ← transient planning/scratch notes (not durable knowledge)
│
├── `Assets/`            ← Unity project root (everything outside `!_Game/` is third-party packages)
│   └── `!_Game/`        ← first-party content authored in-studio
│       ├── ProjectScope.prefab   ← Reflex ProjectScope (ProjectInstaller lives here)
│       ├── Tasks.md              ← first-party task memos
│       ├── `Editor/`             ← editor-only C# (assembly `Editor`, ns `BarkingBird.Editor`): custom inspectors, menu tools, asset/scene utilities
│       │   ├── `Formulas/`       ← PropertyDrawer + resolver for `[Formula]` string fields
│       │   └── `SceneWorkflow/`  ← editor tooling for the scene-chain workflow (collection runner, toolbox window, handoff)
│       └── `Runtime/`            ← all shipped runtime C# (assembly `Runtime`)
│           ├── `Gameplay/`       ← scene/gameplay-bound code — ns `BarkingBird.Runtime.Gameplay.*`
│           │   ├── Data/         ← non-code gameplay assets: input action maps, post-processing profiles
│           │   ├── Resources/    ← runtime-loaded assets (always via `AssetService.R`): audio, cursors, materials, models, prefabs, shaders, textures
│           │   │   └── Settings/ ← ScriptableObject config/profile assets: ConfigHub `*Config`, UnitProfiles, HitFeedback, SceneCollections, SceneRunConfigurations
│           │   ├── Scenes/       ← Unity scenes (`0.Bootstrap`→`4.City`) + their ECS subscenes
│           │   └── !_Scripts/    ← all gameplay C#
│           │       ├── Components/   ← ECS data: `IComponentData`/buffer structs + their `*Authoring`+`Baker` (root files = global namespace)
│           │       │   └── AI/       ← AI-pipeline components & authoring (`...Gameplay.AI`)
│           │       ├── Systems/      ← ECS behaviour: `ISystem`/`SystemBase` (root files = global namespace)
│           │       │   └── AI/       ← AI pipeline: brain → steering → targeting → mover (`...Gameplay.AI`)
│           │       └── _MonoWorld/   ← non-ECS gameplay MonoBehaviours (each subfolder = one `Gameplay.*` namespace)
│           │           ├── Camera/        ← battle camera: drag, border constraints, ECS frustum-sync bridge
│           │           ├── Cursor/        ← cursor world-projection, throw-velocity tracking, visual representation
│           │           ├── DaylightCycle/ ← day/night cycle driver + ECS bridge
│           │           ├── Input/         ← player input: raycast grab, grab/throw (`GrabAndThrow/`), release routing
│           │           ├── Scenes/        ← per-scene `*Flow` (init) + `*Installer` (DI wiring)
│           │           └── Settings/      ← gameplay settings + `StateOverride` subclasses (`StateOverrides/`)
│           └── `Infrastructure/`  ← framework services, subdomain-agnostic — ns `BarkingBird.Runtime.Infrastructure.*`
│               ├── (root files)   ← cross-cutting services tied to no subdomain: `InputManager`, `ReflexExtensions`, `StateOverride` (base), `StringExtensions`
│               ├── Commands/      ← `CommandDispatcher` + `ICommand` — imperative 1-to-1 requests
│               ├── EventBus/      ← static `EventBus` + `IEvent` — 1-to-many notifications (root namespace)
│               ├── Formulas/      ← `[Formula]` string parsing/evaluation (`FormulaParser`/`FormulaEvaluator`)
│               ├── GameLoop/      ← `GameLoopManager` + `IGameListener` interfaces
│               ├── Pooling/       ← `UiPool`/`UiPoolItem` — data-keyed UI list rows
│               ├── Save/          ← persistence seam: `ISaveSystem`, `DummySaveSystem`, `ActiveSlot`
│               ├── SceneWorkflow/ ← runtime scene-chain runner + `RunConfiguration`/`SceneChain` SOs
│               ├── Settings/      ← `ConfigHub`, `BlobContainer`, `RuntimeConstants`
│               └── Utilities/     ← shared statics: `Log`, `AssetService`, `MathHelper`, `CoreHelper`, `PhysicsUtility`
```
**Keep this tree honest:** if you add a package/tech or a new top-level area not reflected above, ask whether to record it here.

## Knowledge Routing (read BEFORE coding, not after getting stuck)
Project knowledge lives in **three sinks** + the standards skill — load the right one before non-trivial work; **don't re-derive what's already written.** (What each sink *is* and who writes it: Agent skills › Domain docs.)

| Task touches | Load |
|---|---|
| Meaning/naming of a domain term (Faction, Structure, Base, Pearl…) | `CONTEXT.md` (glossary, repo root) |
| Why a past architectural decision was made / a real trade-off | `Claude/docs/adr/` (scan the index; `0001` = knowledge architecture) |
| Coding rules / standards (always-never, hygiene, architecture patterns) | `unity-coding-standards` skill (auto-loads when writing/reviewing C#) |
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

After completing work that produced non-obvious discoveries, offer to run `/knowledge-save`. When you add/rename/remove a file in `Claude/learnings/`, update this table and `learnings/README.md` so the index doesn't drift.

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
