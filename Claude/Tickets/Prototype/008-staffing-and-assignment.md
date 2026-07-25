---
id: 008
title: Staffing + assignment transfer
band: Core
status: open
assignee:
blocked-by: [006, 007]
---

## What to build

Guards come from staffing, and assignment is the atomic transfer verb (DesignDoc › Assignment model, ticket 010):

- **Staffing:** each Special building holds 1–3 staffing slots, bought one at a time in the shop (Pearls). Buying a slot creates a **Guard of the building's class** with the battle brain, bound to that building as home + flee target. No jobless state — a Guard's home can't die.
- **Reassignment = atomic transfer:** grab a Guard, release at another Special building with an open slot → old slot frees, Guard takes the new building's class (brain swap). Release at a full building or anywhere else = normal drop, assignment untouched. Allowed anytime, incl. mid-Day — same physical verb.
- **HP converts by fraction** (round, min 1): new HP = old HP% × new max.
- Assignment = brain payload: class, home, flee target in one. A Mount (ticket 016) is a *position*, not an assignment.

## Acceptance criteria

- [ ] Shop sells staffing slots per building up to its cap; purchase spawns a Guard of the correct class
- [ ] Grab-release at a building with an open slot transfers: slot bookkeeping, class kit swap, HP by fraction (round, min 1)
- [ ] Release at a full building / open ground = plain drop, no assignment change
- [ ] Transfer works mid-Day during combat
- [ ] Guard's flee target = its home building (consumed by ticket 009)

## Blocked by

- Defense shop UI (006)
- Special buildings (007)
