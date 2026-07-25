---
id: 227
title: Offline edge-case QA
area: Ship
status: open
assignee:
blocked-by: [202, 214]
---

## What to build

The deferred offline edge cases (TaskBreakdown 13.3): **1 day / 1 week / 1 month** absences run against the final closed-form calc (post-hauling efficiency factors). Every case must produce a sane, explainable state: starvation floor (3–5 villagers) holds, food/resource math never goes negative, Beacon repair timer folds in correctly, Wealth recalc (and therefore Danger) lands somewhere recoverable — the recover-after-absence promise is the point. Multi-world interaction verified: per-world offline clocks stay independent. Automated tests pin the closed-form at the edge durations so tuning can't silently break them.

## Acceptance criteria

- [ ] 1d/1w/1m absences produce sane states; survival floor holds; no negative resources
- [ ] Repair timer + Wealth recalc correct across all three durations
- [ ] Per-world clocks independent (switch worlds, both age correctly)
- [ ] Automated closed-form tests at edge durations

## Blocked by

- Multi-world saves + switcher (202)
- Physical hauling (214)
