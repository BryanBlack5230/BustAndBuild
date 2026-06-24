# Logic Prototype

A tiny interactive **C# console app** that lets the user drive a state model by hand. Use this when the
question is about **business logic, state transitions, or data shape** — the kind of thing that looks
reasonable on paper but only feels wrong once you push it through real cases.

If the question is "what should this look like" — wrong branch, use [UI.md](UI.md). If it's "does this
*feel* right in space" — wrong branch, scaffold a scene (see [SKILL.md](SKILL.md)).

## When this is the right shape

- "I'm not sure this state machine handles the case where X then Y." (retreat → re-enter base → ?)
- "Does this data model actually let me represent the case where…"
- "I want to feel out what this API should look like before writing it."
- Anything where the user wants to **press a key and watch state change**.

## Process

### 1. State the question

Before any code, write what state model and what question you're prototyping — one paragraph in a
`NOTES.md` next to the prototype, or a comment at the top of `Program.cs`. A logic prototype that
answers the wrong question is pure waste; make the question explicit so it can be checked later, whether
the user is watching now or returning to it AFK.

### 2. Isolate the logic in a portable module — the production seam

Put the bit that answers the question behind a small, **pure** interface that could be lifted straight
into the real codebase later (`codebase-design`: this is the **production seam**; the TUI is a **stub
adapter**). Pick the shape that fits the *question*, not whatever is easiest to wire to a console:

- **A pure reducer** — `State Reduce(State s, Action a)`. Good when actions are discrete events and
  state is a single value.
- **A state machine** — explicit states + legal transitions. Good when "which actions are even legal
  right now" is part of the question.
- **A small set of pure functions** over a plain `record`. Good when there's no implicit current state,
  just transformations (scorer math, formula evaluation).
- **A class with a clear method surface** when the logic genuinely owns ongoing internal state.

Keep it pure: no `Console.*`, no I/O, no logging for control flow. The TUI calls into it; nothing flows
back. This purity is exactly what lets the validated reducer / machine lift into the real module while
the console shell gets deleted. Prefer plain `record` / `enum` types so the seam is engine-agnostic
(don't pull in `Unity.*` — the prototype runs under plain `dotnet`, not Unity).

### 3. Scaffold the project — one folder, one command

```
Claude/prototypes/<name>/
├── NOTES.md          ← the question + (later) the answer
├── <name>.csproj     ← `dotnet new console` output
├── Model.cs          ← the portable seam (step 2)
└── Program.cs        ← the throwaway TUI (step 4)
```

`dotnet new console -o Claude/prototypes/<name>` gets the `.csproj` + `Program.cs`. The repo has
**dotnet 7.0.404** on PATH — confirm with `dotnet --version` before relying on it. The user runs it with:

```
dotnet run --project Claude/prototypes/<name>
```

That's the one command — they never need to remember a path beyond the project folder.

### 4. Build the smallest TUI that exposes the state

A **lightweight redraw loop**: each tick, clear the screen and re-render the whole frame, so the user
always sees one stable view, not growing scrollback.

```csharp
Console.Clear();                       // wipe + redraw every frame
Console.WriteLine($"\x1b[1mState\x1b[0m");      // \x1b[1m bold, \x1b[2m dim, \x1b[0m reset
// … one field per line, diff-friendly …
Console.WriteLine("\x1b[2m[a]\x1b[0m add  \x1b[2m[t]\x1b[0m tick  \x1b[2m[q]\x1b[0m quit");
var key = Console.ReadKey(intercept: true).Key;   // one keystroke at a time
state = Dispatch(state, key);          // mutate via the pure seam, then loop
```

Two parts per frame, in order:

1. **Current state**, pretty-printed and diff-friendly — one field per line (or formatted output).
   **Bold** field names/headers, **dim** less-important context (timestamps, derived values). Native
   ANSI escapes are fine; don't pull in a styling library.
2. **Keyboard shortcuts** at the bottom: `[a] add  [t] tick  [q] quit`.

Behaviour: initialise a single in-memory state object → render the first frame → read one keystroke →
dispatch to the pure seam → re-render the *whole* frame (replace, don't append) → loop until quit. The
whole frame fits on one screen.

### 5. Hand it over

Give the user the run command. They drive it; the interesting moments are when they say "wait, that
shouldn't be possible" or "huh, I assumed X" — those are bugs in the *idea*, the whole point. If they
want new actions, add them. Prototypes evolve.

### 6. Capture the answer

When it's done its job, the answer is the only keeper. If the user is around, ask what it taught them.
Route it: a **decision with a trade-off** → an ADR (`domain-modeling`); a **gotcha / how-it-works** →
a learning (`knowledge-save`). If AFK, leave the verdict in `NOTES.md` before the prototype is deleted.

## Anti-patterns

- **Don't add tests.** A prototype that needs tests is no longer a prototype. (Real tests go through the
  `Tests` asmdef once the seam lifts into the codebase — see `Claude/learnings/testing-methodology.md`.)
- **Don't reach for Unity types.** The seam runs under plain `dotnet`; `float3`/`Entity`/`World` don't
  belong here. If the logic genuinely can't be expressed without them, it's the spatial branch, not this
  one.
- **Don't persist.** In-memory store unless the question is specifically about persistence.
- **Don't generalise.** No "what if we wanted X later." One question.
- **Don't blur the seam and the TUI.** If the reducer/machine references `Console`, it's no longer
  portable. Thin shell over a pure module.
- **Don't ship the shell.** The TUI is optimised to be driven by hand; the seam behind it is the keeper.

---
*Fork of mattpocock `prototype` (LOGIC) — see ADR-0001.*
