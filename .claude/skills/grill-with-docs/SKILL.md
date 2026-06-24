---
name: grill-with-docs
description: A relentless interview to sharpen a plan or design, which also creates docs (ADRs + glossary) as we go.
disable-model-invocation: true
---

Run a `/grilling` session, using the `/domain-modeling` skill to capture what crystallises.

As decisions and terms resolve, write them down **immediately** through `domain-modeling`'s three
sinks — don't batch the capture:

- a resolved **glossary term** → `CONTEXT.md` (repo root)
- a **hard-to-reverse, surprising, real-trade-off decision** → an ADR in `Claude/docs/adr/`
- **"how it works" / a gotcha** → *not here*; route it to `knowledge-save` → `Claude/learnings/`

---
*Fork of mattpocock `grill-with-docs` — see ADR-0001.*
