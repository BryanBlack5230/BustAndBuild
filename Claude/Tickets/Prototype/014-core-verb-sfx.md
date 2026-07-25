---
id: 014
title: Core-verb SFX
band: Core
status: open
assignee:
blocked-by: []
---

## What to build

Placeholder one-shots for the core verbs (DesignDoc › Prototype shell › Audio): grab, throw whoosh, border bounce, ground impact, pearl pickup, beacon (hum/insert/destruction-clamp). Plain Unity audio — no music/ambience beds, no dynamic mixing, no FMOD (that trial is gated at HS). Rationale: the story test reads game feel; a silent build under-reads the fun.

Dunk splash SFX ships with ticket 019.

## Acceptance criteria

- [ ] Each core verb audibly fires at the right moment with sane volume balance
- [ ] Routed through a master volume the Options slider controls (ticket 012 consumes)
- [ ] No clipping spam when many impacts fire in one frame (throttle/pool)
- [ ] Assets loaded per project convention (AssetService)

## Blocked by

None — can start immediately.
