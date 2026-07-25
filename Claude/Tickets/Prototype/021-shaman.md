---
id: 021
title: Shaman
band: Trim
status: open
assignee:
blocked-by: [004]
---

## What to build

The enemy healer special (DesignDoc › Special enemies › Shaman), introduced at D6: squishy backline, single-target heal **cast on cooldown**, healing a **random amount (min–max config)**; target = lowest-HP Enemy Unit in range (default, config knob). Uses/extends the heal path in the health pipeline (shared with Heal Rings — whichever lands first builds it).

Includes the D6 preset + staged-intro wiring. Playtest watch (parked): Shaman healing a submerged Digger = sustain fortress combo.

## Acceptance criteria

- [ ] Shaman stays backline and casts heal on cooldown at the lowest-HP enemy in range
- [ ] Heal amount random within config min–max; cooldown + range config
- [ ] Squishy profile: dies fast to focused ally damage / a good flick
- [ ] D6 preset introduces Shamans via the assembler

## Blocked by

- Wave assembler + Danger curve (004)
