# Claude Learnings Knowledge Base

Project-specific discoveries saved across conversations. Use these as fast reference to avoid re-deriving known facts from scratch. Load only the file relevant to the current task.

## Index

### Architecture (project-wide)
- [Project Overview](project-overview.md) — game concept, folder layout, stack, naming conventions, style quirks
- [Design Heuristics](design-heuristics.md) — polymorphism test: no abstraction extraction without a named second consumer
- [DI Architecture](di-architecture.md) — Reflex installers, container parent chain, listener auto-collection, registration order
- [Game Loop & Listeners](game-loop-listeners.md) — `IGameListener` interfaces, `GameLoopManager` state machine, `DotsGameLoopBridge`
- [Events & Services](events-and-services.md) — `EventBus` + `CommandDispatcher` (decision guide), `Log`, `LoadingService`, `RuntimeConstants`, config pipeline
- [Config / Balance System](config-system.md) — `ConfigHub` (SO profiles + flat singletons), `BlobContainer` baker, enum-slot baking, Rebake, what's still JSON, deferred phases
- [Logging System](logging-system.md) — `Log`/`TagLog`, `[HideInCallstack]` + strip toggle, Console Pro `CPIGNORE`, `[CallerFilePath]` class prefix, color-key overload resolution gotcha
- [Scene Flow System](scene-flow-system.md) — `ISceneFlow`, `RunConfiguration`, `SceneChain`, editor toolbox, state overrides
- [Currency & Saves](currency-and-saves.md) — `Wallet`/`CurrencyType`, `WorldSaveService` hydration, `ISaveSystem` dummy seam, save timing, `GameWorld` namespace gotcha

### Testing
- [Testing Methodology](testing-methodology.md) — test-layer assignment, ECS systems as the unit-test sweet spot (World/`Update`/assert pattern + enableable & domain-reload arrange traps), TDD red-green loop, technique selection, PRNG strategies, naming; **ECS test harness recipes** (LookupSource for `ComponentLookup`, fabricating `DistanceHit`s to drive `ICollector` folds, manual `CollisionWorld` build for filter tests, deep-gate one-tick arrange, `InternalsVisibleTo`); now holds the 26-test broadphase-refactor suite

### ECS / Battle
- [ECS Architecture](ecs-architecture.md) — the map: system groups, battle pipeline order, enableable components, `BattleCoordinator`, spawning, walls, namespaces
- [ECS Combat & Collisions](ecs-combat-and-collisions.md) — damage/death pipeline (HealthAspect, IsInvulnerable intent), bounce/landing systems, UnitMover velocity override, hit-feedback blocks, throw settings
- [ECS Patterns](ecs-patterns.md) — reusable DOTS patterns: enableable queries, aspects, ECB gotchas, snapshot jobs, baking gotchas, ECS↔managed bridging
- [Unity Physics Gotchas](unity-physics-gotchas.md) — AABB local-vs-world, post-physics system placement, gravity factor baking, hover bodies, collision matrix hex, pickup mid-air-settle + upward-only bob, drop-spawn ground anchoring, battle ground/CollisionWorld topology
- [Steering & AI](steering-and-ai.md) — brain → steering → mover pipeline, 8-dir context map, target profiles + `TargetScorerJob`, attack cooldown pattern

### Input / Camera / Throw
- [Input System](input-system.md) — grab/drag/release flow, `ReleaseCoordinator`, `ThrowTrajectoryPredictor`, camera drag, screen-edge gotchas

### C# / Unity Patterns
- [Unity C# Patterns](unity-csharp-patterns.md) — fake-null with #nullable, ScriptableObject settings pattern, required SerializeField guard
- [Vortex Framework Notes](vortex-framework-notes.md) — what was ported (`[Formula]`, `DateTimeTimer`, `UiPool`) and harvested ideas: audio channels, queued-action timer, ExtensibleEnum, preset→model + notify-once discipline, def-GUID save-reference conventions
- [UGFW Harvest](ugfw-harvest.md) — what was ported (`Timer`, `TimeFormatter`, `NumberFormatter`, `PriorityQueue`, `MissingScriptsFinder`) and harvested ideas: UI channel/stack architecture, pending-transaction pattern, registry-keyed pooled spawners, GUID-identity definition SOs, PlayerPrefs save stopgap
- [Play Mode & Hot Reload](play-mode-and-hot-reload.md) — domain/scene reload OFF, static-state reset pattern, Hot Reload limitations (DOTS, Reflex, ScriptableObjects)

### Editor Tooling
- [Custom Inspector Patterns](custom-inspector-patterns.md) — `SceneChain` inspector, `FindPropertyRelative`, scene-open guards; OVDF authoring (naming, tabs, hex-color locale fix, additive-only, empty-tab culling, InlineEditor propagation)
- [Editor Window Patterns](editor-window-patterns.md) — IMGUI button highlights, `PingObject`, OVDF limitation, SceneWorkflowHandoff null guard
- [ScriptableObject Patterns](scriptable-object-patterns.md) — editor-only buttons, `[ReadOnly]` vs `[HideInInspector]`, `SetDefault` cross-asset pattern

### Workflow / Version Control
- [Merge Conflicts in Unity Files](merge-conflicts.md) — never hand-merge `.unity`/`.prefab` (graph, not text); defer to UnityYAMLMerge / the user; no Smart Merge configured yet
