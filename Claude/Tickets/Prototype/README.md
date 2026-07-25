# Prototype tickets

Execution tickets to finish the **Prototype scope** ("Battle depth", DesignDoc.md). Created 2026-07-13 from DesignDoc "Existing vs new" + TaskBreakdown state + wayfinder decisions (tickets 002–010). Conventions mirror `Claude/wayfinder/README.md` (frontmatter, claim-before-work, refer by title).

**Work the frontier:** any `status: open` ticket whose `blocked-by` ids are all closed. Clear context between tickets (`/implement` style).

**Bands** (cut order at checkpoint 2026-10-09 / expiry 2027-01-09: Trim first, then Depth bottom-up; Core never; Shell outranks content):
Prefactor → Core → Shell → Depth → Trim.

| id | Ticket | Band | Blocked by |
|----|--------|------|-----------|
| 001 | Code-hygiene renames | Prefactor | — |
| 002 | Enemy emotion writers | Core | 001 |
| 003 | Emotion telegraphs | Core | 002 |
| 004 | Wave assembler + Danger curve | Core | — |
| 005 | Sockets + perimeter structures + empty-socket breach | Core | — |
| 006 | Defense shop UI | Core | 005 |
| 007 | Special buildings | Core | — |
| 008 | Staffing + assignment transfer | Core | 006, 007 |
| 009 | Ally emotion writers + never-die clamp | Core | 002, 008 |
| 010 | Heal Rings | Core | 009 |
| 011 | Between-Days checkpoint save | Shell | 006, 008 |
| 012 | Main menu shell | Shell | 011 |
| 013 | Slow-mo pause | Shell | 012 |
| 014 | Core-verb SFX | Core | — |
| 015 | PowerHit finish | Depth | — |
| 016 | Mounts | Depth | 005, 008 |
| 017 | Guard role behaviors | Depth | 008 |
| 018 | Digger + Tunnel | Depth | 004 |
| 019 | Water Dunk rule | Depth | — |
| 020 | Repairman AI | Trim | 008 |
| 021 | Shaman | Trim | 004 |
| 022 | Tar barrel | Trim | 006 |

HITL note: Claude is read-only on `.unity`/`.prefab` — tickets flagged **HITL** need Bryan to author scene/prefab changes; Claude preps everything else + gives placement specs.
