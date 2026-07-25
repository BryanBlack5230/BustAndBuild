---
id: 224
title: VFX set + flavor anims
area: Art
status: open
assignee:
blocked-by: []
---

## What to build

**HITL-heavy.** The full VFX set in the established language (painted flipbook particles + shader FX — no realistic sims): beacon activation, faith abilities (lightning + the 211 additions), oil puddle, flick trail, bounce flash, ground-impact dust, pearl shimmer, Dunk splash. Plus the deferred flavor anims: **seaweed on dunked murlocs** (ticket 007 flavor) and the **Beacon repair-crew** (assigned battle units visibly fix it during the repair timer — ticket 005 flavor). Day/night lighting passes tuned per framing at key dayPercent points (the lighting is the look).

## Acceptance criteria

- [ ] VFX list covered in the flipbook+shader language; nothing photoreal
- [ ] Seaweed Dunk flavor + Beacon repair-crew anim in
- [ ] Faith-ability VFX cover the final 211 set
- [ ] Lighting passes tuned per framing; night Beacon hero-light budget respected (≤2 casters)

## Blocked by

None — ability VFX batch follows 211; everything else can start immediately.
