# CONTEXT.md Format

`CONTEXT.md` is the project glossary — the ubiquitous language. One file at the repo root.

## Structure

```md
# {Project / Context Name}

{One or two sentence description of what this context is and why it exists.}

## Language

**Unit**:
A single combatant spawned into a battle and driven by the AI pipeline.
_Avoid_: enemy, mob, agent

**Beacon**:
{one-or-two-sentence meaning}
_Avoid_: {synonyms to avoid}
```

> The examples above are **format illustrations** — replace them with this project's real terms
> (e.g. Beacon, Unit, WallSection, Pearl, Castle) as each is resolved. Don't assert a meaning
> you haven't confirmed with the user or the code.

## Rules

- **Be opinionated.** When multiple words exist for the same concept, pick the best one and list
  the others under `_Avoid_`.
- **Keep definitions tight.** One or two sentences max. Define what it IS, not what it does.
- **Only domain terms.** General programming concepts (pooling, timeouts, error types, DI) don't
  belong even if used heavily. The "how it works" side belongs in `Claude/learnings/`, not here.
  Before adding a term, ask: is this unique to the game's domain, or a general engineering concept?
  Only the former belongs.
- **Group terms under subheadings** when natural clusters emerge (e.g. *Combat*, *Economy*,
  *Structures*). A flat list is fine if all terms cohere.

## Single-context

This repo is single-context: one `CONTEXT.md` at the root, no `CONTEXT-MAP.md`. (Multi-context
handling from the upstream skill was not adopted.)

---
*Fork of mattpocock `domain-modeling` (CONTEXT-FORMAT) — see ADR-0001.*
