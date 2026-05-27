You are an architect-level Unity engineer. Implement the system specified below. For each Open Question, state your choice and one-sentence rationale before writing code. Produce code in the order of section 9 (Deliverables).
Respond with only the resolved Open Questions + a file/folder layout, before any code. I'll green-light implementation in a follow-up.

## 1. Context

The project uses **Reflex DI**. Each scene has two scripts that drive its lifecycle:

- **SceneInstaller** — registers instances/services into the scene's DI container.
- **SceneFlow** — orchestrates initialization, loads resources, sets the parent-child relationship between this scene's container and its predecessor's, and triggers the next scene's additive load when initialization completes.

Today, scene loading order is **hard-wired inside each `SceneFlow`**, and game state is **purely event-driven** with no explicit state objects:

- `GameLoopManager` exposes `StartGame()`, `PauseGame()`, `FinishGame()` — currently invoked only from UI.
- Camera switching is performed via `EventManager.Input.SceneChangeRequest`. This event does **not** load scenes; it only switches which already-loaded scene's virtual camera is active.

There are no formal "game state" objects.

---

## 2. Goal

Replace the current implicit, hard-wired scene linkage with a **declarative, ScriptableObject-driven scene workflow system** that:

1. Lets the developer define ordered, hierarchical scene chains as data.
2. Lets the developer define **named Run Configurations** that combine a chain with optional state overrides for testing.
3. Integrates with Editor Play Mode so hitting Play from any scene "just works" — the correct prefix of scenes loads automatically.
4. Exposes a small **Editor toolbox window** to pick what to run (which configuration; full chain vs. single scene).

The system **must compose with** the existing `SceneInstaller` / `SceneFlow` Reflex DI pattern and the existing event-based runtime systems — not replace them.

---

## 3. Glossary

| Term | Meaning |
|---|---|
| **Scene Chain** | An ordered list of scenes that load additively, in order, with DI parent-child links established by `SceneFlow`. Example: `Bootstrap → World → Battle`. |
| **Run Configuration** | A ScriptableObject describing one runnable scenario: a Scene Chain reference plus zero or more State Overrides. Example: `NormalFlow`, `BattleDevFlow`. |
| **State Override** | A piece of post-init configuration applied once the chain is fully loaded and initialized. Examples: "set `GameLoopManager` to `Playing`", "switch active camera to `BattleCamera`". |
| **Scene Workflow Toolbox** | The Editor window from which the developer selects a Run Configuration and chooses load behavior (full chain vs. single scene) before pressing Play. |
| **Runner** | The runtime component that walks a Run Configuration: drives the chain's `SceneFlow` sequence and applies state overrides at the end. |

---

## 4. Functional Requirements

### FR-1 — Scene Chain Definition
- A `SceneChain` ScriptableObject contains an ordered list of scene references.
- Loading is additive: the first scene is loaded by Unity / the Editor; subsequent scenes are loaded by their predecessor's `SceneFlow`.
- A chain is reusable across multiple Run Configurations.

### FR-2 — Run Configuration Definition
- A `RunConfiguration` ScriptableObject references **one** `SceneChain` and contains **zero or more** State Overrides.
- Multiple Run Configurations may reference the same chain with different overrides.
- Example: `NormalFlow` and `BattleDevFlow` both reference `Bootstrap → World → Battle`, but `BattleDevFlow` additionally specifies `GameLoop = Playing` and `ActiveCamera = BattleCamera`.

### FR-3 — State Override Model
- A formal, extensible State Override type system (e.g. abstract `StateOverride` ScriptableObject base, or `[SerializeReference]` polymorphism — to be decided).
- Initial concrete overrides:
  - **`GameLoopStateOverride`** — invokes `GameLoopManager.Start()` / `.Pause()` / `.Finish()` based on a configured target.
  - **`ActiveCameraOverride`** — fires `EventManager.Input.SceneChangeRequest` with the configured camera target.
- Overrides are applied **after** all scenes in the chain have completed their `SceneFlow` initialization.
- The system must remain **open for new override types** without modifying the runner (open/closed).

### FR-4 — Editor Play-Mode Integration
On entering Play mode (or via the toolbox Play button), the system must:
1. Identify the **currently open scene** (the one the developer was editing).
2. Resolve a Run Configuration whose chain contains that scene. Selection comes from the toolbox; if "Auto" is selected and the scene appears in multiple chains, fall back to a default policy (see open questions).
3. Load the chain prefix **up to and including** the current scene, in order, additively, respecting `SceneFlow` initialization between steps.
4. Apply state overrides once initialization completes.

### FR-5 — Editor Toolbox Window
The toolbox is an `EditorWindow` exposing:
- A picker for the Run Configuration to use (or **Auto** — pick by current scene).
- A toggle: **Full Chain** vs **Single Scene** load mode.
  - In **Single Scene** mode, only the currently open scene loads (no predecessors). Used for fast iteration.
- A **Play** button that enters Play mode under the chosen settings.
- *(Nice-to-have)* Persisted last selection per developer (EditorPrefs).
- *(Nice-to-have)* Validation summary: warn if the current scene is not in any chain, or referenced scenes are missing from build settings.

### FR-6 — Compatibility with existing architecture
- The runner must **drive** `SceneInstaller` / `SceneFlow`, not bypass them.
- DI container parent-child relationships established by `SceneFlow` must remain intact.
- State Overrides must invoke **existing systems** (`GameLoopManager`, `EventManager.Input.SceneChangeRequest`) rather than introducing parallel state machines.

---

## 5. Non-Functional Requirements

- **Authoring ergonomics.** Defining a new chain or run configuration is data-only — no code, no scene file edits.
- **Editor / runtime separation.** Toolbox code and current-scene resolution live under `Editor/`. The runner and override types compile into builds.
- **Determinism.** Same Run Configuration + same starting scene → same load sequence and same applied state, every time.
- **Failure visibility.** Missing scene → toolbox surfaces the error before entering Play. Missing override target at runtime → clear log message, not a silent no-op.
- **No hidden coupling.** New override types should not require touching `RunConfiguration` or the runner.

---

## 6. Use Cases

**UC-1 — "Just hit Play on Battle, normal flow"**
Developer has `Battle.unity` open. Toolbox set to `NormalFlow`, mode = Full Chain. Hits Play. `Bootstrap` loads → initializes → loads `World` additively → initializes → loads `Battle` additively → initializes. No state overrides applied. Game sits in its normal pre-gameplay state.

**UC-2 — "Battle dev iteration"**
Same as UC-1 but toolbox set to `BattleDevFlow`. After Battle finishes initializing, the runner invokes `GameLoopManager.Start()` and fires `SceneChangeRequest(BattleCamera)`. Developer drops straight into active gameplay framed by the battle camera.

**UC-3 — "Single-scene fast iteration"**
Developer has `Battle.unity` open. Toolbox mode = Single Scene. Hits Play. Only `Battle` loads (predecessors skipped). State Overrides apply if compatible (see open questions about DI parenting).

**UC-4 — "Auto-resolve from current scene"**
Developer has `World.unity` open. Toolbox set to **Auto**. Hits Play. System finds `World` in `NormalFlow`'s chain, loads `Bootstrap → World`.

---

## 7. Out of Scope (v1)

- Branching chains / DAGs. Chains are linear lists.
- Conditional overrides ("apply only if X"). v1 applies all overrides unconditionally.
- Runtime UI for chain selection. This is editor-time tooling, not a user-facing feature.
- Replacing the event-based architecture of `GameLoopManager` or camera switching.
- Save/load or persistence of in-progress game state across runs.

---

## 8. Open Questions

These are decisions the implementer should make explicit and justify:

1. **Scene reference type.** `SceneAsset` (Editor-only references, validated at edit time) vs. addressables vs. build-index ints. Recommend `SceneAsset` in editor + cached path/index for runtime. Also read RuntimeConstants file, might help to answer.
2. **Multi-chain disambiguation.** When a scene appears in more than one chain under "Auto" mode — first match? Last-used? Prompt the user? A `[Default]` flag on a `RunConfiguration`?
3. **Single-scene mode + DI parents.** When predecessors are skipped, what container does the loaded scene parent under? A synthetic root container? A "stub init" path? Should certain overrides be disabled in this mode?
4. **State Override polymorphism.** `[SerializeReference]` interface list (more flexible inspector, single asset) vs. ScriptableObject sub-assets (more explicit, separately reusable). Pick one and justify.
5. **Where the runner lives.** Component on a Bootstrap-scene GameObject? Editor-injected GameObject created on Play? Standalone object marked `DontDestroyOnLoad`? How does it learn which Run Configuration to execute on Play?
6. **Editor → Play handoff mechanism.** EditorPrefs? A scratch ScriptableObject? `SessionState`? The chosen Run Configuration must survive the domain reload that Play mode triggers.

---

## 9. Expected Deliverables

1. **Runtime types**
   - `SceneChain` (SO)
   - `RunConfiguration` (SO)
   - `StateOverride` base + `GameLoopStateOverride` + `ActiveCameraOverride`
   - `Runner` (drives `SceneFlow` walk + applies overrides post-init)
2. **Editor types**
   - `SceneWorkflowToolbox : EditorWindow`
   - Current-scene → Run Configuration resolver
   - Editor → Play handoff mechanism (per open question #6)
3. **Documentation**
   - Short README on: defining a chain, defining a run configuration, adding a new override type, troubleshooting.
4. **Example assets** (optional but recommended)
   - One `SceneChain` for `Bootstrap → World → Battle`
   - `NormalFlow` and `BattleDevFlow` `RunConfiguration` assets demonstrating both empty and populated override sets.

---

## 10. Acceptance Criteria

- [ ] A new chain can be authored without writing code.
- [ ] A new Run Configuration can be authored without writing code.
- [ ] A new state override **type** can be added with a single new class implementing the override contract — no edits to `Runner` or `RunConfiguration`.
- [ ] Pressing Play with `Battle.unity` open and `NormalFlow` selected produces UC-1's behavior exactly.
- [ ] Pressing Play with `Battle.unity` open and `BattleDevFlow` selected produces UC-2's behavior exactly.
- [ ] Existing `SceneInstaller` / `SceneFlow` files require no architectural rewrites — at most additive hooks the runner can call.
- [ ] Toolbox flags missing scenes / unresolved current scene before Play is entered.