---
id: 111
title: Battle-quit loss + offline repair timer
area: Saves
status: open
assignee:
blocked-by: [109, 110]
---

## What to build

The two beacon save rules deferred from proto. **Quit during battle = Beacon destroyed** — loss state on next launch (replaces the proto's revert-to-Day-start; the quit-scumming window closes). **Beacon repair free timer lengthens to ~5 min** (config; proto's ≤30s retires) — a deliberate breather pushing the player into the City — and **ticks in real time incl. offline** (folds into 110's calc; a returning player usually finds it elapsed). Pearls skip it anytime, incl. mid-timer.

## Acceptance criteria

- [ ] Quit or kill mid-Day → next launch: Beacon destroyed, repair paths offered (no Day-start revert)
- [ ] Quit outside active battle unchanged (no loss)
- [ ] Free timer ~5 min config; keeps counting across quit + offline
- [ ] Pearl skip works mid-timer

## Blocked by

- World save + auto/manual save (109)
- Offline progression (110)
