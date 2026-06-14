# Testing Methodology

Distilled test-design methodology for this project. Use it when designing or writing tests so the suite stays maintainable instead of becoming a refactor anchor. Cross-cutting like [[design-heuristics]]; the SUTs it talks about live in [[ecs-architecture]], [[ecs-combat-and-collisions]], [[steering-and-ai]], and [[events-and-services]].

## Current state (read first)
There are **no tests, no test asmdef, and no MCP test runner** in this project today. `com.unity.test-framework` (1.6.0) and Rider are installed, but nothing uses them. So before any of the below is actionable:
- Create a `Tests` asmdef referencing `Runtime` (+ `Unity.Entities`, `Unity.Entities.Hybrid`, `nunit.framework`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`) with `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`. Editor-only tests go in a second asmdef referencing `Editor`.
- `com.nowsprinting.test-helper` / `.ui` are **not** installed — the screenshot/statistical-sampling helpers the upstream guide assumes don't exist here. Stick to plain NUnit + an Entities test `World` until that changes.
- No JetBrains MCP (`run_unity_tests`) is configured — run via the Unity Test Runner window or `-runTests` on the CLI.

## Layer assignment — map each SUT to the cheapest layer that can witness it
1. **Editor tests** — `Assets/!_Game/Editor/` code (ConfigGenerator, SceneChainEditor, Formula drawers) and asset validation: `*Config` SO sanity, SceneCollection/RunConfiguration consistency, `Config.json` shape. `[Formula]` parsing (`FormulaParser`/`FormulaEvaluator`) is pure → cheapest, highest-value first tests.
2. **Unit tests (Play Mode)** — runtime logic initiated by a direct call. **This is the sweet spot for this project** (see next section): ECS jobs/systems, `MathHelper`, `Wallet`/`CurrencyType` math, `NumberFormatter`/`TimeFormatter`. Prioritize the *least integrated* targets and cover them thoroughly; keep density low where a system collaborates with many others.
3. **Integration tests** — a GameObject wired via `AddComponent<T>()`, a prefab, or a scene. Reflex installer wiring (the Project→Bootstrap→World→Battle chain, [[di-architecture]]) and `SceneChain`/`RunConfiguration` flows ([[scene-flow-system]]) live here. Higher cost — design these *before* dropping to visual/manual, but keep them sparse.
4. **Visual verification** — screenshot + image analysis (trajectory arc, daylight cycle, UI layout). Needs `test-helper.ui`; treat as future.
5. **Manual** — only for what no assertion or image can judge: game feel, animation polish, audio balance. Never duplicate something an integration test already covers.

> Never split one SUT across Edit Mode and Play Mode — the two runners can't run together, so you lose "run all" in one shot. Runtime logic = Play Mode, always.

## ECS systems are the high-leverage target
ECS systems are deterministic transforms over component data — near-pure functions with externally observable state. That makes them the most testable code in the project and where unit tests pay off most. Pattern:

```csharp
[Test]
public void ApplyDamageSystem_LethalDamage_DropsHealthToZero()   // Method_Condition_Expected
{
    using var world = new World("Test");
    var em = world.EntityManager;
    var e  = em.CreateEntity();
    em.AddComponentData(e, new Health { Value = 10 });
    // + the damage-intent component for this pipeline (see [[ecs-combat-and-collisions]]),
    //   enabled so the query matches it:
    // em.SetComponentEnabled<PendingDamage>(e, true);

    world.GetOrCreateSystem<ApplyDamageSystem>().Update(world.Unmanaged); // one tick
    em.CompleteAllTrackedJobs();                                          // drain jobs before asserting

    Assert.That(em.GetComponentData<Health>(e).Value, Is.EqualTo(0));
}
```

Two project-specific arrange traps:
- **Enableable components gate the query.** A system won't touch an entity whose query component is *disabled* — you must `SetComponentEnabled<T>(e, true)` in arrange (or the system must use `IgnoreComponentEnabledState`). Same gotcha that bites at runtime, see [[ecs-patterns]].
- **Domain reload is OFF** ([[play-mode-and-hot-reload]]), so static state survives between Play Mode tests. Reset any static fields in `[SetUp]`/`[TearDown]`, and prefer a fresh `World` per test over reusing the default one.

## Choosing techniques (prefer specification-based over structural)
Structural tests couple to implementation and die on every refactor; write one only as temporary scaffolding, then delete it once a spec test covers the same behavior.
- **Equivalence partitioning + boundary values** — one representative per partition; consolidate partition members and edges into a parameterized test (below). Skip boundary testing when the spec doesn't differentiate near the edge.
- **State transition (0-switch)** — for FSMs: `GameLoopManager`'s state machine ([[game-loop-listeners]]), enemy escape states (`HasLeftBase`/`Escaped`/`Scared`, [[steering-and-ai]]). One direct A→B transition per case.
- **Decision table** — when several conditions combine (e.g. target scoring in `TargetScorerJob`).
- **Error guessing** — derive from game failure patterns this codebase actually exposes: collision tunneling, simultaneous/mashed input, input during scene transition or loading, PRNG bias, numeric overflow.
- **Reproduction test** (bug fixes) — one case that triggers the bug; must fail before the fix, pass after. **Cover-and-modify** (refactors) — write regression coverage *before* changing the implementation.

### Randomness (PRNG-dependent SUTs)
Pick by what the spec actually pins down: **seed/stub** when output→behavior is a fixed mapping (assert exact); **range/bounds** when only the output range is specified (assert with `Range`/`Within`, add `[Repeat]` so a lucky run doesn't mask flakiness); **statistical** for distribution shape (sample in a loop, assert on mean/variance); **characteristic** for procedural content (assert invariants like reachability, not exact output). Systems that seed `Unity.Mathematics.Random` from a component are easy to make deterministic — inject the seed via the component in arrange.

## Naming & parameterization
- Editor / Unit: `MethodName_Condition_ExpectedResult` (target is a method).
- Integration / Visual: `Condition_ExpectedResult` (target is a wiring/render, not one method — no method name).
- Parameterized: the `<Condition>` segment is the **equivalence-partition name**, not an enumeration of arg values. Merge only same-partition / same-expected cases; never put `if`/`switch` in a parameterized body, and don't parameterize the expected value.

## Test doubles & testability
xUTP vocabulary, noted only where it matters in output: **stub** (canned responses — arrange concern), **fake** (working lightweight impl — arrange concern), **spy** (records calls — note it in the verification, e.g. "uses spy: IFoo"). Decide whether to test a dependency's error path by origin: external library/framework → test it with a stub; the game's own code → trust it; unsure → ask.

Testability is mostly already good here: Reflex DI ([[di-architecture]]) makes dependencies injectable, the `ISaveSystem`→`DummySaveSystem` seam ([[currency-and-saves]]) is the model to copy, and ECS component state is observable by construction. Flag a design (don't just write an awkward test) when you hit: hidden state with no external read, dependencies that can't be injected (static/global coupling, `new` in a constructor), or test-case count growing faster than O(n) in the conditions.

*(Adapted from the `test-designing-guide` skill in nowsprinting/unity-coding-skills (Koji Hasegawa, Unlicense), reduced to the tool-agnostic methodology and re-framed for this project's ECS/Reflex stack. The skill's MCP/`test-helper`-dependent workflow — `run-tests`, `edit-scene`, the test-first agents — was not adopted; revisit it if a real test assembly + JetBrains MCP get set up.)*
