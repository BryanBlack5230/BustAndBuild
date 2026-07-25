---
id: 022
title: Tar barrel
band: Trim
status: open
assignee:
blocked-by: [006]
---

## What to build

The one prototype throwable (DesignDoc › Throwable: tar barrel): bought between Days in the shop (cheap, stock cap ~3–5), racked physically near the Beacon — no supply-selector UI, throwables are physical (grab & throw, the universal verb). **Shatters on first hard contact** (no bouncing, light impact damage, no multiplier) → **oil puddle ~10s** applying a big move-speed debuff to **all** ground Units crossing — symmetric; oiling your own gate is a legitimate mistake. Puddle under a tunnel exit = welcome mat.

Barrel stock joins the checkpoint payload (ticket 011). Last-standing Trim item — cut last.

## Acceptance criteria

- [ ] Shop sells barrels up to the stock cap; bought barrels appear racked near the Beacon next Day
- [ ] Grab/throw works like any Grabbable; shatters on first hard contact, no bounce, no multiplier
- [ ] Puddle slows every ground Unit crossing it, both factions, for its config lifetime
- [ ] Stock survives the between-Days checkpoint

## Blocked by

- Defense shop UI (006)
