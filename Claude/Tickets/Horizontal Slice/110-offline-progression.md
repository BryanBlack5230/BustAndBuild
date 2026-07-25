---
id: 110
title: Offline progression
area: Saves
status: open
assignee:
blocked-by: [104, 108, 109]
---

## What to build

**One closed-form calculation on login — a formula, not a simulation:** elapsed time → resource ticks by staffed buildings (the *same* income formula as 104, efficiency factor included), food consumption, starvation deaths with the **3–5-villager survival floor**. Then Wealth recalculates (108) so the next Day's Danger reflects the losses — offline absence → recovery is the coupling under test. The beacon free-repair countdown elapses inside the calc too (111 consumes this). Offline edge-case QA (1 day/1 week/1 month) stays at 1.0; HS needs correctness, not hardening.

## Acceptance criteria

- [ ] Returning after a gap: stockpiles ticked, food consumed, starvation applied with the floor (config 3–5)
- [ ] Closed-form — no fast-forwarded sim on load, arbitrary gap length is O(1)
- [ ] Shares the live income formula (single source of truth)
- [ ] Wealth recalculated after the calc; next Day's Danger provably eased after a starvation event
- [ ] Unit tests: elapsed→outcome table, incl. the starvation floor and zero-gap no-op

## Blocked by

- Rate-based income + beacon-buff growth (104)
- Wealth → Danger director (108)
- World save + auto/manual save (109)
