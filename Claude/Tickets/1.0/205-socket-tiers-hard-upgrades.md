---
id: 205
title: "Socket tiers: hard upgrades + Mount gating"
area: Defense
status: open
assignee:
blocked-by: [204]
---

## What to build

The tier system's shape/capability half. **Hard upgrades queue and apply when the battle ends**: wall T2→T3 **battlement** — walkable top, the ONLY mountable wall tier. Mount gating is diegetic: lower tiers simply aren't an `AssignablePlace` (no top to stand on, visible in the card, cursor never offers assign, no refusal UX). This supersedes the prototype's ungated wall mounting. **Towers mountable from T1**; tier raises HP + height (taller perch = bigger mounted-range multiplier, per-tier knob); **T3 adds a second Mount**. Queued hard upgrade + destruction both resolve at Day end — rebuild lands already-upgraded. Melee-on-wall shrug rule unchanged.

## Acceptance criteria

- [ ] Wall Mounts exist only at T3; T1/T2 never offer assign (no refusal UX needed)
- [ ] Hard purchases queue mid-Day and apply at Day end; soft effects (204) stay instant
- [ ] Tower mounted-range multiplier is per-tier config; T3 tower carries two Mounts
- [ ] Destroyed socket with a queued hard upgrade rebuilds already-upgraded at Day end
- [ ] Mount deployments persist across the upgrade (mounted Guard re-mounts the new shape)

## Blocked by

- Socket tiers: core + soft effects (204)
