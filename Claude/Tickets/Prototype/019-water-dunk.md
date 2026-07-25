---
id: 019
title: Water Dunk rule
band: Depth
status: open
assignee:
blocked-by: []
---

## What to build

Water becomes a real gameplay surface — own physics layer replacing the walk-on-water Ground collider (DesignDoc › Water: the Dunk rule, wayfinder ticket 007). Any Unit landing in the sea (thrown, PowerHit-shoved, knocked) is **Dunked**:

1. **Splash damage** = existing impact pipeline × `WaterImpactMultiplier` (config ~0.4). Lethal splash kills enemies normally — loot drops at the splash point and **sinks**; allies never die (standard clamp).
2. **Goes under on contact**: out of play — hidden, untargetable, ungrabbable, invulnerable, AI off — for `DunkDuration` (config ~2–3s).
3. **Reappears at the enemy spawn shore, both factions.** HP and emotion persist; no new emotion triggers.

Owned emergent rules: dunked Scared murloc surfaces at its Base → normal escape fires with **drop anchor reset to land** (never drops loot mid-sea); dunking your own guard = teleport-behind-lines (damage tax + walk-back self-limits). **Pickups sink — loot over water is lost** (wash-ashore = playtest contingency, not built). Trajectory predictor's impact circle already renders on water — aiming a Dunk is deliberate. Splash SFX included (core-verb family). Fallback if cut: today's walk-on-water.

## Acceptance criteria

- [ ] Landing in sea applies splash damage via the impact pipeline × the multiplier
- [ ] Dunked Unit fully out of play for DunkDuration, then reappears at the spawn shore, HP/emotion intact
- [ ] Dunked Scared enemy escapes at its Base with the escape drop anchored on land
- [ ] Pearls/pickups landing on water sink (removed)
- [ ] Both factions obey the same rules; allies take the never-die clamp
- [ ] Splash SFX fires on entry

## Blocked by

None — can start immediately.
