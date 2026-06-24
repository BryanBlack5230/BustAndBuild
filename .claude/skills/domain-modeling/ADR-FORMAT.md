# ADR Format

ADRs live in `Claude/docs/adr/` and use sequential numbering: `0001-slug.md`, `0002-slug.md`, etc.

Create the `Claude/docs/adr/` directory lazily — only when the first ADR is needed. (It already
exists in this repo as of ADR-0001.)

## Template

```md
# {Short title of the decision}

{1-3 sentences: what's the context, what did we decide, and why.}
```

That's it. An ADR can be a single paragraph. The value is in recording *that* a decision was made
and *why* — not in filling out sections.

## Optional sections

Only include these when they add genuine value. Most ADRs won't need them.

- **Status** frontmatter (`proposed | accepted | deprecated | superseded by ADR-NNNN`) — useful
  when decisions are revisited
- **Considered Options** — only when the rejected alternatives are worth remembering
- **Consequences** — only when non-obvious downstream effects need to be called out

## Numbering

Scan `Claude/docs/adr/` for the highest existing number and increment by one.

## When to offer an ADR

All three of these must be true:

1. **Hard to reverse** — the cost of changing your mind later is meaningful
2. **Surprising without context** — a future reader will look at the code and wonder "why on
   earth did they do it this way?"
3. **The result of a real trade-off** — there were genuine alternatives and you picked one for
   specific reasons

If a decision is easy to reverse, skip it — you'll just reverse it. If it's not surprising, nobody
will wonder why. If there was no real alternative, there's nothing to record.

### What qualifies (Unity-flavoured)

- **Architectural shape.** "Battle simulation is ECS/DOTS; UI and scene-flow are MonoBehaviour."
  "Services are wired through Reflex DI, not singletons."
- **Cross-system integration patterns.** "Systems communicate state changes via the EventBus, not
  direct references." "Requests go through the CommandDispatcher."
- **Technology choices that carry lock-in.** Reflex, Odin, the DOTS stack, the save backend — the
  ones that would take weeks to swap, not every package.
- **Boundary and scope decisions.** "Gameplay/ owns scene-bound code; Infrastructure/ owns
  framework services; nothing in Infrastructure references Gameplay." The explicit no-s matter.
- **Deliberate deviations from the obvious path.** "We poll X instead of subscribing because Y."
  Anything where a reasonable reader would assume the opposite, so nobody 'fixes' it later.
- **Constraints not visible in the code.** Performance budgets, platform limits, design pillars.
- **Rejected alternatives when the rejection is non-obvious.** Otherwise someone re-proposes them.

---
*Fork of mattpocock `domain-modeling` (ADR-FORMAT) — see ADR-0001.*
