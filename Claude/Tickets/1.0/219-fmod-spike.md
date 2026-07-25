---
id: 219
title: FMOD trial spike
area: Audio
status: open
assignee:
blocked-by: []
---

## What to build

The **2–3 day trial gate** at the HS→EA boundary, run when dynamic-music work starts: integrate FMOD and build one battle track + a `threat` parameter + one stinger. Verdict: adopt FMOD, or knowingly fall back to the Unity mixer. Explicit rule from the grilling: do NOT hand-roll adaptive layering on Unity's mixer first and migrate later — this spike decides the middleware before any layering work exists.

## Acceptance criteria

- [ ] FMOD in-project; battle track layers respond to `threat`; one stinger fires
- [ ] Verdict recorded as an ADR within the timebox
- [ ] If rejected: Unity-mixer fallback plan documented before 220 starts

## Blocked by

None — run when dynamic-music work starts (gates 220/221 routing).
