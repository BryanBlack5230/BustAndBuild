---
id: 103
title: Villager population
area: City
status: open
assignee:
blocked-by: [102]
---

## What to build

The population engine of the Wealth-crash-and-recover loop, Timberborn-style: **hire villagers with Pearls** at the main building; cap = Σ housing × 2 (no breeding, no aging). **Jobless villagers idle/wander near the main building** until grab-placed — auto-assignment is dead (wayfinder ticket 010). **Food is upkeep:** population drains the food stockpile at a config rate; food < pop → starvation deaths. Only starvation kills villagers — battle deaths impossible (allies never die).

## Acceptance criteria

- [ ] Hire flow: pay Pearls → villager spawns; blocked at cap; placing housing raises cap
- [ ] Jobless villagers idle/wander near the main building; grabbable like any ally
- [ ] Food drains by population; at 0 food villagers die at a config cadence
- [ ] Villagers default to Aware; emotion tables apply unchanged
- [ ] Population is durable world state (Wealth + save surfaces)

## Blocked by

- Building placement (102)
