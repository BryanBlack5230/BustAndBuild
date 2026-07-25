---
id: 109
title: World save + auto/manual save
area: Saves
status: open
assignee:
blocked-by: [102, 103]
---

## What to build

The proto between-Days checkpoint (proto ticket 011) grows into the **single-world full save**: payload adds population + assignments, placed buildings, all six stockpiles, day number, **last-seen timestamps** (the offline calc's input, 110), and beacon repair-timer state (TaskBreakdown 1.3's missing fields). **Auto-save every 10 min + manual Save on the pause menu.** Same `ISaveSystem` seam, versioned format. Multi-world/switcher stays Full Game.

## Acceptance criteria

- [ ] Payload round-trips: population + assignments, buildings, stockpiles, day index, timestamps, beacon timer
- [ ] Auto-save every 10 min (config) + manual Save in pause menu
- [ ] Kill the app outside battle → relaunch restores city + battle durable state
- [ ] Version bump handled: proto-era checkpoint loads gracefully (migrate or clean fresh world, no crash)
- [ ] Serialize/deserialize round-trip unit tests

## Blocked by

- Building placement (102)
- Villager population (103)
