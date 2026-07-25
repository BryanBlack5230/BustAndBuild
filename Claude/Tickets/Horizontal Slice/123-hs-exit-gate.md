---
id: 123
title: "HS exit: perf budget + checkpoint + itch build"
area: Exit
status: open
assignee:
blocked-by: [104, 105, 110, 112, 113, 118]
---

## What to build

The closing ticket — run it when the frontier is otherwise empty (listed blockers are the load-bearing gates, not the full set). **Write the perf budget** (TaskBreakdown 1.6, finally: target FPS, draw-call ceiling, unit caps per framing), then run the **perf checkpoint**: peak battle (50–100 enemies) + city sim + cross-scene continuity at target FPS — companion count at scale is a named ADR-0010 risk. Fix or file what fails. Sweep for **dead ends** (no softlocks or unrecoverable death spirals across a week of mixed play). Ship the **HS build to itch.io** — the public iteration channel — and start the coupling test (testers with real offline gaps explaining the loops back unprompted).

## Acceptance criteria

- [ ] Perf budget written (per-framing targets); min-spec testing stays at 1.0
- [ ] Peak battle + city sim + framing swaps hold target FPS
- [ ] No-dead-ends: a week of mixed play without softlock or unrecoverable spiral
- [ ] HS build live on itch.io; coupling test underway

## Blocked by

- Rate-based income + beacon-buff growth (104)
- Villager battle staffing + dismissal panel (105)
- Offline progression (110)
- Unattended battle sim (112)
- Map live view (113)
- Visual Companion system (118)
