# 1.0 tickets

Execution tickets to finish the **Full Game scope** ("1.0", DesignDoc.md). Created 2026-07-19 from DesignDoc › Full Game + Deferred-to-1.0 systems + the Socket tiers block, TaskBreakdown gaps, and the Prototype/HS ticket sets. Conventions mirror `Claude/wayfinder/README.md` (frontmatter, claim-before-work, refer by title). Ids start at **201** so they never collide with Prototype (001+) or HS (101+) ids.

**Precondition:** HS exit gates passed (coupling test · no dead ends · perf checkpoint); HS build live on itch.io — the de-facto EA channel. Production art sprints live inside this set (222–224), toward the EA bar first, 1.0 second.

**No cut bands** — 1.0 wasn't grilled with a cut policy. The ship line is DesignDoc's: **compact roster complete + proc-gen + multi-world + controller verdict + localization + the endless plateau tuned.** `area` is grouping only. **Work the frontier:** any `status: open` ticket whose `blocked-by` ids are all closed; clear context between tickets (`/implement` style).

| id | Ticket | Area | Blocked by |
|----|--------|------|-----------|
| 201 | Proc-gen island | World | — |
| 202 | Multi-world saves + switcher | World | — |
| 203 | Main-menu live background | World | 202 |
| 204 | Socket tiers: core + soft effects | Defense | — |
| 205 | Socket tiers: hard upgrades + Mount gating | Defense | 204 |
| 206 | New specials — design session | Roster | — |
| 207 | Special enemy A | Roster | 206 |
| 208 | Special enemy B | Roster | 206 |
| 209 | Special enemy C (stretch) | Roster | 206 |
| 210 | Group preset library ~40 | Roster | 207, 208 |
| 211 | Faith abilities to 3–5 | Roster | — |
| 212 | Throwables to 3–4 + supply production | Roster | — |
| 213 | City building catalog to ~10 | City | — |
| 214 | Physical hauling | City | — |
| 215 | Telemetry hooks | Tuning | — |
| 216 | Endless plateau tuning | Tuning | 205, 210, 211, 212, 214, 215 |
| 217 | Controller/Steam Deck experiment | Input | — |
| 218 | Onboarding | Shell | — |
| 219 | FMOD trial spike | Audio | — |
| 220 | Dynamic music + city ambient | Audio | 219 |
| 221 | Gibberish speech + SFX completion | Audio | 219 |
| 222 | Character art + animation sets | Art | — |
| 223 | Building + environment art | Art | — |
| 224 | VFX set + flavor anims | Art | — |
| 225 | Deity descend/ascend transition | Art | — |
| 226 | Localization | Ship | 218 |
| 227 | Offline edge-case QA | Ship | 202, 214 |
| 228 | 1.0 release gate | Ship | 216, 217, 222, 223, 224, 226, 227 |

**Deliberately absent** (DesignDoc says so): win state (pure endless) · tutorial mode / guided island · F2P/IAP anything · authored modules for socket tiers (shape ships empty — future sink headroom) · verified Steam Deck support if 217 fails (post-launch revisit).

HITL note: Claude is read-only on `.unity`/`.prefab` — art/audio tickets (219–225) and any prefab authoring are Bryan-heavy; Claude preps code, pipelines, and placement specs. 209 is optional by design (roster target "2–3 specials").
