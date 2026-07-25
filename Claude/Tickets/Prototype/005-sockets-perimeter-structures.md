---
id: 005
title: Sockets + perimeter structures + empty-socket breach
band: Core
status: open
assignee:
blocked-by: []
---

## What to build

The wall line becomes ~6 fixed **Sockets**; **geometry decides the structure** (ticket 010): straight Sockets hold a WallSection, corner Sockets hold a **tower** — a corner WallSection variant (destructible, breach-relevant, repairable, no staffing, no Heal Ring; its Mount comes with the Mounts ticket). Breach semantics per ticket 002:

- **Breach = the perimeter has a hole**, however it appeared. Destroyed tower breaches exactly like a destroyed WallSection (existing breach detection → global re-evaluation).
- A Day starting with **any empty Socket = breached from tick one** — enemies path through the gap instead of battering walls.
- Breach is **one-way per Day**: no mid-Day rebuild. Day end rebuilds everything free and resets the flag.

No purchase flow yet — sockets fill via dev/debug toggle or hand-authored layout; the shop is ticket 006.

**HITL:** Socket positions authored in the subscene by Bryan (Claude supplies placement spec).

## Acceptance criteria

- [ ] All-sockets-filled Day: enemies batter walls as today
- [ ] Empty-socket Day: breached from tick one, enemies path through the gap
- [ ] Destroying a tower triggers the same breach flow as a WallSection
- [ ] Destroyed structures stay gone until Day end; Day end restores all structures free + resets breach
- [ ] Socket count/layout data-driven (config/SO), not hardcoded

## Blocked by

None — can start immediately.
