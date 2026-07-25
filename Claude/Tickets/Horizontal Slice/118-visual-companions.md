---
id: 118
title: Visual Companion system
area: Visuals
status: open
assignee:
blocked-by: [117]
---

## What to build

The pooled Mono presentation layer per unit (ADR-0010), un-deferred now that skeletal 2D is the committed animation path: SpriteSkin companions, LateUpdate transform sync from ECS, **companion owns ALL presentation** — flip/lean, juice, emote icons + tint (the proto telegraph rendering migrates here), throw tumble. Sim stays pure ECS; the ECS damage-juice blocks that a companion supersedes retire.

## Acceptance criteria

- [ ] Units render via pooled companions synced from ECS; sim untouched
- [ ] Proto emote/tint telegraphs migrate onto companions — one system, both factions
- [ ] Pool churn-safe through spawn/death/Dunk at 50–100 units
- [ ] Superseded ECS juice blocks retired; no double presentation

## Blocked by

- Look-dev spike (117)
