---
id: 006
title: Defense shop UI
band: Core
status: open
assignee:
blocked-by: [005]
---

## What to build

The between-Days **shop**: opens at Day end (with the free heal/rebuild), spends Pearls at flat prices. First sellable line: a structure into an empty/destroyed Socket — a **hard** purchase (applies at Day start; the Socket's geometry already fixed what it will be, no wall-vs-tower choice). Sealing all Sockets for the first time is a felt progression moment.

Built as an extensible surface: staffing slots (ticket 008) and tar barrels (ticket 022) add purchase lines later without rework.

## Acceptance criteria

- [ ] Shop opens between Days only; no purchases mid-Day
- [ ] Structure purchase costs Pearls, fills the Socket at next Day start
- [ ] UI shows each Socket's state (filled / empty / bought-pending)
- [ ] Insufficient Pearls = disabled affordance, not an error
- [ ] Wallet integration uses the existing currency service

## Blocked by

- Sockets + perimeter structures + empty-socket breach (005)
