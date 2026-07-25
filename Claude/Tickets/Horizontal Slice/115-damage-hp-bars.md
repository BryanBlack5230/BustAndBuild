---
id: 115
title: Damage-reactive HP bars
area: UI
status: open
assignee:
blocked-by: []
---

## What to build

**Anything damaged shows its HP bar temporarily, then fades** — units, WallSections/towers, the Beacon. World-anchored, pooled, refresh-on-hit. Special buildings never show one (no HP, unkillable).

## Acceptance criteria

- [ ] Damage event → bar appears over the target, fades after config seconds, refreshes on repeat hits
- [ ] Covers units, perimeter structures, Beacon; never Special buildings
- [ ] Pooled and peak-battle safe (50–100 enemies)
- [ ] Any always-on debug bars retired

## Blocked by

None — can start immediately.
