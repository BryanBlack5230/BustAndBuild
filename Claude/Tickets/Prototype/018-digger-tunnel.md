---
id: 018
title: Digger + Tunnel
band: Depth
status: open
assignee:
blocked-by: [004]
---

## What to build

The transport special (DesignDoc › Special enemies › Digger), introduced at D8:

1. Stops at long range from target → **5s dig channel** — grabbable/flickable only during this window (flick = interrupt; survivor walks back, tries again).
2. Dig complete → Tunnel open; Digger **submerges at entrance**: ungrabbable, killable only by Ally Unit damage.
3. Tunnel exit **always outside the wall perimeter**, just short of the nearest blocking Structure toward the Beacon (or short of the Beacon if no wall). Skips the killing field, never the wall.
4. **≤2 travelers underground at a time** (traverse ~3s, config); travelers untargetable.
5. Digger dies → collapse → travelers eject at entrance, **stunned ~2s** (combo window).

Includes the D8 preset + staged-intro wiring into the wave assembler.

## Acceptance criteria

- [ ] Dig channel: grabbable only during the 5s window; flick interrupts; survivor retries
- [ ] Submerged Digger ungrabbable (incl. PowerHit) and killable only by ally damage
- [ ] Exit placement rule holds with and without standing walls
- [ ] Traveler cap, traverse time, untargetability underground per config
- [ ] Collapse ejects + stuns travelers at the entrance (~2s)
- [ ] D8 preset introduces Diggers via the assembler

## Blocked by

- Wave assembler + Danger curve (004)
