---
id: 013
title: Slow-mo pause
band: Shell
status: open
assignee:
blocked-by: [012]
---

## What to build

Lean pause menu (Resume · Options · Main Menu) over a **slow-mo backdrop**: world ticks at a tiny timescale under the menu, gameplay input off (existing OnPause unregister pattern) — not bullet-time. Replaces the interim hard game-loop-group toggle.

- Pause mid-grab **force-releases through the normal release path with zero throw velocity** (gentle drop, no multiplier).
- A charging PowerHit cancels (criterion active once ticket 015 lands).
- Main Menu mid-Day = the checkpoint revert (ticket 011 rule).

## Acceptance criteria

- [ ] Pause: world visibly ticks at slow-mo config timescale, all gameplay input dead
- [ ] Resume restores normal timescale + input
- [ ] Pause mid-grab drops the body gently via the normal release path (no multiplier)
- [ ] Options panel reused from ticket 012; Main Menu returns and triggers the mid-Day revert
- [ ] Hard game-loop toggle removed from the player path

## Blocked by

- Main menu shell (012)
