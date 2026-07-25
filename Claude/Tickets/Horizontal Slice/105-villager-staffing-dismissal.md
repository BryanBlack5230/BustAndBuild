---
id: 105
title: Villager battle staffing + dismissal panel
area: City
status: open
assignee:
blocked-by: [103, 104]
---

## What to build

**Villagers replace pearl slots** — the city-work vs defense trade-off becomes the economy. Grab a villager → release at a battle Special building with an open slot = **staff** (Guard of the building's class: kit + battle brain; assignment = class, home, flee target in one). The prototype's slot-purchase-creates-a-Guard model retires: buying a slot is **capacity only**, a villager fills it. Proto ticket 008's transfer rules carry over unchanged, now spanning city↔battle (atomic transfer, HP by fraction, anytime incl. mid-Day).

**Dismissal is UI:** click a staffed building (city or battle) → panel showing worker slots + rates; dismiss → villager detaches to jobless, walks out, idles near the main building. Two verbs, no overlap: grab-release = assign/transfer; panel = explicit detach. The panel doubles as the building's info surface.

## Acceptance criteria

- [ ] Grab jobless villager → battle building open slot = Guard of that class (brain swap)
- [ ] City↔battle transfers atomic, HP converts by fraction (round, min 1)
- [ ] Slot purchase grants capacity only; no Guard spawns without a villager
- [ ] Building panel: click → slots + rates; dismiss → villager walks out jobless
- [ ] Panel works on both city work buildings and battle Special buildings

## Blocked by

- Villager population (103)
- Rate-based income + beacon-buff growth (104)
