---
id: 001
title: Code-hygiene renames
band: Prefactor
status: open
assignee: Claude
blocked-by: []
---

## What to build

Codebase vocabulary matches DesignDoc before new systems build on it: `Emotion.Normal` → `Emotion.Aware` (ubiquitous language, DesignDoc › Emotion system), and `TunnelTeleporter` → `OverlapEjector` (it's a throw-release overlap resolver; "Tunnel" becomes a domain term owned by the Digger).

## Acceptance criteria

- [x] No reference to `Emotion.Normal` or `TunnelTeleporter` remains anywhere (code, configs, OVDF, docs that claim to describe current code)
- [ ] Project compiles, existing tests green (unverified — no Unity CLI/editor available this session; grep sweep confirms no leftover identifiers, need in-editor confirm)
- [x] CONTEXT.md / learnings updated if they name the old identifiers

## Blocked by

None — can start immediately.
