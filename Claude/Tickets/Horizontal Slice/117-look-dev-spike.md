---
id: 117
title: Look-dev spike
area: Visuals
status: open
assignee:
blocked-by: []
---

## What to build

**Timeboxed 1–2 weeks, runs immediately after the prototype passes its story test.** One in-engine shot: greybox terrain + a handful of AI-gen sprites + the FULL visual stack — rotating sun, rim/backlight sprite shader, cross-quad silhouette shadows, planar water, post (bloom, dayPercent grading, per-scene DoF, depth haze) — at all three framings (ADR-0010). **It gates the Visual Companion build (118) and all production art.** Answers the parked question: does flat-lit painted art look alive, or do sprites need normal maps?

## Acceptance criteria

- [ ] One scene demonstrates the full stack at Battle, City, and Map framings
- [ ] A2C sprite-edge approach validated (or the plain-clip fallback chosen, recorded)
- [ ] Written verdict: flat diffuse ships / normal maps un-parked
- [ ] Timebox respected — spike closes ≤2 weeks with a verdict even if partial

## Blocked by

None — starts at HS kickoff.
