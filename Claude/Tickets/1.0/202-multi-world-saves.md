---
id: 202
title: Multi-world saves + switcher
area: World
status: open
assignee:
blocked-by: []
---

## What to build

The main menu grows world creation & switching: multiple worlds, **one save per world**, a switcher listing worlds (name, day, last played) with create/continue/delete. Continue resumes the last-played world. All behind the existing `ISaveSystem`/slot seam — per-world offline clocks stay independent. Until Proc-gen island (201) lands, New World clones the handcrafted island (expand–contract: the generator slots in later without touching this flow).

## Acceptance criteria

- [ ] Multiple worlds, isolated saves; switcher lists name/day/last-played
- [ ] Continue resumes last-played world; New World creates + enters a fresh one
- [ ] Delete requires confirmation and can't nuke the wrong slot
- [ ] Per-world offline clocks independent (switching worlds doesn't cross-contaminate elapsed time)
- [ ] Works before 201 (handcrafted clone) and with it after

## Blocked by

None — can start immediately after HS close.
