# Horizontal Slice tickets

Execution tickets to finish the **Horizontal Slice scope** ("One game, all systems", DesignDoc.md). Created 2026-07-19 from DesignDoc HS + Visuals sections, TaskBreakdown gaps, and the Prototype ticket set. Conventions mirror `Claude/wayfinder/README.md` (frontmatter, claim-before-work, refer by title). Ids start at **101** so they never collide with Prototype ids.

**Precondition:** Prototype story test PASSed. All Prototype **Core** tickets assumed done; Depth/Trim may have been cut at box close — HS tickets touching one say so and carry a fallback.

**No cut bands** — HS wasn't grilled with a cut policy. Every ticket serves one of the three exit gates (coupling test · no dead ends · perf checkpoint). `area` is grouping only. **Work the frontier:** any `status: open` ticket whose `blocked-by` ids are all closed; clear context between tickets (`/implement` style).

| id | Ticket | Area | Blocked by |
|----|--------|------|-----------|
| 101 | City scene bootstrap | City | — |
| 102 | Building placement | City | 101 |
| 103 | Villager population | City | 102 |
| 104 | Rate-based income + beacon-buff growth | City | 102, 103 |
| 105 | Villager battle staffing + dismissal panel | City | 103, 104 |
| 106 | Altar + Faith + lightning | City | 104 |
| 107 | Cursor-reactive villagers | City | 103 |
| 108 | Wealth → Danger director | Director | 102, 103 |
| 109 | World save + auto/manual save | Saves | 102, 103 |
| 110 | Offline progression | Saves | 104, 108, 109 |
| 111 | Battle-quit loss + offline repair timer | Saves | 109, 110 |
| 112 | Unattended battle sim | Cross-scene | 101 |
| 113 | Map live view | Cross-scene | 112 |
| 114 | Contextual cursor + hover detail | UI | — |
| 115 | Damage-reactive HP bars | UI | — |
| 116 | Shell odds: scene beds, destruction SFX, resolution list | Shell | — |
| 117 | Look-dev spike | Visuals | — |
| 118 | Visual Companion system | Visuals | 117 |
| 119 | Emotion presentation: eyes + posture | Visuals | 118 |
| 120 | Map-height LOD | Visuals | 118 |
| 121 | Water render stack + Dunk presentation | Visuals | 117 |
| 122 | Blockout silhouette pass | Visuals | 102, 103 |
| 123 | HS exit: perf budget + checkpoint + itch build | Exit | 104, 105, 110, 112, 113, 118 |

**Out of HS scope** (stays where DesignDoc put it): FMOD trial spike (HS→EA boundary, when dynamic-music work starts) · multi-world + switcher + menu live background · proc-gen island · deity transition FX · socket tiers · gibberish audio vocalizations · offline edge-case QA (1 day/week/month) · production art sprints (post-HS, toward EA bar).

HITL note: Claude is read-only on `.unity`/`.prefab` — tickets flagged **HITL** (101, 122, plus any prefab authoring inside others) need Bryan to author scene/prefab changes; Claude preps everything else + gives placement specs.
