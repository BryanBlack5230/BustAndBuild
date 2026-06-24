---
name: knowledge-save
description: Triggered by "what have you learned?" — analyzes the current conversation and saves worthy technical discoveries, patterns, and project-specific knowledge to Claude/learnings/ for future reuse.
---

# Knowledge Save Skill

## Trigger Phrase
`what have you learned?` (or close variants like "save what you learned", "save learnings")

## Purpose

Extract durable, reusable knowledge from the current conversation and persist it to `Claude/learnings/`. This reduces token usage in future sessions by giving Claude a fast reference instead of re-deriving facts from scratch.

## What To Save

Save things that are **non-obvious from reading the code alone** and that a future Claude instance would benefit from knowing upfront:

| Worth saving | Not worth saving |
|---|---|
| Discovered system behaviors or quirks | Things already obvious from class/method names |
| Bugs found + root cause | Current conversation task state |
| Architecture decisions + rationale | Code that already explains itself |
| Project-specific patterns not in CLAUDE.md | Things already in unity-coding-standards skill |
| Integration gotchas (3rd party libs) | Ephemeral debug output |
| Performance constraints discovered | Git history / who changed what |
| Data flow between systems | Things already in memory/ files |

## File Structure in Claude/learnings/

Organize by topic, not by date. Use kebab-case filenames.

```
Claude/learnings/
├── README.md                  ← index (one line per file) — keep in sync
├── scene-flow-system.md       ← how scene loading / ISceneFlow works
├── unity-physics-gotchas.md   ← AABB / baking / collision-layer gotchas
├── di-architecture.md         ← Reflex installers, container chain, NonLazy
├── input-system.md            ← grab/throw, cursor, camera-drag quirks
└── ...
```

One file per system/topic. **Prefer updating an existing file** over creating a new one.

## File Format

Each file should have a short header and use `##` sections:

```markdown
# <Topic Name>

## <Discovery Title>
**Context:** What was being worked on.  
**Finding:** The concrete fact or pattern.  
**Why it matters:** How this saves future effort.

## <Another Discovery>
...
```

Keep entries tight — 3–6 lines each. Code snippets are fine when the pattern isn't obvious from prose.

Cross-link related files with `[[file-stem]]` wikilinks (e.g. `[[design-heuristics]]`, `[[currency-and-saves]]`) — the existing files use these for navigation. Link liberally, even to a file that doesn't exist yet; it marks something worth writing later.

## Process (Step by Step)

1. **Scan the conversation** for technical findings: bugs fixed, patterns used, system behaviors explained, gotchas hit, decisions made with rationale.
2. **Filter** — only keep facts a future Claude would actually use. Skip one-off trivia.
3. **Check existing files** in `Claude/learnings/` — update rather than duplicate.
4. **Write or update** files using the format above.
5. **Update the index & routing.** Add or refresh the file's one-line entry in `Claude/learnings/README.md`. If you created a **new** file (not just updated one), also add a row to the routing table in `CLAUDE.md` (the "Learnings Routing" section) so future sessions load it for the matching task — an unindexed learning is one nobody finds.
6. **Report back** to the user: list what was saved and to which file(s), in one short paragraph.

## Output to User

After saving, report concisely:
> Saved N learnings to `Claude/learnings/<file>.md` — [one sentence summary of what was captured].

Do not repeat the full content back to the user.
