---
name: codebase-design
description: Shared vocabulary for designing deep modules. Use when the user wants to design or improve a module's interface, find deepening opportunities, decide where a seam goes, make code more testable or AI-navigable, or when another skill needs the deep-module vocabulary.
---

# Codebase Design

Design **deep modules**: a lot of behaviour behind a small interface, placed at a clean seam,
testable through that interface. Use this language and these principles wherever code is being
designed or restructured. The aim is leverage for callers, locality for maintainers, and
testability for everyone.

## In this project (Unity / DOTS)

This vocabulary fits the two halves of this codebase differently — apply it knowingly:

- **Infrastructure / MonoWorld / Reflex services — fits cleanly.** A small interface over a
  swappable implementation is exactly a deep module. The canonical example is `ISaveSystem` →
  `DummySaveSystem` (see `learnings/currency-and-saves.md`): callers depend on the interface, the
  real and dummy adapters sit at the seam, tests run through it. Reflex DI is the "accept
  dependencies, don't create them" principle institutionalised — a constructor that takes its
  collaborators *is* a seam.
- **ECS / DOTS battle simulation — maps only loosely.** DOTS deliberately splits behaviour
  (systems) from data (components), so a "module" isn't a class with a small method surface. A
  system's effective **interface** is the set of components it reads/writes (its query) plus where
  it runs in the group order; **depth** is a lot of simulation behaviour behind a small, stable set
  of components; a **seam** is usually a system-group ordering boundary or a singleton/blob config
  input, not an injected port. Don't force `adapter`/port language onto systems — reach for it on
  the service/Mono side, and think components + queries + ordering on the ECS side.

## Glossary

Use these terms exactly — don't substitute "component," "service," "API," or "boundary." Consistent
language is the whole point.

**Module** — anything with an interface and an implementation. Deliberately scale-agnostic: a
function, class, package, or tier-spanning slice. _Avoid_: unit, component, service.

**Interface** — everything a caller must know to use the module correctly: the type signature, but
also invariants, ordering constraints, error modes, required configuration, and performance
characteristics. _Avoid_: API, signature (too narrow — they refer only to the type-level surface).

**Implementation** — what's inside a module, its body of code. Distinct from **Adapter**: a thing
can be a small adapter with a large implementation (a Postgres repo) or a large adapter with a small
implementation (an in-memory fake). Reach for "adapter" when the seam is the topic; "implementation"
otherwise.

**Depth** — leverage at the interface: the amount of behaviour a caller (or test) can exercise per
unit of interface they have to learn. A module is **deep** when a large amount of behaviour sits
behind a small interface, **shallow** when the interface is nearly as complex as the implementation.

**Seam** _(Michael Feathers)_ — a place where you can alter behaviour without editing in that place;
the *location* at which a module's interface lives. Where to put the seam is its own design
decision, distinct from what goes behind it. _Avoid_: boundary (overloaded with DDD's bounded
context).

**Adapter** — a concrete thing that satisfies an interface at a seam. Describes *role* (what slot it
fills), not substance (what's inside).

**Leverage** — what callers get from depth: more capability per unit of interface they learn. One
implementation pays back across N call sites and M tests.

**Locality** — what maintainers get from depth: change, bugs, knowledge, and verification
concentrate in one place rather than spreading across callers. Fix once, fixed everywhere.

## Deep vs shallow

**Deep module** = small interface + lots of implementation:

```
┌─────────────────────┐
│   Small Interface   │  ← Few methods, simple params
├─────────────────────┤
│                     │
│  Deep Implementation│  ← Complex logic hidden
│                     │
└─────────────────────┘
```

**Shallow module** = large interface + little implementation (avoid):

```
┌─────────────────────────────────┐
│       Large Interface           │  ← Many methods, complex params
├─────────────────────────────────┤
│  Thin Implementation            │  ← Just passes through
└─────────────────────────────────┘
```

When designing an interface, ask:

- Can I reduce the number of methods?
- Can I simplify the parameters?
- Can I hide more complexity inside?

## Principles

- **Depth is a property of the interface, not the implementation.** A deep module can be internally
  composed of small, mockable, swappable parts — they just aren't part of the interface. A module
  can have **internal seams** (private to its implementation, used by its own tests) as well as the
  **external seam** at its interface.
- **The deletion test.** Imagine deleting the module. If complexity vanishes, it was a pass-through.
  If complexity reappears across N callers, it was earning its keep.
- **The interface is the test surface.** Callers and tests cross the same seam. If you want to test
  *past* the interface, the module is probably the wrong shape.
- **One adapter means a hypothetical seam. Two adapters means a real one.** Don't introduce a seam
  unless something actually varies across it. (See also `learnings/design-heuristics.md` — the
  project's "name a second consumer" rule for the same call.)

## Designing for testability

Good interfaces make testing natural:

1. **Accept dependencies, don't create them.** Reflex makes this the default — inject, don't `new`.

   ```csharp
   // Testable — collaborators injected (Reflex resolves them)
   public ReleaseCoordinator(IOverlapResolver resolver, ITunnelTeleporter teleporter) { … }

   // Hard to test — collaborator constructed inside
   public ReleaseCoordinator() { _resolver = new OverlapResolver(); }
   ```

2. **Return results, don't produce side effects.**

   ```csharp
   // Testable — pure, assert on the return value
   float CalculateThrowPower(CursorVelocity velocity) { … }

   // Hard to test — mutates external state, nothing to assert
   void ApplyThrowPower(GrabbedEntity e) { e.Power -= drag; }
   ```

3. **Small surface area.** Fewer methods = fewer tests needed. Fewer params = simpler test setup.

## Relationships

- A **Module** has exactly one **Interface** (the surface it presents to callers and tests).
- **Depth** is a property of a **Module**, measured against its **Interface**.
- A **Seam** is where a **Module**'s **Interface** lives.
- An **Adapter** sits at a **Seam** and satisfies the **Interface**.
- **Depth** produces **Leverage** for callers and **Locality** for maintainers.

## Rejected framings

- **Depth as ratio of implementation-lines to interface-lines** (Ousterhout): rewards padding the
  implementation. We use depth-as-leverage instead.
- **"Interface" as the C# `interface` keyword or a class's public methods**: too narrow — interface
  here includes every fact a caller must know (invariants, ordering, error modes, config, perf).
- **"Boundary"**: overloaded with DDD's bounded context. Say **seam** or **interface**.

## Going deeper

- **Deepening a cluster given its dependencies** — see [DEEPENING.md](DEEPENING.md): dependency
  categories, seam discipline, and replace-don't-layer testing.
- **Exploring alternative interfaces** — see [DESIGN-IT-TWICE.md](DESIGN-IT-TWICE.md): spin up
  parallel sub-agents to design the interface several radically different ways, then compare on
  depth, locality, and seam placement.

---
*Fork of mattpocock `codebase-design` — see ADR-0001.*
