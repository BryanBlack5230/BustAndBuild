---
id: 009
title: Ally emotion writers + never-die clamp
band: Core
status: open
assignee:
blocked-by: [002, 008]
---

## What to build

Allies get their emotion writers per DesignDoc › Emotion system › Ally table, sharing the trigger/timer/latch infrastructure from ticket 002:

- **Scared**: entry via **Courage** formula — HP < 10%/2^n (n = non-Scared allies within radius; floor 1%). Effects: invulnerable, flees to its **home building** (assignment from ticket 008), heals there (heal itself is ticket 010).
- **Angry**: ally within radius became Scared → hits harder, expires to Aware.
- **Allies never die**: lethal damage clamps to 1 HP + force-Scared — overrides Angry precedence and Courage. Punishment is tempo, not loss.

## Acceptance criteria

- [ ] Lethal damage on any ally clamps to 1 HP and force-enters Scared, even while Angry
- [ ] Courage: same damage scares a lone ally but not one in a group (unit tests on threshold math)
- [ ] Scared ally is invulnerable and flees to its home Special building
- [ ] Nearby ally turning Scared → witness turns Angry, expires to Aware
- [ ] Telegraphs (ticket 003 system) show ally states with zero extra work
- [ ] All knobs in ConfigHub

## Blocked by

- Enemy emotion writers (002)
- Staffing + assignment transfer (008)
