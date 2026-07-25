---
id: 216
title: Endless plateau tuning
area: Tuning
status: open
assignee:
blocked-by: [205, 210, 211, 212, 214, 215]
---

## What to build

The ship-line item "the endless plateau tuned". Pure endless means difficulty and content **plateau by design** — this ticket chooses where the curve flattens and makes long worlds live there: Wealth weights (durables incl. tiers + slots), Danger curve + variety band at the plateau, economy curves (drop rates, building/upgrade/villager costs, offline rates, faith rates — TaskBreakdown 12). Telemetry-driven, EA channel as the data source. Long-run bar: a multi-week world neither runaway-snowballs nor death-spirals, and combinatorics (emotions × specials × physics × defense layouts) keep Days from feeling identical. Verify the named combo risks (Shaman↔Digger sustain + anything 207/208 flagged) are non-degenerate at plateau density.

## Acceptance criteria

- [ ] Plateau point chosen + authored (curve, assembler behavior, extrapolation retired past it)
- [ ] Wealth weights + economy curves tuned against telemetry from long runs
- [ ] Multi-week world stays challenging and recoverable — no dead ends at plateau
- [ ] Named sustain combos verified non-degenerate at 50–100-enemy density
- [ ] Tuning rationale recorded (values in config; reasoning in DesignDoc/learnings)

## Blocked by

- Socket tiers: hard upgrades + Mount gating (205)
- Group preset library ~40 (210)
- Faith abilities to 3–5 (211)
- Throwables to 3–4 + supply production (212)
- Physical hauling (214)
- Telemetry hooks (215)
