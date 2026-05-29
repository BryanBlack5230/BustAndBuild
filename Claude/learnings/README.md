# Claude Learnings Knowledge Base

Project-specific discoveries saved across conversations. Use these as fast reference to avoid re-deriving known facts from scratch. Load only the file relevant to the current task.

## Index

### Architecture (project-wide)
- [Project Overview](project-overview.md) — game concept, folder layout, stack, naming conventions, style quirks
- [DI Architecture](di-architecture.md) — Reflex installers, container parent chain, listener auto-collection, registration order
- [Game Loop & Listeners](game-loop-listeners.md) — `IGameListener` interfaces, `GameLoopManager` state machine, `DotsGameLoopBridge`
- [Events & Services](events-and-services.md) — `EventManager` hub, `Log`, `LoadingService`, `RuntimeConstants`, config pipeline
- [Scene Flow System](scene-flow-system.md) — `ISceneFlow`, `RunConfiguration`, `SceneChain`, editor toolbox, state overrides

### ECS / Battle
- [ECS Architecture](ecs-architecture.md) — system groups, pipeline order, enableable components, damage/death, `BattleCoordinator`, `TargetScorerJob`
- [Steering & AI](steering-and-ai.md) — brain → steering → mover pipeline, 8-dir context map, target profiles, attack cooldown pattern

### Input / Camera / Throw
- [Input System](input-system.md) — grab/drag/release flow, `ReleaseCoordinator`, `ThrowTrajectoryPredictor`, camera drag, screen-edge gotchas

### C# / Unity Patterns
- [Unity C# Patterns](unity-csharp-patterns.md) — fake-null with #nullable, ScriptableObject settings pattern, required SerializeField guard

### Editor Tooling
- [Custom Inspector Patterns](custom-inspector-patterns.md) — `SceneChain` inspector, `FindPropertyRelative`, scene-open guards
- [Editor Window Patterns](editor-window-patterns.md) — IMGUI button highlights, `PingObject`, OVDF limitation, SceneWorkflowHandoff null guard
- [ScriptableObject Patterns](scriptable-object-patterns.md) — editor-only buttons, `[ReadOnly]` vs `[HideInInspector]`, `SetDefault` cross-asset pattern
