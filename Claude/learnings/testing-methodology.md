# Testing Methodology

Distilled test-design methodology for this project. Use it when designing or writing tests so the suite stays maintainable instead of becoming a refactor anchor. Cross-cutting like [[design-heuristics]]; the SUTs it talks about live in [[ecs-architecture]], [[ecs-combat-and-collisions]], [[steering-and-ai]], and [[events-and-services]].

## Current state (read first)
The first test batch has landed (`com.unity.test-framework` 1.6.0 + Rider installed):
- **`Tests`** asmdef (`Assets/!_Game/Tests/`, Play Mode) — refs `Runtime`, `Unity.Entities`(+`.Hybrid`), `Unity.Collections`, `Unity.Mathematics`, **`Unity.Physics`, `Unity.Transforms`**, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `overrideReferences` + `precompiledReferences:["nunit.framework.dll"]`, `autoReferenced:false`, `defineConstraints:["UNITY_INCLUDE_TESTS"]`. The ECS one-tick-harness + unit-test home. Holds the **broadphase-refactor suite** (`Tests/AI/`, 26 tests — collectors, `StructureBoundsSystem`, brain one-tick, real-`CollisionWorld` filter integration; see "ECS test harness — proven patterns" below) plus the `MathHelperTests` tracer (`GetForwardFromHeading`) at the root.
- **`Editor.Tests`** asmdef (`Assets/!_Game/Tests/Editor/`, Edit Mode) — same flags, additionally refs `Editor`. Empty, awaiting its first `[Formula]`-parser / asset-validation test.
- `com.nowsprinting.test-helper` / `.ui` are **not** installed — the screenshot/statistical-sampling helpers the upstream guide assumes don't exist here. Stick to plain NUnit + an Entities test `World` until that changes.
- No JetBrains MCP (`run_unity_tests`) is configured — run via the Unity Test Runner window or `-runTests` on the CLI.

## TDD loop (red-green)
The workflow when building test-first. The `/tdd` skill runs it in full; this is the nugget so the principle survives outside the skill.

Work in **vertical** slices, never horizontal: don't write all the tests then all the code — bulk tests verify *imagined* behavior and outrun your understanding. **Tracer-bullet** one behavior at a time — `RED:` one test for one behavior fails → `GREEN:` minimal code makes it pass → repeat, each cycle learning from the last. One test → one implementation; don't anticipate future tests. **Never refactor while RED** — reach GREEN first, then deepen modules / extract duplication with the tests holding you.

## Layer assignment — map each SUT to the cheapest layer that can witness it
1. **Editor tests** — `Assets/!_Game/Editor/` code (SceneChainEditor, Formula drawers) and asset validation: `*Config`/profile-SO sanity (enum-slot coverage, no null/duplicate types — the kind of thing `ConfigHub.OnValidate` warns on), SceneCollection/RunConfiguration consistency. `[Formula]` parsing (`FormulaParser`/`FormulaEvaluator`) is pure → cheapest, highest-value first tests. *(`ConfigGenerator`/`Config.json` are gone — no JSON-shape test target anymore.)*
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

## ECS test harness — proven patterns (from the broadphase-refactor suite)
Concrete recipes verified against `com.unity.entities@f6e02210` + `com.unity.physics@1.4.2`. These are the non-obvious bits that cost a round-trip to discover — copy them, don't re-derive.

- **`ComponentLookup<T>` in a test → route through a do-nothing `SystemBase`.** `EntityManager.GetComponentLookup<T>` is **internal** to Unity.Entities, so the test assembly can't call it. Make a `partial class LookupSource : SystemBase { protected override void OnUpdate(){} public ComponentLookup<T> GetLookup<T>() where T : unmanaged, IComponentData => GetComponentLookup<T>(true); }`, get it via `world.GetOrCreateSystemManaged<LookupSource>()`, and hand its lookups to the SUT. Create the entities **before** building the collector so the lookups capture them.
- **Unit-test an `ICollector<DistanceHit>` fold directly — no physics query.** `DistanceHit` is fully constructible (`new DistanceHit { Entity=e, Fraction=dist, Position=p }`; `Distance => Fraction`, the *absolute* surface distance). Call `collector.AddHit(hit)` on a local struct (mutates in place) and assert its public fields (`BestEntity`/`Danger[k]`/…). This isolates the *decision* (TargetScoringCollector) / *danger fold* (ObstacleDangerCollector) from the broadphase. Steering danger is flagged for in-play re-tuning → assert **invariants** (smear direction, front/back range asymmetry, max-not-sum, monotonic-by-distance), never magnitudes.
- **Reach `internal` SUTs via `[assembly: InternalsVisibleTo("Tests")]`** in `Runtime/AssemblyInfo.cs` (the collectors are internal). `Tests.asmdef` must also ref `Unity.Physics` + `Unity.Transforms`.
- **Integration filter test → build a `CollisionWorld` by hand.** `var pw = new PhysicsWorld(nStatic,0,0);` → fill `pw.StaticBodies[i] = new RigidBody { Collider, WorldFromBody = new RigidTransform(rot,pos), Entity, Scale = 1f }` (**`Scale=1f` or the AABB is degenerate**) → `pw.CollisionWorld.BuildBroadphase(ref pw, 1f/60f, gravity, buildStaticTree:true)` → `pw.CollisionWorld.CalculateDistance(new PointDistanceInput{Position,MaxDistance,Filter}, ref collector)`. The collision **filter lives in the collider**: `BoxCollider.Create(geom, new CollisionFilter{ BelongsTo=layerBit, CollidesWith=~0u })`. You own the collider blobs → dispose them yourself; `CollisionWorld.Dispose()` only frees `Clone()` deep-copies, so there's no double-free but also no auto-cleanup. This is the only layer that catches a wrong filter mask (e.g. units on **Grabbable(8)**, not Unit(6) — see [[steering-and-ai]]/[[unity-physics-gotchas]]).
- **One-tick a system with deep gates (the brain).** Enableable gates read via lookups on `entity` (`UnableToAct`, `AttackCooldownExpirationTimestamp`) must be **present but disabled** — `AddComponent<T>` then `SetComponentEnabled<T>(e,false)` (AddComponent defaults to *enabled*). `[WithPresent(typeof(SteeringEnabled))]` → just add it. Create every `RequireForUpdate` singleton first (`FactionBases{IsInitialized=true}`, `BattleCoordinator`). Drive with `world.GetOrCreateSystem<T>().Update(world.Unmanaged)` then `em.CompleteAllTrackedJobs()` before asserting.
- **World-AABB from a collider** (`StructureBoundsSystem` test): place the box via a `LocalToWorld` translation and assert the cached AABB centres on the **translation**, not the origin — that one assertion distinguishes the world-vs-local bug ([[unity-physics-gotchas]] "AABB local-vs-world").

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

*(Adapted from the `test-designing-guide` skill in nowsprinting/unity-coding-skills (Koji Hasegawa, Unlicense), reduced to the tool-agnostic methodology and re-framed for this project's ECS/Reflex stack. The skill's MCP/`test-helper`-dependent workflow — `run-tests`, `edit-scene`, the test-first agents — was not adopted; revisit it if JetBrains MCP gets set up. The **TDD loop** section is distilled from the `tdd` skill in the mattpocock skill pack — the red-green / vertical-slice nugget that `/tdd` owns in full.)*
