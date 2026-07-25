---
id: 204
title: "Socket tiers: core + soft effects"
area: Defense
status: open
assignee:
blocked-by: []
---

## What to build

The socket-tier system's foundation (grilled 2026-07-11, DesignDoc › Socket tiers). Every perimeter structure (WallSection, tower) and Special building carries a **tier (1–3)** plus a **0..n module-slot field** (shape only — NO modules authored; door stays open). Purchase anytime incl. mid-Day via the structure's click-panel: T1 Wood → T2 Wood+Stone → T3 Wood+Stone+**Iron** (peak defense ties to the ironsmith chain; values = tuning). **Soft effects apply instantly**: wall/tower HP jump heals by the added max (`current += Δmax` — panic button by design); Special-building tier = staffing-slot cap only (no HP, unkillable, per tickets 002/010). Destruction rebuilds free **at the same tier** between Days — tier investment is permanent per socket. Tiers & slots count as durables → Wealth (upgrading raises Danger). Hard (shape/capability) upgrades are ticket 205.

## Acceptance criteria

- [ ] All socket structures + Special buildings carry tier state, persisted in the world save
- [ ] Click-panel purchase anytime incl. mid-Day; Wood/Stone/Iron costs per tier table (config)
- [ ] Soft effects instant: wall/tower `current += Δmax`; Special-building slot cap grows live
- [ ] Destroyed structure rebuilds at its tier at Day end
- [ ] Wealth includes tiers + slots; Danger reacts to upgrades
- [ ] Module-slot field exists, empty, no UI commitment

## Blocked by

None — can start immediately after HS close (iron income exists at HS).
