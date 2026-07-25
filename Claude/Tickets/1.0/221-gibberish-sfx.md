---
id: 221
title: Gibberish speech + SFX completion
area: Audio
status: open
assignee:
blocked-by: [219]
---

## What to build

**HITL** (sound sourcing = Bryan). Two halves: (1) **Gibberish speech** — syllable-bank synthesis (Animal-Crossing style), pitched per villager, no real language (fiction: the deity can't understand them); cursor-reactive speech clouds and prayer poses get voices. (2) **SFX library completion** — monster vocalizations, ally combat, beacon hum/destruction, remaining verb sounds, and UI SFX (core-verb SFX shipped at proto, destruction at HS). Everything routes through the middleware/mixer 219 decided.

## Acceptance criteria

- [ ] Villager speech synthesized from a syllable bank, per-villager pitch variation
- [ ] SFX library covers combat + beacon + UI; no silent core interaction remains
- [ ] All audio routed per the 219 verdict; volume options still govern everything

## Blocked by

- FMOD trial spike (219) — routing target only; syllable-bank design can start anytime
