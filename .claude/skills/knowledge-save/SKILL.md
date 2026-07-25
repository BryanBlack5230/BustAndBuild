---
name: knowledge-save
description: Save durable discoveries from the current conversation into Claude/learnings/ — triggered by "what have you learned?" / "save learnings". Also prunes the knowledge base — triggered by "reorganize learnings" — dedupe, merge, split, re-index.
---

# Knowledge Save Skill

Persists durable, reusable knowledge to `Claude/learnings/` so future sessions load facts instead of re-deriving them. Two branches: **Save** (default — "what have you learned?", "save learnings") and **Reorganize** ("reorganize learnings").

## What Belongs Here

Save what is **non-obvious from reading the code alone** and useful to a future session:

| Worth saving | Not worth saving |
|---|---|
| Discovered system behaviors or quirks | Things already obvious from class/method names |
| Bugs found + root cause | Current conversation task state |
| Project-specific patterns not in CLAUDE.md | Things already in unity-coding-standards skill |
| Integration gotchas (3rd-party libs) | Ephemeral debug output |
| Tool/CLI configs and workarounds | Git history / who changed what |
| Performance constraints discovered | Things already in memory/ files |
| Data flow between systems | |

**Wrong sink — route, don't save here:** term meanings belong in `CONTEXT.md`; decisions with real trade-offs belong in `Claude/docs/adr/`. For either, offer `/domain-modeling` instead of writing a learning.

## File Structure

Organize by topic, not by date: kebab-case filenames, one file per system/topic (e.g. `scene-flow-system.md`, `unity-physics-gotchas.md`, `di-architecture.md`). `README.md` is the index — one line per file, kept in sync. **Prefer updating an existing file** over creating a new one.

## Entry Format

```markdown
# <Topic Name>

## <Discovery Title> — YYYY-MM-DD
**Context:** What was being worked on.
**Finding:** The concrete fact or pattern.
**Why it matters:** How this saves future effort.
```

Keep entries tight — 3–6 lines; code snippets only when prose can't carry the pattern. The date is what lets Reorganize judge staleness later (existing undated entries count as oldest).

Cross-link related files with `[[file-stem]]` wikilinks (e.g. `[[design-heuristics]]`, `[[currency-and-saves]]`). Link liberally, even to a file that doesn't exist yet — it marks something worth writing later.

## Save — "what have you learned?"

1. **Scan the conversation** for technical findings: bugs fixed, patterns used, system behaviors explained, gotchas hit.
2. **Filter** by the table above; route wrong-sink items instead of saving them.
3. **Check existing files** in `Claude/learnings/` — update rather than duplicate.
4. **Write or update** files in the entry format.
5. **Index & routing.** Refresh each touched file's one-line entry in `Claude/learnings/README.md`; for each **new** file, add a row to the "Knowledge Routing" table in `CLAUDE.md`. Done when every touched file has a current README line and every new file has a routing row — an unindexed learning is one nobody finds.
6. **Report** in one short paragraph — files and counts, not content:
   > Saved N learnings to `Claude/learnings/<file>.md` — [one-sentence summary of what was captured].

## Reorganize — "reorganize learnings"

1. **Read every file** in `Claude/learnings/`, including `README.md`.
2. **Delete duplicates and outdated entries.** Dates mark the stale suspects; when unsure, verify against the current code before deleting.
3. **Merge** entries that belong together; **split** any file covering too many topics.
4. **Relocate wrong-sink entries** (see What Belongs Here) — flag glossary/ADR material for `/domain-modeling` rather than writing those sinks yourself.
5. **Rebuild the index.** Sync `README.md` and the CLAUDE.md "Knowledge Routing" table. Done when index lines, routing rows, and files on disk match one-to-one.
6. **Summarize what changed** — merged, split, deleted, relocated, with counts.
