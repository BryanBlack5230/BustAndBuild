---
id: 101
title: City scene bootstrap
area: City
status: open
assignee:
blocked-by: []
---

## What to build

**HITL.** The player zooms from Map into a real City framing for the first time: 2.5D isometric camera rig for the City (TaskBreakdown 2.3), pan-with-LMB-drag, `CityFlow` + `CityInstaller` per the scene-chain conventions, and a **handcrafted city region** on the island with hand-placed resource nodes (trees, stone, iron, farm soil) — scene authoring is Bryan's; Claude preps flow/installer code + placement specs. Existing zoom transitions route in/out (deity descend/ascend FX stays deferred to 1.0). World topology stays one additive world (ADR-0009): City is a camera framing, not a scene load.

## Acceptance criteria

- [ ] Map → zoom-in over city region lands in the iso City framing; zoom-out returns to Map
- [ ] Pan-with-drag works with border constraints, consistent with battle camera feel
- [ ] City region + resource nodes present in the world composition (HITL-authored)
- [ ] CityFlow/CityInstaller wired per scene-flow conventions; battle framing untouched

## Blocked by

None — can start immediately after prototype close.
