---
name: ask-bryan
description: Which skill or flow fits the situation — a router over this repo's user-invoked skills.
disable-model-invocation: true
---

# Ask Bryan

You don't remember every skill, so ask. This routes over **this project's** toolbox — the Unity skills,
the vendored + global mattpocock pack, and caveman. Each entry says *when to reach*; the skill's own
description says what it does.

A **flow** is a path through the skills. Most work runs along the main flow; **on-ramps** merge onto it.

## The main flow: idea → shipped Unity code

1. **Sharpen the idea — `/grilling`.** Relentless interview, learnings-first. Reach for
   **`/grill-with-docs`** instead when the session will resolve glossary terms or real decisions worth
   keeping — it captures them as you go (see *the three sinks*).
2. **Branch — does a question need a _runnable_ answer?** State/logic, a UI you must see, or a spatial
   feel you can't judge on paper → **`/prototype`** (throwaway C# console / HTML / scene scaffold). It's
   the exploratory sibling of grilling: grill sharpens a thesis you have, prototype finds one you don't.
   Bridge into and out of a prototype session with **`/handoff`**.
3. **Branch — is this a multi-session build?**
   - **Yes** → **`/to-prd`** → **`/to-issues`**, keeping steps 1–3 in **one context window** so they
     build on the same thinking. Then a **fresh session per issue**, each kicking off **`/implement`**.
   - **No** → build it right here.
4. **Build it (Unity).** Always **`/unity-coding-standards`** for the house C#/architecture rules. Then
   by surface:
   - new ECS system / component / job / authoring → **`/dots-new-system`**
   - inspector layout / OVDF → **`/odin-visual-designer`**
   - editor C# (custom inspectors, windows, drawers, asset mutation) → **`/unity-editor-scripting`**
   - hand-editing a ScriptableObject / material / asset YAML → **`/unity-yaml-editing-guide`**
5. **Review — `/code-review-unity`** (day-to-day standards). `/code-review` for correctness+cleanup on
   the diff; `/security-review` before shipping anything sensitive.
6. **Capture what you learned** → route into *the three sinks* below.

## On-ramps

A starting situation that generates work, then merges onto the main flow.

- **A hard bug, right now → `/diagnosing-bugs`.** The discipline: build a **tight, red** feedback loop
  first, hypothesise second. Pairs with two auto-firing helpers you can also name directly:
  **`/dots-troubleshoot`** (DOTS runtime symptom→fix — ECB playback, ComponentLookup, stale enableable
  flags, systems not running) and **`/scene-debug`** (when the bug smells like scene/prefab authoring).
- **Bug reports / requests piling up → `/triage`.** Moves raw issues to agent-ready, which `/implement`
  picks up. Only for issues **you didn't create** — `/to-issues` output is already agent-ready.

## The three sinks — where knowledge goes

This repo's load-bearing rule (`Claude/docs/adr/0001`). Pick by **shape**:

- **A glossary term** (what a word *means*) → `CONTEXT.md`, via **`/domain-modeling`**.
- **A decision with a real trade-off** (hard to reverse, surprising, genuine alternatives) → an ADR in
  `Claude/docs/adr/`, via **`/domain-modeling`**.
- **How-it-works / a gotcha / a system map** → `Claude/learnings/`, via **`/knowledge-save`**.

Don't smuggle a gotcha into an ADR or a term into a learning — the shapes are different.

## Design vocabulary

- **`/codebase-design`** — deep-module language (interface, seam, depth, adapter) for when you're
  deciding where a seam goes or making code testable. Fits Infrastructure/Mono/Reflex cleanly; ECS only
  loosely.

## Crossing sessions & upkeep

- **`/handoff`** — compact a full or branching thread into a markdown file, then open a **fresh session**
  against it. The bridge between context windows; `/handoff` forks, `/compact` (built-in) continues in
  place at a clean phase break.
- **`/resolving-merge-conflicts`** — but **never hand-merge `.unity` / `.prefab`** (read-only — defer to
  UnityYAMLMerge or the user).
- **`/improve-codebase-architecture`** — upkeep; surfaces deepening opportunities that become ideas you
  take back to step 1.

## Token diet

- **`/caveman`** (talk compressed), **`/caveman-commit`**, **`/caveman-review`**, **`/compress`** (shrink
  a memory file), **`/caveman-help`** (the index).

## Standalone

- **`/grill-me`** — relentless interview with **no codebase** (stateless; writes nothing).
- **`/teach`** — learn a concept over multiple sessions in a stateful workspace.
- **`/writing-great-skills`** — reference for authoring and editing skills.

---
*Fork of mattpocock `ask-matt` (renamed) — see ADR-0001.*
