---
id: 017
title: Guard role behaviors
band: Depth
status: open
assignee:
blocked-by: [008]
---

## What to build

Guard classes differ in *play*, not just stats (DesignDoc › Guard class roles):

- **Mage**: mid range, slow cast, small **AoE splash** — punishes clumps (tunnel exits, puddles).
- **Melee**: short range, solid DPS, bodily **blocks** enemy movement — a living wall.
- **Archer**: long range, fast, single-target — verify the existing attack pipeline already delivers this (baseline, likely no new code).

Repairman behavior is ticket 020.

## Acceptance criteria

- [ ] Mage hit damages all enemies in its splash radius (config)
- [ ] Melee Guard's body stops enemy pathing (enemies can't walk through, must go around or fight)
- [ ] Archer confirmed: long range, fast single-target via existing profile knobs
- [ ] All class parameters live in profiles/ConfigHub, no hardcoding

## Blocked by

- Staffing + assignment transfer (008)
