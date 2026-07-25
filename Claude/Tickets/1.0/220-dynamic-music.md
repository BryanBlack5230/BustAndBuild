---
id: 220
title: Dynamic music + city ambient
area: Audio
status: open
assignee:
blocked-by: [219]
---

## What to build

**HITL** (score authoring/sourcing = Bryan). The full dynamic-music model on the middleware 219 chose: parameters `dayPhase` (idle/battle/sundown) · `threat` (active enemy count × Danger) · `beaconProximity` (nearest enemy) drive layer mixing in battle; stingers on Day start, wall breach, Day end; **City ambient layer scales with building count/type**. The three stock HS beds retire in favor of real score; framing swaps (Map ↔ Battle ↔ City) crossfade instead of hard-cutting.

## Acceptance criteria

- [ ] Three parameters audibly drive battle layers
- [ ] Three stingers wired (Day start, breach, Day end)
- [ ] City ambient scales with building count/type
- [ ] Framing transitions crossfade; no hard cuts

## Blocked by

- FMOD trial spike (219)
