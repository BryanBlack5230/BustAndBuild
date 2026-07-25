---
id: 222
title: Character art + animation sets
area: Art
status: open
assignee:
blocked-by: []
---

## What to build

**HITL-heavy** production art sprint (Bryan authors; Claude preps pipeline/specs/import tooling). Painted/storybook **skeletal 2D** sets for all units — villager variants, all monster variants incl. new specials, ally classes — full anim list: idle / walk / attack / hurt / scared / angry / death. AI-gen + manual cutout pipeline (skeletal needs part-segmented art — segmentation cost budgeted per character, pipeline documented + repeatable). Visual Companions consume everything; the HS emotion presentation (cursor-tracking eyes + posture) extends across the full roster. Existing-roster batches can start immediately; new-special batches follow 207/208.

## Acceptance criteria

- [ ] Every shipped unit has its full anim set live on its Visual Companion
- [ ] Segmentation/cutout pipeline documented; per-character cost known and repeatable
- [ ] Emotion telegraphs (eyes + posture) present across the roster
- [ ] Atlased per pipeline conventions (~2× display size, per-character skeletal atlases); perf holds at 50–100 companions

## Blocked by

None — start with the existing roster; new-special batches follow 207/208.
