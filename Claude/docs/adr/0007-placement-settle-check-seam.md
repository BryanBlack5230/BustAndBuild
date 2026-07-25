# Placement is a shared settle-check seam, not a release-moment intercept

## Context

Inserting the Beacon Core is the first instance of a general verb: drop a grabbable near a designated
place to assign it there. The named second consumer is the planned **assign-Units-to-buildings**
feature (drop a Unit near an assignable building). Because a real second consumer exists, a generic
seam is justified (cf. `Claude/learnings/design-heuristics.md` — no abstraction without a named
second consumer).

## Decision

Placement/assignment is detected by a **settle-check**, not by intercepting the release. An entity
tagged `Claimable` that is **`InAir`-enabled, moving slower than an `InsertThreshold` velocity, and
within an `AssignablePlace`'s radius** is claimed by that place; the place's handler then acts
(Beacon Core → `TrySpend` the insert cost + `StartDayCommand` + hide the Core; Unit → assign, later).
The grab/throw classes (`GrabbingInteractor`, `ReleaseCoordinator`) are **not** touched — the Core is
released as a normal grabbable and the check observes it afterward.

## Considered Options

- *Intercept inside `GrabbingInteractor.Release()` before the throw branch.* Rejected: couples the
  generic grab class to placement, and reacting after release would mean undoing the just-applied
  throw (impulse + `InAir`). The settle-check needs zero grab/throw changes and tolerates a frame or
  two of physics.
- *Gate on the throw-gesture power (`rawPower <= ThrowThreshold`).* Superseded: gating on the
  entity's **live velocity** near the place also stops a fast fly-by from being assigned by accident
  and catches a slow drifting object — independent of how it was released.

## Consequences

- Detection must **debounce per settle** (a one-shot guard, cleared when the entity lands / leaves
  the window or is re-grabbed); otherwise a can't-afford Core re-runs `TrySpend` every frame it
  lingers `InAir` near the socket.
- `InsertThreshold` must tolerate the first frames of fall (gravity grows speed) — tune it generously
  or sample release-velocity.
- Implemented Mono-side for the single Core now; lifts to an ECS detector when Unit-assignment brings
  volume. The stable contract is `Claimable` + `AssignablePlace { position, radius, velThreshold,
  handler }`.
