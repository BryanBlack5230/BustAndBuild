---
id: 225
title: Deity descend/ascend transition
area: Art
status: open
assignee:
blocked-by: []
---

## What to build

The deity descend/ascend transition (TaskBreakdown 2.6), deferred from HS: **camera dolly + post-FX + audio crossfade** on framing swaps (Map ↔ Battle/City) — the deity fantasy made physical, replacing the plain Cinemachine zoom. Hard constraint: it must never add friction to the mid-Day scene-leave tension — fast, interruptible, and the unattended battle sim keeps running underneath (ADR-0009). Also dresses the menu-background entry (203) so Continue feels like descending into the world.

## Acceptance criteria

- [ ] Descend/ascend FX on all framing transitions; plain-zoom fallback stays as a config toggle
- [ ] Fast + interruptible; mid-Day City trips take no longer than the HS zoom
- [ ] Audio crossfade tied in per the middleware verdict (219)
- [ ] Menu → world entry uses the descend

## Blocked by

None — can start immediately after HS close.
