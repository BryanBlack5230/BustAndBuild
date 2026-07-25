---
id: 112
title: Unattended battle sim
area: Cross-scene
status: open
assignee:
blocked-by: [101]
---

## What to build

**Mid-Day scene-leaving is a core tension:** the player zooms out to Map or into the City mid-Day; the battle keeps running **full-sim, unattended** (ADR-0009 — this ticket proves the architecture) — defenses hold or fail without the cursor while the player farms the beacon-buff. Leaving mid-grab force-releases through the normal release path with zero throw velocity (same rule as slow-mo pause); a charging PowerHit cancels. Battle input verbs scope to the active framing.

## Acceptance criteria

- [ ] Leave battle mid-Day → waves, combat, breach, and Day end all resolve unattended
- [ ] In-flight grab force-released on leave (gentle drop); PowerHit charge cancels
- [ ] Return mid-Day → coherent state, seamless resume
- [ ] Day ending while away: shop/checkpoint behave normally on return
- [ ] Battle verbs inert while City/Map framing is active

## Blocked by

- City scene bootstrap (101)
