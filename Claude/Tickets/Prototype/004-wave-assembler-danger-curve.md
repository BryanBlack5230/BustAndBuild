---
id: 004
title: Wave assembler + Danger curve
band: Core
status: open
assignee:
blocked-by: []
---

## What to build

A 10-Day run assembles its waves instead of hand-spawning: **Danger = authoredCurve(dayIndex)**; the assembler picks designer-authored preset Groups summing ≈ Danger and feeds the existing spawning pipeline as a **continuous trickle with intensity valleys** over the first 3–4 min of a 5–6 min Day (breathing room, not discrete pulses). Enemies finished ≠ Day end — the sunlit tail stays.

Staged intros: D1–2 default · D3 +fast · D4 +sturdy (D6 Shaman and D8 Digger presets land with their own tickets). Past D10: extrapolate +15%/Day, all types.

## Acceptance criteria

- [ ] Playable 10-Day escalating run using only existing variants (default/fast/sturdy)
- [ ] Group presets are ScriptableObjects per ADR-0004, designer-editable without code
- [ ] Trickle pacing with visible valleys; Day length + trickle span are config knobs
- [ ] Past D10 the curve extrapolates +15%/Day
- [ ] Unit tests: assembler picks Groups summing ≈ Danger for representative curve points

## Blocked by

None — can start immediately.
