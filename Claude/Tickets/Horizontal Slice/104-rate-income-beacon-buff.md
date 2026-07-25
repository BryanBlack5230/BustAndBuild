---
id: 104
title: Rate-based income + beacon-buff growth
area: City
status: open
assignee:
blocked-by: [102, 103]
---

## What to build

Grab & place a villager onto a city work building = **assign** (civilian brain, slot fills); a staffed building yields **X/min into the stockpile** — rate-based for HS, walk-loops decorative (Visual Companions, 118). Catalog grows: **farm, stonemason, ironsmith** (+ 102's woodcutter). **Farm crops deplete on harvest and regrow**; regrowth scales with sunlight — dim default, ×N while the Beacon is active (beacon-buff multipliers: farms large · woodcutter slight · stone/iron none, config). The income formula carries a **per-building efficiency factor (=1 at HS)** so the committed 1.0 physical-hauling retrofit re-bases rates without reshaping the formula.

This closes the starvation loop with 103: staffed farm sustains the population; unstaffed, food drains to deaths.

## Acceptance criteria

- [ ] Grab villager → release at work building with open slot = assigned, income starts; full building = normal drop
- [ ] Each staffed building ticks its resource at its SO-config rate
- [ ] Farm regrowth visibly faster during an active Day (beacon-buff)
- [ ] Income formula includes the per-building efficiency factor (=1), shared with the offline calc (110)
- [ ] End-to-end: staffed farm sustains pop; unstaffing it leads to starvation

## Blocked by

- Building placement (102)
- Villager population (103)
