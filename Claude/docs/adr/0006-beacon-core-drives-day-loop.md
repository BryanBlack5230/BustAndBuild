# Beacon Core drives the Day loop; beacon invulnerability ends the Day

## Context

The Day (active combat window) used to auto-start on game start and could auto-loop on a timer,
independent of any other system. We are making the **Beacon Core** — a grabbable sphere — the
player-driven gate for the loop, which couples the previously-independent Beacon/Structure (ECS)
and DayNightCycle (Mono) systems together.

## Decision

- **Insertion starts the Day.** `DayNightCycle.OnStartGame`'s auto-start is removed; gently placing
  the charged-by-payment Beacon Core into the Beacon dispatches `StartDayCommand`. The
  `DayCycleStateOverride` (StartDay / ForceFinish) is kept as the dev/debug entry point.
- **Beacon invulnerability ends the Day.** The Beacon bakes `IsInvulnerable` (it didn't before), so
  the `HealthAspect` tripwire clamps it to 1 HP instead of killing it. `BeaconEcsBridge` (Mono,
  `IGameUpdateListener`) polls the Beacon's `IsInvulnerable` each frame and, on the rising edge,
  dispatches `ForceFinishDayCommand`. Chosen over an ECS-raised event so the signal **pauses with the
  game** (the Day must not end during a soft-pause) and both day-control directions live in one Mono
  class. The timer-expiry path remains the other Day-end cause.
- **Both Day-end causes converge on `DayEndedEvent`** (the invuln path after its 3 s sundown). A
  single subscriber re-enables + repositions the one persistent Core entity at the Beacon's drop
  socket and heals the Beacon to full (direct `Health = Max` + clear `IsInvulnerable`). Extends the
  established Mono→ECS bridge shape of `DaylightEcsBridge`.

## Considered Options

- *Day auto-loops independently; Core/Beacon are cosmetic.* Rejected — the design pillar is a
  player-gated loop where surviving the Beacon assault is the win condition for the Day.
- *Drop Core + heal at the invuln instant (before sundown).* Rejected — duplicates the drop/heal
  logic across two trigger points; unifying on `DayEndedEvent` keeps one seam, at the cost of a ~3 s
  delay on the invuln path (cosmetic).

## Consequences

- Beacon HP is now load-bearing for the day cycle: removing/zeroing the Beacon's `IsInvulnerable`
  silently breaks Day-end (the Beacon would die, or the Day would never force-finish). Don't strip it.
- ECS↔Mono is now bidirectional for this loop: Mono→ECS pushes day-phase + drop/heal; Mono polls
  the Beacon's invuln flag to drive the day-end. A future reader should not "simplify" either
  direction away.
- The Core's pay-on-insert cost (100 Pearls, free after 5 min since drop) and the persisted
  free-countdown timer are mechanics/how-it-works — recorded in `Claude/learnings/`, not here.
