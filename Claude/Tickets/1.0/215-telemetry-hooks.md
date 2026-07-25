---
id: 215
title: Telemetry hooks
area: Tuning
status: open
assignee:
blocked-by: []
---

## What to build

Balance telemetry (TaskBreakdown 13.7), local-first: per-run metrics captured to files — Day reached, Wealth/Danger over time, deaths by cause, verb usage (flicks, PowerHits, dunks, grabs), economy flows (income/sinks per resource), ability/throwable usage, emotion-state distributions. Versioned schema, readable in tuning sessions without special tooling. EA testers can export/attach their files (opt-in — no silent phoning home). This is the data source plateau tuning (216) runs on.

## Acceptance criteria

- [ ] Per-run metrics written locally, versioned schema, human/Claude-readable format
- [ ] Coverage: combat verbs, waves, economy, emotions, Wealth/Danger timeline
- [ ] No measurable frame cost at peak battle (50–100 enemies)
- [ ] Opt-in export path a tester can actually follow

## Blocked by

None — can start immediately after HS close.
