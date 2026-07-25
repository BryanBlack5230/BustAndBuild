---
id: 015
title: PowerHit finish
band: Depth
status: open
assignee:
blocked-by: []
---

## What to build

Finish the existing PowerHit stub into the second verb (DesignDoc › PowerHit): RMB **hold-to-charge, release-to-detonate**. Radius/force grow along the config SizeCurve up to the Duration cap (auto-detonate at cap); detonation applies a radial impulse (Force × charge%) to **all Grabbable bodies** in radius — enemies, allies, barrels, airborne bodies (juggling emerges).

- **Zero direct damage** — shoved bodies feed the existing bounce/collision damage pipeline.
- Cost: cursor can't Grab while charging + short cooldown (~3–5s). Disabled while Grabbing.
- Symmetric physics: scatters your own guards, even off walls. Submerged Digger immune (not Grabbable — falls out naturally).
- Charge telegraph = simple ring decal. No new emotion triggers — collisions fire damage-based rules naturally.

## Acceptance criteria

- [ ] Hold RMB grows the ring telegraph per SizeCurve; release or cap detonates
- [ ] Radial impulse scales with charge%; affected set = Grabbable bodies only
- [ ] No direct damage anywhere; kills come via the existing collision pipeline
- [ ] Grab blocked while charging; PowerHit blocked while Grabbing; cooldown enforced
- [ ] Airborne bodies receive the impulse (juggle works)

## Blocked by

None — can start immediately.
