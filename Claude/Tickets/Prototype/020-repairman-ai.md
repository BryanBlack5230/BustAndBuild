---
id: 020
title: Repairman AI
band: Trim
status: open
assignee:
blocked-by: [008]
---

## What to build

Repair Shop Guards get their role: **non-combatant** repairman targeting the **nearest damaged (HP > 0) perimeter structure** (WallSection or tower) and restoring HP over time. Destroyed structures are gone for the Day (breach one-way rule); interior buildings have no HP. Flickable like any ally; obeys ally emotions (Scared flees to Repair Shop ring).

First cut at expiry per the Trim order (repairman → Shaman → tar barrel).

## Acceptance criteria

- [ ] Repairman walks to and repairs the nearest damaged perimeter structure; repair rate config
- [ ] Ignores destroyed structures and interior buildings; idles at home when nothing is damaged
- [ ] Never attacks; flick/grab and emotion rules work as for any ally
- [ ] Retargets sensibly when the current target reaches full HP or is destroyed mid-repair

## Blocked by

- Staffing + assignment transfer (008)
