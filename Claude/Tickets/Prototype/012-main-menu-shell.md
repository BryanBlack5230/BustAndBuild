---
id: 012
title: Main menu shell
band: Shell
status: open
assignee:
blocked-by: [011]
---

## What to build

Near-HS main menu replacing the GameManager debug HUD (DesignDoc › Prototype shell): **Continue — Day N · New Run · Options · Feedback · Quit**, static blockout background.

- New Run over an existing checkpoint → confirm panel: *"This ends your current run — Day N reached."* The confirm **is** the run's end screen (endless soft-fail has no other end). No best-day persistence.
- Options (shared panel, reused by pause): master volume slider + windowed/fullscreen toggle. No resolution list.
- Feedback = in-build button opening an external form URL.

## Acceptance criteria

- [ ] Boot lands on the main menu; debug HUD gone from the player path
- [ ] Continue shows the checkpoint's Day N and resumes it; hidden/disabled with no checkpoint
- [ ] New Run over a checkpoint shows the wipe-confirm with Day N; confirm wipes and starts fresh
- [ ] Volume + fullscreen apply and persist
- [ ] Feedback opens the external URL; Quit exits cleanly

## Blocked by

- Between-Days checkpoint save (011)
