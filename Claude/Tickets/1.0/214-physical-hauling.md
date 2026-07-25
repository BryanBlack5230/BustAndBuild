---
id: 214
title: Physical hauling
area: City
status: open
assignee:
blocked-by: []
---

## What to build

The committed 1.0 retrofit (grilled 2026-07-10, retrofit accepted — plan-for-it, don't-discover-it): villagers **physically haul** goods — walk loops become the income, not decoration. Stockpile grows on delivery, so building placement distance becomes a real economic decision. The offline closed-form gains a **per-building efficiency factor approximating haul distance**, and HS income rates get **re-based** so overall pacing survives the switch. Hauling must keep running unattended while the player is in another framing (ADR-0009 sim continuity).

## Acceptance criteria

- [ ] Staffed building's villager visibly hauls; stockpile grows on delivery, not by timer
- [ ] Offline formula uses the per-building efficiency factor; no simulation-at-login
- [ ] Income re-based: a representative HS save plays at comparable pacing
- [ ] Unattended City keeps hauling during battle; perf holds at full catalog + population

## Blocked by

None — can start immediately after HS close.
