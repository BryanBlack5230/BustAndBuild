---
name: domain-modeling
description: Build and sharpen THIS project's domain model — the glossary (CONTEXT.md) and decision records (Claude/docs/adr/). Use when pinning down domain terminology/ubiquitous language, recording an architectural decision, or when another skill (grill-with-docs) needs to maintain the domain model.
---

# Domain Modeling

Actively build and sharpen the project's domain model as you design. This is the *active*
discipline — challenging terms, inventing edge-case scenarios, and writing the glossary and
decisions down the moment they crystallise. (Merely *reading* `CONTEXT.md` for vocabulary is
the consume-side habit described in `Claude/docs/agents/domain.md` — that's not this skill.
This skill is for when you're *changing* the model.)

## What this skill owns — and what it doesn't

This project keeps knowledge in **three distinct sinks** (see `Claude/docs/adr/0001-adopting-mattpocock-skills.md`). Keep them separate:

- **`CONTEXT.md`** (repo root) — the **glossary**: what domain terms *mean*. This skill writes it.
- **`Claude/docs/adr/`** — **decisions** with genuine trade-offs. This skill writes them.
- **`Claude/learnings/`** — **how-it-works, gotchas, system maps**. The `knowledge-save` skill
  owns these — *not this skill*.

Litmus test: if a fact is "how the code behaves" or "a trap to avoid," it's a learning — route
it to `knowledge-save`. Don't smuggle implementation detail into `CONTEXT.md` or an ADR.

## File structure

This repo is **single-context**: one `CONTEXT.md` at the root + `Claude/docs/adr/` for decisions.

```
/
├── CONTEXT.md                                   ← glossary (create lazily, on first resolved term)
├── Claude/docs/adr/
│   ├── 0001-adopting-mattpocock-skills.md
│   └── 0002-….md
└── Assets/ …
```

Create files lazily — only when you have something to write. If no `CONTEXT.md` exists yet,
create it when the first term is resolved. `Claude/docs/adr/` already exists.

## Seeding the glossary

`CONTEXT.md` doesn't exist yet. Until it does, the project's **de-facto vocabulary** lives as
prose and ECS type names in `Claude/Project.md` (the Project Overview + the `Components/` ·
`Systems/` name lists — Castle, Beacon, WallSection, Health, Attack, Grabbed, Spawn…) and
`Claude/learnings/project-overview.md` (Game Concept). When first populating `CONTEXT.md`, mine
the canonical nouns from there — Castle, Beacon, WallSection, Unit, grab/throw, the Battle vs
City loop — then sharpen each (pick one word, list rejected synonyms under `_Avoid_`). **Read
those docs for vocabulary only — don't copy their architecture/how-it-works content; that's the
`learnings/` genre, not the glossary.**

## During the session

### Challenge against the glossary
When the user uses a term that conflicts with the existing language in `CONTEXT.md`, call it out
immediately. "Your glossary defines 'Beacon' as X, but you seem to mean Y — which is it?"

### Sharpen fuzzy language
When the user uses vague or overloaded terms, propose a precise canonical term. "You're saying
'enemy' — do you mean a spawned **Unit**, or the opposing player? Those are different things."

### Discuss concrete scenarios
When domain relationships are being discussed, stress-test them with specific scenarios that
probe edge cases and force the user to be precise about the boundaries between concepts.

### Cross-reference with code
When the user states how something works, check whether the code agrees. If you find a
contradiction, surface it: "Your code destroys the entity once it re-enters the base, but you
just said a retreating Unit returns and regroups — which is right?"

### Update CONTEXT.md inline
When a term is resolved, update `CONTEXT.md` right there — don't batch. Use the format in
[CONTEXT-FORMAT.md](./CONTEXT-FORMAT.md). `CONTEXT.md` is a **glossary and nothing else**:
totally devoid of implementation details. Not a spec, not a scratch pad. Term meanings only —
"how it works" belongs in `Claude/learnings/`.

### Offer ADRs sparingly
Only offer to create an ADR when **all three** are true:

1. **Hard to reverse** — the cost of changing your mind later is meaningful
2. **Surprising without context** — a future reader will wonder "why did they do it this way?"
3. **The result of a real trade-off** — there were genuine alternatives and you picked one

If any is missing, skip the ADR. Use the format in [ADR-FORMAT.md](./ADR-FORMAT.md). ADRs live in
`Claude/docs/adr/`; scan it for the highest number and increment.

---
*Fork of mattpocock `domain-modeling` — see ADR-0001.*
