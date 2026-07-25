---
id: 120
title: Map-height LOD
area: Visuals
status: open
assignee:
blocked-by: [118]
---

## What to build

Past a zoom threshold (with hysteresis), Visual Companions return to their pool and units render as **static ECS quad billboards** (base sprite, faction tint, no icons) — fixing map-height faction readability (TaskBreakdown 5.1 fog: factions currently indistinguishable from above). Sim untouched (ADR-0009).

## Acceptance criteria

- [ ] Threshold crossing swaps companion↔billboard both directions with hysteresis — no pops, no pool leaks
- [ ] Factions distinguishable at map height
- [ ] Map framing over a peak battle performs at least as well as the battle framing

## Blocked by

- Visual Companion system (118)
