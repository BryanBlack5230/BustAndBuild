---
name: prototype
description: Build a throwaway prototype to answer a design question empirically — a runnable C# console for logic/state, vanilla HTML/JS variations for UI, or in-engine scaffolding for a scene you author. The exploratory sibling of grilling; non-Unity by default, lives in Claude/prototypes/.
disable-model-invocation: true
---

# Prototype

A prototype is **throwaway code that answers a question**. The question decides the shape.

## Non-Unity by default — and the fidelity boundary

This project's prototypes are **non-Unity throwaways**, run outside the engine:

- **Logic / state** → a C# console app (`dotnet run`). See [LOGIC.md](LOGIC.md).
- **UI / UX** → vanilla HTML/JS variations in a browser. See [UI.md](UI.md).

The one exception is the **spatial** branch below, which *must* run in-engine.

**A prototype answers design / logic / UX questions — NOT engine-fidelity questions.** A console appproves a state machine's transitions are right; it says nothing about DOTS scheduling cost. An HTML mock proves a layout reads well; it is not Unity's renderer. Treat questions about **DOTS performance, `Unity.Physics` behaviour, job timing, or baking** as explicitly out of scope — those need a real in-engine measurement (see `diagnosing-bugs`' Phase-1 one-tick harness), not a prototype. State this boundary at the top of the prototype so its answer isn't over-trusted.

## Pick a branch

Identify which question is being answered — from the prompt, the surrounding code, or by asking if the user is around:

- **"Does this logic / state model feel right?"** → [LOGIC.md](LOGIC.md). A tiny interactive C# console
  that drives the state machine through cases that are hard to reason about on paper.
- **"What should this look like?"** → [UI.md](UI.md). Several radically different UI variations on one
  HTML page, switchable from a floating bar.
- **"Does this *feel* right in space?"** (an irreducibly-spatial question — steering, formations, placement, throw arcs) → **scaffold, don't fabricate** (see below).

Getting the branch wrong wastes the whole prototype. If genuinely ambiguous and the user isn't reachable, default by what's being prototyped (a system/state question → logic; a screen → UI; a positional/movement question → spatial) and state the assumption at the top.

### The spatial branch — scaffold a scene, don't fabricate one

Some questions are irreducibly spatial and can't be answered in a console or a browser. These are the **one branch that lives in-engine** (inside `Assets/`), because nothing else has the real coordinate space. But Claude is **read-only on `.unity` / `.prefab`** (the user authors scenes by hand) — so you **never fabricate a scene or prefab**. Instead, build **runnable scaffolding the user drops into a scene they author**:

- A throwaway **spawner** MonoBehaviour (`[Button] Spawn`, count/spread fields) that lays out the entities the question needs.
- A **debug-toggle** component + `Debug.DrawLine`/Gizmos that surface the quantity in question (the steering vector, the predicted arc, the AABB) — same idea as `diagnosing-bugs`' visual probe.
- An **Odin `[Button]` playground** on a MonoBehaviour/ScriptableObject to poke parameters live.

Name it so it's obviously a prototype, mark it throwaway, and hand the user the scaffolding + what to author around it. The captured answer still feeds an ADR or a learning; the scaffolding gets deleted.

## Mirror the real seams, stub the connections

The logic that answers the question should be written so it can be **lifted into the real codebase**.
In `codebase-design` terms: write the answer behind a **portable interface (the production seam)**, and let everything that wires it to the prototype shell be **stub adapters** you throw away.

- The pure reducer / state machine / function set in a logic prototype = the production seam. The
  console TUI around it = a stub adapter (gets deleted).
- A UI variant's data = a stub (hard-coded sample), standing in for the real source.
- Spatial scaffolding's spawner = a stub adapter; the tuned values / chosen approach are what lift out.

Nothing flows from the shell back into the seam. If the reducer references `Console.Write` or the variant references a real save/load, the seam isn't portable — fix that before handing over.

## Rules that apply to every branch

1. **Throwaway from day one, clearly marked.** A casual reader must see it's a prototype, not production.
2. **Lives OUTSIDE `Assets/`** → `Claude/prototypes/<name>/` (create the folder lazily). Unity's importer chokes on a console/HTML project under `Assets/`, and it keeps throwaways out of the build. The **spatial** branch is the sole exception — it must live in `Assets/` to run in-engine, so mark it loudly and delete it the moment it's answered.
3. **One command to run.** `dotnet run` for a console; open the HTML file (or `python -m http.server`) for UI. The user starts it without thinking.
4. **No persistence.** State lives in memory. Persistence is a thing a prototype *checks*, not depends on — unless the question is specifically about saving.
5. **Skip the polish.** No tests, no abstractions, no error handling beyond what makes it runnable. Learn fast, then delete.
6. **Surface the state.** After every action (logic) or on every variant switch (UI), render the full relevant state so the user sees what changed.
7. **Delete or absorb when done.** Either delete it or fold the validated decision into real code — don't leave it rotting.

## When done — capture the answer

The **answer** is the only thing worth keeping. Route it into the right sink:

- A **decision with a real trade-off** (we chose state machine over reducer because…) → an **ADR** in `Claude/docs/adr/` (via `domain-modeling`).
- A **"how it works" / a gotcha** the prototype surfaced → a **learning** (via `knowledge-save`).

If the user is around, that capture is a quick conversation. If not, leave a `NOTES.md` next to the prototype stating the question and a placeholder verdict, so it can be filled in before the prototype is deleted

---
*Fork of mattpocock `prototype` — see ADR-0001.*