# Notifications via a custom EventBus; requests via CommandDispatcher

**Status:** accepted

Two separate in-process buses carry "A wants B to know / do something": `EventBus` for past-tense
notifications (1-to-many, fan-out, unhandled is normal) and `CommandDispatcher` for imperative
requests (1-to-1, throws on duplicate handler, silent no-op when unhandled). The default for
same-layer calls stays **direct Reflex injection** — a bus is used only to cross a layering
boundary, reify intent as storable data, or send from a non-injectable site (SO /
`SerializeReference`). This replaced the old loose-`Action` `EventManager`.

## Considered options

- **A single bus / mediator for both** → rejected: collapses the request-vs-notification signal
  that the type names encode for humans (`Send(new Fire())` reads imperative, `Raise(new Fired())`
  reads indicative).
- **An off-the-shelf bus (e.g. GenericEventBus)** → rejected: we needed allocation-free `Raise`
  for 100+ units/frame (per-type `static class Listeners<T>`, no dictionary lookup or list-copy),
  queued re-entrant raise, queued subscribe/unsubscribe during dispatch, and `SubsystemRegistration`
  clearing that survives Disable-Domain-Reload (ADR-0003). The custom bus is ~one file and owns
  these invariants.

## Consequences

- `EventBus` is **static** (not Reflex-injected) on purpose, so ECS systems and MonoBehaviours use
  it with no DI plumbing; it self-clears on play-mode enter (ADR-0003).
- `CommandDispatcher` is Reflex-injected and enforces 1-to-1 via the type system (`Dictionary<Type,
  object>` of a single `Action<T>`), so the rule is the code, not a convention.
- How-it-works + the full direct-inject / command / event decision guide live in
  `Claude/learnings/events-and-services.md`.
