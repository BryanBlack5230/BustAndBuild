---
id: 113
title: Map live view
area: Cross-scene
status: open
assignee:
blocked-by: [112]
---

## What to build

Map = **functional router + live small-scale view** of one continuous world: monsters moving during an active battle, villagers moving in the city, seen from map height (no detailed animations — readability polish rides Map-height LOD, 120). Zoom-target picking (exists) now routes to two dive targets: Battle or City by cursor position.

## Acceptance criteria

- [ ] During an active Day, Map shows enemies/allies moving live (view only — sim untouched)
- [ ] Villagers visible moving in the city region
- [ ] Zoom-target picking routes to Battle or City correctly
- [ ] Framing swaps cause no sim-rate change (ADR-0009)

## Blocked by

- Unattended battle sim (112)
