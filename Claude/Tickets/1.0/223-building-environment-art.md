---
id: 223
title: Building + environment art
area: Art
status: open
assignee:
blocked-by: []
---

## What to build

**HITL-heavy** production art sprint. The cardboard kingdom (ADR-0010) gets its real cards: all Structures as painted sprite cards — city catalog (~10), WallSections + towers **× 3 tiers** with damage-state texture swaps, Beacon, Special buildings — footprint pivots, roofline-occlusion discipline. Environment set: island terrain, water/beach, **vegetation states** (dim vs beacon-buffed growth must read). The T3 battlement card must *visibly* have a walkable top — it IS the diegetic Mount gate (205). Proc-gen (201) composes from this set, so pieces must tile/mix without hand-tuning.

## Acceptance criteria

- [ ] All structures as painted cards, footprint pivots, damage states swap correctly
- [ ] Wall/tower tiers visually distinct; T3 battlement reads walkable at a glance
- [ ] Terrain/water/vegetation set; vegetation reads growth states
- [ ] Generated islands (201) look composed, not scattered

## Blocked by

None — tier shapes are already designed; batches can start immediately.
