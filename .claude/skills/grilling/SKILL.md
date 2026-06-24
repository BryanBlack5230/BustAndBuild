---
name: grilling
description: Interview the user relentlessly about a plan or design. Use when the user wants to stress-test a plan before building, or uses any 'grill' trigger phrases.
---

Interview me relentlessly about every aspect of this plan until we reach a shared understanding.
Walk down each branch of the design tree, resolving dependencies between decisions one-by-one. For
each question, provide your recommended answer.

Ask the questions one at a time, waiting for feedback on each question before continuing. Asking
multiple questions at once is bewildering.

If a question can be answered by exploring the codebase, explore the codebase instead — and in this
project, **start with the knowledge that's already written down**: `Claude/learnings/` (the routing
table in `CLAUDE.md` maps task → learning), `Claude/Project.md`, and the `CONTEXT.md` glossary if it
exists. Don't re-derive what a learning already records.

Use the glossary's exact terms in your questions. If you need a domain term that isn't defined yet,
that's a signal — either you're inventing language the project doesn't use (reconsider), or there's
a real gap (hand it to `domain-modeling` to sharpen).

---
*Fork of mattpocock `grilling` — see ADR-0001.*
