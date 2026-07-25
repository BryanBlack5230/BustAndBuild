# Battle simulation runs continuously across scene switches

## Context

Design.md promises that the world keeps living while the player looks elsewhere: monsters are
visible from the Map mid-battle, and the beacon-buff accelerates City growth *during* an active
Day. Grilling the Horizontal Slice (2026-07-09, `Claude/DesignDoc.md`) sharpened this into a
named core tension: **the player may abandon an active battle to farm the beacon buff in the
City, leaving walls and Guards to hold alone.** Player attention is the game's real resource —
now spent across scenes, not just across the Battleground.

## Decision

- **The battle sim runs full-rate, unattended, while the City or Map scene is in front.**
  No time dilation, no sim pause on scene switch. The ECS world's lifecycle is independent of
  which Unity scene is fronted.
- Scene switching mid-Day must therefore not unload the Battleground ECS subscene or its
  bridges; a visual scene change is not a sim lifecycle event. (App quit mid-battle remains
  "Beacon destroyed" per Design.md — leaving the *scene* is explicitly not quitting.)
- Global time controls (pause menu slow-mo) still affect everything uniformly — pause is a
  world-level control, not a per-scene one.
- The Horizontal Slice must prove this architecture end-to-end (it is one of the HS exit
  gates, together with the 50–100-enemy + city-sim perf checkpoint).

## Considered Options

- *Lock the player into the Battleground while a Day is active.* Rejected — kills the
  buff-farming decision (the reason defenses exist as an alternative to the cursor) and
  contradicts Design.md's live-Map promise.
- *Unattended battle runs at reduced speed.* Rejected — two sim rates to maintain and it
  softens both the risk and the "living world" fantasy.

## Consequences

- Scene-flow code (`*Flow`/`*Installer`, RunConfigurations) must distinguish "scene in front"
  from "sim alive"; nobody should "optimize" the battle subscene away when its scene isn't
  visible.
- Mono↔ECS bridges that assume the Battleground is fronted (camera frustum sync, cursor
  bridge, grab/throw input) must degrade gracefully when it is not.
- Balance consequence: defenses must be tuned to hold a while without any cursor
  intervention, or the tension is fake.
- Perf consequence: worst case is battle peak + city sim simultaneously — that combined load,
  not the battle alone, defines the performance budget.
