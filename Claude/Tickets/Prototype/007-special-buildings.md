---
id: 007
title: Special buildings
band: Core
status: open
assignee:
blocked-by: []
---

## What to build

Four **Special buildings** — Barracks (melee), Archery Range (archer), Mage Academy (mage), Repair Shop (repairman) — stand on separate fixed spots inside the Castle (tickets 002/010): **no HP, untargetable, unkillable**. Physically a normal collider: slight steering obstacle, thrown bodies bounce off, no other gameplay rule. Enemy targeting rule: enemies target perimeter structures, the Beacon, and Units — **never Special buildings**; incidental damage does nothing.

Staffing slots and Guards are ticket 008; Heal Rings are ticket 010.

**HITL:** the four spots authored in the scene by Bryan (spot positions are open map-fog — pick pragmatic placeholders).

## Acceptance criteria

- [ ] Four buildings present on fixed interior spots
- [ ] Enemy target selection never picks them; incidental hits change nothing
- [ ] Bodies (thrown or walking) collide/bounce; steering treats them as obstacles
- [ ] Building definitions are SO-based per ADR-0004 (class association data-driven)

## Blocked by

None — can start immediately.
