---
id: 207
title: Special enemy A
area: Roster
status: open
assignee:
blocked-by: [206]
---

## What to build

First new special, end-to-end per its 206 spec: profile ScriptableObject (ADR-0004 pattern), ECS components/systems, spawning via wave-assembler preset Groups from its staged-intro Day, emotion-system integration, telegraphs (emote/tint + Visual Companion presentation), config knobs in the ConfigHub surface. Demoable bar: a Day featuring it plays out with the new battle shape visible and counterable.

## Acceptance criteria

- [ ] Spawns via preset Groups from its staged-intro Day; past-plateau extrapolation includes it
- [ ] Behavior matches the 206 spec incl. emotion interactions
- [ ] Telegraphs readable (emote/tint/companion); counters via existing verbs work
- [ ] Config knobs designer-editable; tests cover its decision logic

## Blocked by

- New specials — design session (206)
