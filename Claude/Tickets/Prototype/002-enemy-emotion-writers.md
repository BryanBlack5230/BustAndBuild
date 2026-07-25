---
id: 002
title: Enemy emotion writers
band: Core
status: open
assignee:
blocked-by: [001]
---

## What to build

Enemies change emotional state from real gameplay events, per DesignDoc › Emotion system › Enemy table. The existing Scared *readers* (flee-to-Base, escape with smaller drop) start firing from real triggers instead of debug pokes:

- **Suspicious**: enemy within witness radius became Grabbed → slower move & attack; expires to Aware.
- **Scared**: burst damage (−2/3 max HP within window) OR low HP threshold, any damage source.
- **Angry**: enemy died within witness radius, or Scared timer expiry → ignores danger, faster, hits harder; expires to Aware.
- **Precedence** Angry > Scared > Suspicious > Aware; while Angry, Scared triggers ignored; new Angry trigger refreshes timer.
- **Anti-cycle latch**: low-HP Scared fires once; re-arms only when HP climbs back above threshold.
- All triggers **event-edge** (damage events, witness events, timer expiry) — never continuous HP polling.

## Acceptance criteria

- [ ] Grabbing an enemy makes nearby enemies Suspicious (visibly slower); expires back to Aware
- [ ] Burst damage / low HP → Scared: flees to Base, escapes with the reduced drop (existing reader path)
- [ ] Scared timer expiry → Angry; witnessing a death → Angry; Angry expiry → Aware
- [ ] Precedence and latch behave per spec (unit tests on trigger/precedence/latch logic)
- [ ] All radii/timers/thresholds are ConfigHub knobs (initial guesses from DesignDoc › Config knobs)

## Blocked by

- Code-hygiene renames (001)
