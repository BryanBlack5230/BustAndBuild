---
id: 108
title: Wealth → Danger director
area: Director
status: open
assignee:
blocked-by: [102, 103]
---

## What to build

The real formula replaces the prototype's `authoredCurve(dayIndex)` input to the wave assembler: **Wealth = Σ weighted durables** (city buildings, battle sockets/staffing, population) — **liquid currency invisible** (savers aren't punished; losses lower Danger automatically — the recover-after-absence promise). Snapshot at Beacon Core insert (and after the offline calc once 110 lands). **Danger = curve(Wealth) ± small variety band** → feeds the existing wave assembler unchanged. **Wealth is never shown** (internal director value); Danger has no meter.

## Acceptance criteria

- [ ] Wealth sums durables only; pearl/stockpile swings don't move it
- [ ] Snapshot at Core insert; the Day assembles from curve(Wealth) ± band
- [ ] Losing villagers/buildings measurably lowers the next Day's Danger
- [ ] Weights, curve, and band are SO config
- [ ] Unit tests: representative durable states → expected Danger ranges
- [ ] No Wealth number anywhere in the HUD

## Blocked by

- Building placement (102)
- Villager population (103)
