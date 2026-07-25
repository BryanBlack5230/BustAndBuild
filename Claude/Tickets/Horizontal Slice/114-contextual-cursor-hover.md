---
id: 114
title: Contextual cursor + hover detail
area: UI
status: open
assignee:
blocked-by: []
---

## What to build

The existing cursor system grows a **state machine that transcribes the available verb**: grab (enemy), ally-colored grab, default/move, assign-to-building over valid drop targets, PowerHit cooldown ring… **Hover = the detail layer:** tooltips and full readouts on structures/buildings (HP, rates, slots) — the HUD stays diegetic-first, the overlay stays counters + Faith bar. Starts on proto verbs; city verbs (assign/staff) register as their tickets land.

## Acceptance criteria

- [ ] Cursor icon changes per hover context (enemy grab / ally grab / assignable drop / default)
- [ ] While holding a unit, valid drop targets read as such
- [ ] Hover on structures/buildings shows tooltip with HP or rates/slots
- [ ] Verbs register declaratively — adding one doesn't rewrite the machine

## Blocked by

None — can start immediately.
