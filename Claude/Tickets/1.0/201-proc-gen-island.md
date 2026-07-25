---
id: 201
title: Proc-gen island
area: World
status: open
assignee:
blocked-by: []
---

## What to build

Starting a new world generates a fresh island **once per world** — terrain, water/beach boundaries, resource-node placement (wood/stone/iron, farm-viable soil), city region, and the battle framing's anchors (socket line, spawn shore, Base, killing field). Its value is replay variety across worlds (why DesignDoc parked it to 1.0). The generated island must satisfy every existing consumer unmodified: building-placement proximity checks, node workplace anchors, enemy sea-spawn → Beacon pathing, water as a gameplay surface (Dunk). The handcrafted HS island stays valid for existing worlds.

## Acceptance criteria

- [ ] New World → generated island: terrain, beaches, node placement pass validity rules
- [ ] Battle framing works on generated terrain: socket line, spawn shore, Base, breach pathing
- [ ] City framing works: buildable ground, reachable nodes, placement validity unmodified
- [ ] Generation runs once and persists in the world save; reload reproduces the same island
- [ ] Two consecutive New Worlds are visibly different
- [ ] Existing handcrafted-island worlds still load

## Blocked by

None — can start immediately after HS close.
