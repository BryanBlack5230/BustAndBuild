---
name: grilling
description: Interview the user relentlessly about a plan or design. Use when the user wants to stress-test a plan before building, or uses any 'grill' trigger phrases.
---

Interview me relentlessly about every aspect of this plan until we reach a shared understanding.
Walk down each branch of the design tree, resolving dependencies between decisions one-by-one. For
each question, provide your recommended answer.

Ask the questions one at a time, waiting for feedback on each question before continuing. Asking
multiple questions at once is bewildering.

## Before grilling: gather inputs first

Don't ask "Plan A or B?" before you know the baseline — that's planning on sand. Fix these before
the first decision question:

- **Scale** — real runtime numbers, not "scope": how many entities/objects at once, what
  frequencies, what budgets.
- **Tech in play** — anything beyond the default stack this touches (a specific DOTS feature,
  Burst/jobs constraints, a third-party package). Know its gotchas or research them first.
- **Prior attempts** — if the feature already exists and is being redone, what was tried and *why it
  was rejected*. Skip this and you'll re-propose a dead end.
- **Hard constraints** — what can't change: public API, config/SO format, perf baseline, IL2CPP/Burst
  limits.

The cheapest source is what's already written (see the next paragraph) — read it before asking. For
inputs not on paper, this is the **one** place you may batch: ask 2-4 in a single `AskUserQuestion`.
The grill loop itself stays one-at-a-time.

If an input surfaces *mid-grill* (a constraint you should've had eight questions in), that's a failed
pre-flight: **stop, name the gap, replan.** Don't retrofit earlier answers onto the new context.

If a question can be answered by exploring the codebase, explore the codebase instead — and in this
project, **start with the knowledge that's already written down**: `Claude/learnings/` (the routing
table in `CLAUDE.md` maps task → learning), `Claude/Project.md`, and the `CONTEXT.md` glossary if it
exists. Don't re-derive what a learning already records.

Use the glossary's exact terms in your questions. If you need a domain term that isn't defined yet,
that's a signal — either you're inventing language the project doesn't use (reconsider), or there's
a real gap (hand it to `domain-modeling` to sharpen).

## When to stop

Default: don't stop early. Keep going until the main question is covered on every axis:

- **Scope** — what's in, what's out.
- **API shape** — field names, types, config surface.
- **Lifecycle** — when it's created, destroyed, changes phase.
- **Edge cases** — interruptions, re-application, races, zero/negative values, empty/null.
- **Adjacent mechanics** — other sources of the same effect, the view / ECS-bridge side, save/load.
- **Change sites** — which systems/files get touched, in what order (system group, fixed vs update).
- **Risks** — what could break existing behavior, and how you'd diagnose it.

Stop only when the user says so ("enough" / "write the plan"), or every axis is closed *and* you see
no open branch left in the tree. Don't fear being tiresome — pulling the implicit assumptions out
*before* the plan is the whole job.

## Argue, don't nod

The worst failure here is being a yes-man. If you see a real problem in an answer, raise it — even if
pushback annoys. But argue **only** on a concrete contradiction (with research, the
learnings/`CONTEXT.md`, existing code, or the plan's own internal logic), and cite the file/line.
"I'd do it differently" isn't a reason — with no concrete objection, accept and move on. Between
questions, cross-check the last answer against what you know. If the user rejects a recommendation,
update and don't re-propose it.

---
*Fork of mattpocock `grilling` — see ADR-0001.*
