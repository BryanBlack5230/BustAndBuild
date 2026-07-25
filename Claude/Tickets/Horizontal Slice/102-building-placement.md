---
id: 102
title: Building placement
area: City
status: open
assignee:
blocked-by: [101]
---

## What to build

The real placement system (free placement, not sockets): build menu → **ghost preview → proximity/validity ring → place**, paying resource costs from the Wallet. Building definitions are ScriptableObjects per ADR-0004. First two catalog entries prove both validity paths: **housing** (free placement, no node) and **woodcutter** (requires tree-node proximity for its gatherers). Placed buildings become durable world state — the surface Wealth (108) and the save payload (109) read.

## Acceptance criteria

- [ ] Build menu in City framing; picking a building shows a cursor-following ghost with a validity ring
- [ ] Invalid placement (gatherer too far from its node, overlap) visibly blocks
- [ ] Placing deducts SO-defined costs; insufficient funds blocks with feedback
- [ ] Housing + woodcutter definition SOs, designer-editable without code
- [ ] Placed buildings registered as durables in world state (queryable by Wealth/save)

## Blocked by

- City scene bootstrap (101)
