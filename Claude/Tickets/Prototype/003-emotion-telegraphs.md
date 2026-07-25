---
id: 003
title: Emotion telegraphs
band: Core
status: open
assignee:
blocked-by: [002]
---

## What to build

Every Unit's emotional state is readable in play without gizmos: floating emote icon + subtle body tint per state (Rimworld-style mood marks), **one system for both factions**. DesignDoc: "The story test fails if testers can't see the drama." Debug gizmo text stays dev-only. (HS upgrades to eyes + posture — out of scope.)

## Acceptance criteria

- [ ] Each non-Aware state shows a distinct emote icon + tint, applied/removed on state change
- [ ] Works for enemies now and allies automatically once ally writers land (faction-agnostic)
- [ ] Readable at normal battle zoom with 20+ units on screen
- [ ] No per-frame allocation; icons pool

## Blocked by

- Enemy emotion writers (002)
