---
id: 107
title: Cursor-reactive villagers
area: City
status: open
assignee:
blocked-by: [103]
---

## What to build

The deity fantasy read: villagers near the cursor react — **speech clouds with gibberish text** and prayer poses. Speech-cloud rendering system (world-anchored, pooled — UiPool pattern). Poses at blockout tier = simple sprite/emote-level; full posture animation rides the Visual Companion track (118/119). No real language ever (fiction: the deity can't understand them); gibberish **audio** vocalizations stay deferred to 1.0.

## Acceptance criteria

- [ ] Villagers within a config radius of the cursor show clouds/poses; fade on leave
- [ ] Gibberish text only
- [ ] Cloud rendering pooled and perf-safe at village scale
- [ ] Pure presentation — no gameplay effect, no emotion triggers

## Blocked by

- Villager population (103)
