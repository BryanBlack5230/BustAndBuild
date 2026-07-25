---
id: 203
title: Main-menu live background
area: World
status: open
assignee:
blocked-by: [202]
---

## What to build

The main menu's static blockout background is replaced by the **last-played world rendered live at Map framing** — the deity hovering over their island before descending. Ambient sim only (day/night light, idle villagers); no gameplay input reaches the world; menu UI sits on top. Fresh install (no world yet) falls back gracefully to a static background. Entering the world from the menu should feel like the descend, not a hard scene cut.

## Acceptance criteria

- [ ] Menu shows last-played world at Map framing, live/ambient
- [ ] No world yet → graceful static fallback
- [ ] Menu input never leaks into the world; perf cost acceptable at menu idle
- [ ] Continue transitions from the background into play without a jarring cut

## Blocked by

- Multi-world saves + switcher (202)
