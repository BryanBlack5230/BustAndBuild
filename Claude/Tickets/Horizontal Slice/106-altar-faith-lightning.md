---
id: 106
title: Altar + Faith + lightning
area: City
status: open
assignee:
blocked-by: [104]
---

## What to build

The third resource loop proven end-to-end: **altar** joins the catalog (placeable via 102, staffed via 104); villagers on the altar generate **Faith** at a rate. **Faith bar joins the HUD overlay** (overlay = resource counters + Faith bar, nothing else — diegetic-first). One castable battle ability: **lightning** — select on the ability hotbar, click a target in the battle framing, bolt deals damage/impulse through the existing damage pipeline, costs Faith.

## Acceptance criteria

- [ ] Altar placement + staffing reuse 102/104 systems unmodified
- [ ] Faith accrues only while the altar is staffed
- [ ] Faith bar on the HUD
- [ ] Lightning: hotbar select → click-cast in battle → damage/impulse via existing pipeline; Faith deducted; insufficient Faith blocks
- [ ] Cast unavailable outside the battle framing

## Blocked by

- Rate-based income + beacon-buff growth (104)
