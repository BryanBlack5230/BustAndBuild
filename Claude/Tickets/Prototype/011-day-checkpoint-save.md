---
id: 011
title: Between-Days checkpoint save
band: Shell
status: open
assignee:
blocked-by: [006, 008]
---

## What to build

The one prototype save (DesignDoc › Prototype shell, wayfinder ticket 003): a **single-slot real-disk backend behind the existing save seam** (`ISaveSystem` replaces the dummy). Written twice per cycle: at **shop-open** (Day end) and at **Day-start** (locks shopping in). Payload = day index + Pearls + socket layout + staffing + barrel stock (barrels join when ticket 022 lands; structures heal free between Days, allies reset — payload stays HP-free).

Quit or Main-Menu mid-Day = revert to the Day-start checkpoint; quit-scumming accepted at proto scale. No manual save. "Quit during battle = Beacon destroyed" stays deferred to HS.

## Acceptance criteria

- [ ] Kill the app mid-Day → relaunch continues from the Day-start checkpoint
- [ ] Quit during shop → at most the shop is redone (shop-open write survived)
- [ ] Payload round-trips: day index, Pearls, socket layout, staffing
- [ ] Save format versioned (existing save-data version pattern)
- [ ] Unit tests on serialize/deserialize round-trip

## Blocked by

- Defense shop UI (006)
- Staffing + assignment transfer (008)
