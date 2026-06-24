# Adopting and adapting the mattpocock skill pack

**Status:** accepted

We installed the mattpocock skill pack (~24 generic engineering skills) on top of an
already-opinionated Unity-DOTS workflow (custom `.claude/skills/`, a `Claude/learnings/`
knowledge DB, and gh-Issues agent docs under `Claude/docs/agents/`). Rather than use them
as-is, we decided **per skill** (no blanket rule) whether to vendor an adapted copy into
this repo, keep the generic/config-driven one global, distil it into a learning, or drop it.
The driving preference throughout: **light adaptation, minimal cross-wiring, keep skills
independently invocable** — invest only where it pays.

## Knowledge architecture — three sinks

The single most load-bearing decision. Each sink has a distinct shape and owner:

- **`CONTEXT.md`** (repo root) — glossary / ubiquitous language only, devoid of implementation.
  Created lazily by `domain-modeling` when the first real game-domain term is resolved.
- **`Claude/docs/adr/`** — decisions with genuine trade-offs (this file is `0001`). Written by
  `domain-modeling` / `grill-with-docs`, gated by the "hard to reverse + surprising + real
  trade-off" rule.
- **`Claude/learnings/`** — how-it-works + gotchas + system maps. Written by `knowledge-save`.
  Unchanged.

Stray "agreed-design" notes currently lodged in `learnings/` (e.g. distance-calc, design
heuristics) migrate to ADRs lazily, as they're next touched.

## Skill dispositions

| Skill(s) | Disposition |
|---|---|
| `domain-modeling` | **Vendor + adapt** — `CONTEXT.md` at root + ADRs in `Claude/docs/adr/`; inject the three-sink boundary (gotchas/system-maps redirect to `knowledge-save`→`learnings/`); keep the "offer ADRs sparingly" gate; ship `CONTEXT-FORMAT.md` + `ADR-FORMAT.md`. |
| `grilling`, `grill-with-docs` | **Vendor** — `grilling` lightly adapted to explore `learnings/` + the `CONTEXT.md` glossary first; `grill-with-docs` wired to the vendored `domain-modeling`. |
| `grill-me` | **Drop** — redundant; `/grilling` is directly invocable. |
| `diagnosing-bugs` | **Vendor light** — Phase-1 feedback-loop menu rewritten for Unity (minimal-scene play-mode repro w/ fixed timestep + seeded RNG; pure-function ECS NUnit pattern; `scene_inspect`; tagged `Log` probes). No hard-wired delegation; rely on auto-triggers for `dots-troubleshoot`/`scene-debug`. |
| `tdd` | **Vendor adapted + stand up a `Tests` asmdef** (and Editor-tests asmdef) per the recipe in `testing-methodology.md`. Makes the test seam real for the first time. |
| `codebase-design` | **Vendor as a skill** — Unity-reconciled: deep-module vocab applies to Infrastructure/Mono/Reflex (`ISaveSystem` seam = canonical); ECS half maps loosely. Keeps `DESIGN-IT-TWICE` / `DEEPENING`. |
| `prototype` | **Vendor adapted** — exploratory sibling of `grilling`; **non-Unity by default** (C# console for logic/state, HTML for UI/UX), living outside `Assets/` (`Claude/prototypes/<name>/`); mirror the real **seams** with **stub** adapters (cross-ref `codebase-design`). Answers design/logic/UX questions, **not** engine-fidelity. Scenes stay hand-authored — scaffold, don't fabricate. |
| `ask-matt` | **Adapt → project router** over the real toolbox (Unity skills + adapted mattpocock + caveman/etc.). |
| issue trio (`to-prd`/`to-issues`/`triage`), `setup-matt-pocock-skills` | **Global** — already driven by the adapted `Claude/docs/agents/` config; no vendoring. |
| `review` | **Global, experimental** — try its Spec axis (code vs originating gh issue); `code-review-unity` stays the day-to-day standards reviewer. |
| `handoff`, `resolving-merge-conflicts`, `teach`, `implement`, `writing-great-skills`, `improve-codebase-architecture`, `decision-mapping`, `writing-beats`/`-fragments`/`-shape` | **Global** — generic or off-domain; left available. `resolving-merge-conflicts` paired with a small learning: never hand-merge `.unity`/`.prefab` (read-only) → defer to UnityYAMLMerge / the user. |

## Considered options (the non-obvious rejections)

- **Vendor-into-repo** vs an override-doc vs editing globally → chose vendor: project-owned,
  version-controlled, can hard-wire our paths. An override-doc leaves each skill's own text
  ("read `CONTEXT.md`", "write a failing test") misfiring; global edits would pollute
  non-Unity work with DOTS/learnings assumptions.
- **Three sinks** vs learnings-only vs mattpocock-primary → chose three: glossary, decision,
  and how-it-works are genuinely different shapes; collapsing them is *why* `learnings/`
  already mixes gotchas with agreed-designs.
- **Per-skill** vs a blanket "verbs→skills, principles→learnings" rule → chose per-skill; the
  rule was a good prior but each skill landed differently (e.g. `tdd` justified real infra,
  `codebase-design` stayed an invocable skill despite being principle-shaped).

## Consequences

- Vendored skills **fork from upstream** — no auto-updates. Accepted for the few that needed
  Unity adaptation. Fork-provenance is **centralized in this ADR** (see *Per-skill provenance*
  below) rather than restated in each skill; every vendored skill carries only a one-line pointer
  back here (`Fork of mattpocock <name> — see ADR-0001`), keeping bodies lean and the rationale
  single-sourced.
- Same-name vendored skills **shadow** the plugin's generic versions inside this repo —
  intended; in this project `domain-modeling` etc. should resolve to the Unity-adapted copy.
- A **`Tests` asmdef now exists** (there was none), making `tdd` and `diagnosing-bugs`'
  test-seam real. Requires Unity to compile/discover it — verify in the Test Runner.
- `prototype` deliberately produces **non-Unity throwaways**; engine-fidelity questions
  (DOTS perf, `Unity.Physics` behaviour) are explicitly out of its scope.

## Per-skill provenance

Centralized from the skill bodies (each now carries only the one-line pointer). Beyond the per-skill
adaptations already in the disposition table, the deliberate **non-adoptions / renames**:

- **`domain-modeling`** — multi-context (`CONTEXT-MAP.md`) handling not adopted; this repo is
  single-context.
- **`tdd`** — upstream `tests.md` / `mocking.md` / `refactoring.md` side-files not adopted;
  `learnings/testing-methodology.md` covers that ground for this stack.
- **`ask-matt` → `ask-bryan`** — vendored under a **renamed** name (so it doesn't shadow — it's
  project-native); re-pointed at the real toolbox, and `Project.md`'s Skills & Tools table defers to it.
- **`grilling`, `grill-with-docs`, `codebase-design`, `diagnosing-bugs`, `prototype`** — adaptations as
  described in the disposition table; nothing dropped beyond what's noted there (`diagnosing-bugs` left
  un-wired to `dots-troubleshoot`/`scene-debug`; `prototype` non-Unity + scaffold-don't-fabricate).
