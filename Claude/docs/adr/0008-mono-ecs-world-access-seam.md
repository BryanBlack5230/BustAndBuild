# Mono↔ECS classes receive the EntityManager via a flow-driven `IWorldInitializable`, not a Reflex world binding

## Context

Battle-scoped Mono classes that talk to ECS (`PlacementController`, `BeaconEcsBridge`,
`BeaconCoreController`, and the `BeaconCoreHintView`) need an `EntityManager` to create queries and
read/write entities. They were grabbing `World.DefaultGameObjectInjectionWorld.EntityManager` **in
their constructors**. That hid a process-global dependency inside Reflex-constructed objects and
forced every integration test to swap `World.DefaultGameObjectInjectionWorld` to a throwaway world in
SetUp and restore it in TearDown — mutating a global the rest of the engine reads, ordering-sensitive,
leaked into every test's boilerplate.

The project already had an informal answer to "Reflex ctors run before the ECS world content is
ready": an ad-hoc **no-arg** `Initialize()` method that the scene `*Flow` calls in `Start()` (e.g.
`ScrollController.Initialize()` in `WorldFlow`, `BlobContainer.Initialize()` in `BootstrapFlow`). This
ADR formalises that convention for the Mono↔ECS world-access case and decides where the world lives
relative to the DI container.

This project uses Unity's **default** ECS bootstrap — no `ICustomBootstrap`, no runtime `new World(...)`.
There is exactly **one** World; the WorldECS + BattleECS subscenes stream as *sections into it*. So
DI scope ≠ ECS world, and "which world?" has a single answer everywhere.

The same world-grab appears in every Mono service that touches ECS — grab/throw, cursor, camera,
day/night, and the bootstrap bridges — some in ctors, some as lazy per-frame re-grabs. The decision is
therefore applied **project-wide** for consistency, not just to the Beacon classes where it surfaced.

## Decision

**The World stays out of the Reflex container, and no runtime service grabs
`World.DefaultGameObjectInjectionWorld` itself.** A marker interface
`IWorldInitializable { void Initialize(EntityManager em); }` (in `Infrastructure/GameLoop`, beside
`IGameListener`) is implemented by **every** world-touching Mono service in all three scopes. Each
scene flow is the **single tap** for its scope: it reads the default world's `EntityManager` once and
hands it to its participants. Constructors do only world-free work; everything that needs the
`EntityManager` (queries, subscriptions whose handlers touch `em`) moves into `Initialize`. The param
is `EntityManager`, not `World`, because that is all most consumers use — a service that needs the
`World` (e.g. `DotsGameLoopBridge`, `DaylightEcsBridge`) recovers it with `em.World`.

Delivery differs by scope, forced by a Reflex quirk (see Considered Options):
- **Battle** uses the contract + a loop: participants register `typeof(IWorldInitializable)` and
  `BattleGroundSceneFlow` injects its **own** `IEnumerable<IWorldInitializable>` and calls
  `Initialize(em)` on each in `Start()` **before** `AddListeners`. New Battle bridges self-enroll with
  zero flow edits.
- **World / Bootstrap** use explicit concrete-typed `Initialize(em)` calls from `WorldFlow` /
  `BootstrapFlow` — **no contract**. Bootstrap's calls are *ordered* (loop group + throw tracker before
  `await BeginLoading`, config bake after), which a single loop can't express. `BlobContainer` caches
  `em` at `Initialize` so its `Rebake()` button re-bakes without re-tapping the world.

## Considered Options

- *Constructor-inject `World`/`EntityManager` via a Reflex `World` binding.* Rejected: captures the
  reference at `InstallBindings` time (before subscene content is ready) and drags the ECS world into
  the Reflex container, conflating DI scope with the single ECS world.
- *Do nothing — keep the default-world swap as the accepted test idiom and document it once.* Rejected:
  the swap tax (global-state mutation + ordering-sensitive restore) compounds with every new Mono↔ECS
  bridge, and the hidden global dep stays in production.
- *Keep the existing ad-hoc named `Initialize()` per class (status quo).* Rejected: a self-enrolling
  `IEnumerable<IWorldInitializable>` wires future bridges with no flow change, vs hand-adding a named
  call per class. Every world-touching service across all scopes was migrated onto the interface; the
  ad-hoc no-arg `Initialize()` now survives only for **world-free** deferred setup (`ScrollController`'s
  input registration).
- *Use the contract + enumerable loop in every scope (uniform mechanism).* Rejected: Reflex's
  `ContainerBuilder.Build()` copies parent resolvers into the child, so `All<IWorldInitializable>()`
  resolved at the Battle scope is **cumulative** — it would include any World/Bootstrap services that
  registered the contract and `Initialize` them a second time (re-subscribing `DaylightEcsBridge`,
  leaking `ThrowDebugTracker`'s query). So the contract+loop is confined to the **deepest scope
  (Battle)**; World/Bootstrap drive `Initialize(em)` by explicit concrete-typed calls instead.

## Consequences

- **One instance, many contracts.** Each participant keeps a single `AddSingleton` call listing every
  contract (`…, typeof(IGameListener), typeof(IWorldInitializable), typeof(IDisposable)`). Two
  registration mechanics, identical consumption: plain-C# DI services register by type-contract; scene
  MonoBehaviour views (`BeaconCoreHintView`) register as an **instance** (the `BattleSceneData`
  pattern), null-guarded since the scene may not have one wired.
- **No production behaviour change.** Only query-creation *timing* shifts (inject-time → `Start`);
  nothing reads `em`/queries before the first frame. "World ready" here means subscene entity content /
  `PhysicsWorldSingleton` streamed — the World object itself exists `BeforeSceneLoad`.
- **Failure mode:** if a participant isn't registered under the contract, or the flow loop is skipped,
  the symptom is a first-frame `NRE` from a `default` `EntityQuery`. Check the registration contracts
  and that the flow loop ran.
- **Tests** pass a throwaway world via `Initialize(_world.EntityManager)` instead of swapping the
  process-global default — matching the AI suite's bar (local world, no global touch).
- **The interface doubles as the flow's post-construction init seam.** `BattleCameraMovement`
  implements `IWorldInitializable` but ignores `em` — it touches no ECS, and uses the hook only to
  defer its EventBus wiring out of the Reflex ctor. Accepted so the Battle flow runs entirely off the
  enumerable (no bespoke calls); the cost is that "implements `IWorldInitializable`" no longer strictly
  implies "reads the world."
- **Teardown liveness probes stay exempt.** `BeaconCoreHintView.OnDestroy` still reads
  `World.DefaultGameObjectInjectionWorld` to confirm the world is alive before disposing its query —
  that is an existence check at teardown (Unity nulls the static on world disposal), not an
  access-for-work tap, so it is intentionally outside this rule.
