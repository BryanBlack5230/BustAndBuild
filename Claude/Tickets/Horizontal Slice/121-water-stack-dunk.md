---
id: 121
title: Water render stack + Dunk presentation
area: Visuals
status: open
assignee:
blocked-by: [117]
---

## What to build

The HS water stack per the spike recipe: **true planar reflection** (mirrored ortho camera → half-res RT, culled set: units, Structures, sky, hero lights), ripple distortion, depth tint, **shoreline + emergence foam**. Dunk presentation rides it: splash VFX on water entry, emergence foam on the spawn-shore reappear. **Fallback:** if proto's Water Dunk (proto ticket 019, Depth band) was cut at box close, the stack still lands — the two Dunk VFX criteria void.

## Acceptance criteria

- [ ] Planar water at all framings within perf budget (half-res, culled)
- [ ] Shoreline foam + emergence foam
- [ ] Dunk entry splash VFX + reappear foam (only if proto 019 shipped)
- [ ] Trajectory predictor's impact circle still renders on water

## Blocked by

- Look-dev spike (117)
