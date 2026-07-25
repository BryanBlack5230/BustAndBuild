---
id: 116
title: "Shell odds: scene beds, destruction SFX, resolution list"
area: Shell
status: open
assignee:
blocked-by: []
---

## What to build

The HS presentation-bar leftovers in one sweep. **Three stock/placeholder audio beds** — Map (serene), City (calm), Battle (calm pre-battle / active) — on plain Unity audio with a simple crossfade per framing. **Explicit non-goal: no FMOD, no dynamic layer mixing** — FMOD is adopted at the HS→EA boundary behind its own trial spike; do NOT hand-roll adaptive layering first. **Destruction SFX** join the core-verb set (structure collapse, Beacon damage). Options gains the **resolution list**.

## Acceptance criteria

- [ ] Per-framing bed switches with the active framing (crossfade)
- [ ] Destruction one-shots: structure destroyed, Beacon damage state
- [ ] Options: resolution dropdown alongside existing volume + fullscreen toggle
- [ ] No FMOD, no layered mixing anywhere

## Blocked by

None — can start immediately.
