# Design Heuristics

Cross-cutting design rules for this project that aren't tied to a single system.

## Polymorphism Test — when extracting an abstraction is justified

Before extracting a generic core out of a working system — an interface, a base class, a reusable "engine", or a promotion from `Gameplay/` to `Infrastructure/` — require a **named second consumer that exists (or is concretely planned) right now**: a system in this project that would use the extracted part with identical control logic.

- "Might be useful later" does not pass. If no second consumer can be named, keep the code where it is, shaped for its single caller.
- The inverse also holds: **simplicity of the extracted core is not an argument against extraction.** If a real second consumer exists, even a thin core (a dictionary + an event) is worth splitting out.
- "Uses" ≠ "shares logic with": a system *consuming* `EventBus` or `WorldSaveService` is not a reason to split that system. Dependency is not polymorphism.

**Why:** speculative abstractions cost indirection today for reuse that usually never comes, and they produce *false extension points* — interfaces with exactly one implementation that force every reader through a contract while guaranteeing nothing actually swaps.

**How to apply:** when a proposal says "generalize X so it can also handle …", ask: *name the second consumer in this codebase (or in TaskBreakdown) today.* E.g. "generalize wall segments into a placeable-system" passes only if a concrete second placeable (special buildings on walls) is being built against the same logic; a generic "stat modifier pipeline" fails while bounce damage is its only user.

*(Adapted from the splitting criterion in the Vortex AITools `vortex-layer-detect` skill; the Vortex framework itself was not adopted.)*
