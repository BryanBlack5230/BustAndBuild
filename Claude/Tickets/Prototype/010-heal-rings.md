---
id: 010
title: Heal Rings
band: Core
status: open
assignee:
blocked-by: [009]
---

## What to build

Every Special building projects a **Heal Ring** — a visible ground-decal radius slowly healing **any** damaged ally inside (universal, not class-gated; towers and Castle project nothing). Completes the Scared arc end-to-end (DesignDoc › Heal Rings, ticket 009-wayfinder):

- **Unconditional tick** — heals mid-fight too; purely positional (pauses outside any ring, resumes inside, never resets). First heal path in the health pipeline.
- **Scared arc emergent:** flee home → stand in ring → heal to full → Aware → return to post.
- **Scared-at-home = anchored kiting:** avoid-danger steering anchored to the home building, staying inside the ring.
- **Grab = triage:** on release a Scared ally re-evaluates — HP ≥ release-exit knob (~50%) → Aware, drops invulnerability; below → stays Scared, resumes fleeing home.
- **Day end: all allies full heal + reset to Aware** (checkpoint payload stays HP-free).

Visual = simple ring decal (PowerHit-telegraph style).

## Acceptance criteria

- [ ] Damaged ally inside a ring heals at the config rate; healing pauses outside, resumes inside, never resets
- [ ] Full Scared arc: flee → ring → full heal → Aware → returns to post
- [ ] Anchored kiting when threatened at home (stays in ring, scurries)
- [ ] Release re-eval at the ~50% knob works both ways
- [ ] Day end full-heals + Aware-resets every ally
- [ ] Ring radius / heal time-to-full / release-exit knob in ConfigHub

## Blocked by

- Ally emotion writers + never-die clamp (009)
