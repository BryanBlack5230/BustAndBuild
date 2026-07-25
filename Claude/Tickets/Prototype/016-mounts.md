---
id: 016
title: Mounts
band: Depth
status: open
assignee:
blocked-by: [005, 008]
---

## What to build

**Mounts = Grab & place** via the AssignablePlace seam (ADR-0007): 1 Mount per WallSection top and per tower top; grab again to unmount. Grab-off-a-Mount → release at a building = unmount + transfer in one motion (ticket 008 verb).

- Mounted: **invulnerable** (enemies hit the structure), attack range × mounted multiplier — **tower Mount > wall Mount** (two config knobs; height = sight). Mounted Guards are unhealed (perimeter has no healing).
- Melee on a Mount = allowed-but-no-op + shrug indicator.
- **Structure destroyed under a mounted Guard:** 50% max-HP fall damage (floor 1 — never lethal), fights on from the ground; a hard fall usually trips Scared → flees to its home ring.
- **Mounts persist as deployments:** a mounted Guard starts the next Day mounted; after a mid-Day collapse the fallen Guard re-mounts the rebuilt structure on its own between Days.

## Acceptance criteria

- [ ] Grab-place a Guard onto a wall/tower top mounts it; grab again unmounts
- [ ] Mounted Guard invulnerable; range multiplied per structure type (tower > wall, config knobs)
- [ ] Melee Guard mounts fine, attacks nothing, shows the shrug indicator
- [ ] Structure destruction drops the Guard with 50% max-HP damage (floor 1); Courage/Scared reacts naturally
- [ ] Deployment persists across Days incl. re-mount after mid-Day collapse
- [ ] Mount-to-building release performs unmount + transfer atomically

## Blocked by

- Sockets + perimeter structures + empty-socket breach (005)
- Staffing + assignment transfer (008)
