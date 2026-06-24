---
name: diagnosing-bugs
description: Diagnosis loop for hard bugs and performance regressions. Use when the user says "diagnose"/"debug this", or reports something broken/throwing/failing/slow.
---

# Diagnosing Bugs

A discipline for hard bugs. Skip phases only when explicitly justified.

When exploring the codebase, read `CONTEXT.md` (if it exists) to get a clear mental model of the
relevant modules, and check ADRs in the area you're touching.

## Phase 1 — Build a feedback loop

**This is the skill.** Everything else is mechanical. If you have a **tight** pass/fail signal for
the bug — one that goes red on _this_ bug — you will find the cause; bisection, hypothesis-testing,
and instrumentation all just consume it. If you don't have one, no amount of staring at code will
save you.

Spend disproportionate effort here. **Be aggressive. Be creative. Refuse to give up.**

### Ways to construct one — try them in roughly this order

_(Unity / DOTS edition. The goal is unchanged: a fast, deterministic, red-capable signal.)_

1. **One-tick system harness.** Build a fresh `World`, add the entities + components the bug needs
   (**enableable components gate the query** — `SetComponentEnabled<T>(e, true)` in arrange), call
   `world.GetOrCreateSystem<TSystem>().Update(world.Unmanaged)`, then `CompleteAllTrackedJobs()`,
   and assert on component state. Pure, sub-second, deterministic — the tightest loop this project
   has. (Needs the `Tests` asmdef.)
2. **EditMode / PlayMode NUnit test** at whatever seam reaches the bug — pure logic (Formulas,
   `MathHelper`, scorer math) in Edit Mode; runtime-initiated logic in Play Mode.
3. **Minimal play-mode repro.** Boot a `*Dev` RunConfiguration (e.g. `BattleDev`) straight into the
   affected scene so you're not clicking the whole flow each loop. Make it deterministic: fixed
   timestep, and seed any `Unity.Mathematics.Random` from a component so the run repeats.
4. **Differential loop.** Run the same input through two configs/branches (or profiles) and diff the
   outputs — ideal for "this used to work" regressions.
5. **Property / fuzz loop.** For "sometimes wrong" bugs, drive the system over many *seeded* inputs
   and watch for the failure mode.
6. **Visual probe.** For spatial bugs (steering, trajectory, collision, placement),
   `Debug.DrawLine`/`DrawRay`/Gizmos the quantities each frame — the wrong vector usually jumps out
   faster than any log.
7. **Read-only state dump.** Diff the *authored* scene/prefab state against what you expect — many
   "runtime" bugs are actually authoring/baking mistakes.
8. **Bisection harness.** If the bug appeared between two known states (commit, dataset, package
   version), script boot-check-repeat so you can `git bisect run` it.

Build the right feedback loop, and the bug is 90% fixed.

### Tighten the loop

Treat the loop as a product. Once you have _a_ loop, **tighten** it:

- Can I make it faster? (Cache setup, skip unrelated init, narrow the test scope.)
- Can I make the signal sharper? (Assert on the specific symptom, not "didn't crash".)
- Can I make it more deterministic? (Fixed timestep, seed RNG, fresh `World` per run.)

A 30-second flaky loop is barely better than no loop; a 2-second deterministic one is tight — a
debugging superpower.

### Non-deterministic bugs

The goal is not a clean repro but a **higher reproduction rate**. Loop the trigger 100×, parallelise,
add stress, narrow timing windows. A 50%-flake bug is debuggable; 1% is not — keep raising the rate
until it's debuggable. (In this project the determinism levers are usually a seeded
`Unity.Mathematics.Random` + a fixed timestep.)

### When you genuinely cannot build a loop

Stop and say so explicitly. List what you tried. Ask the user for: (a) access to whatever reproduces
it, (b) a captured artifact (a repro scene / RunConfiguration, a saved input recording, a
screen-recording with timestamps, a Profiler capture, a Player log), or (c) permission to add
temporary instrumentation. Do **not** proceed to hypothesise without a loop.

### Completion criterion — a tight loop that goes red

Phase 1 is done when the loop is **tight** and **red-capable**: you can name **one command** — a
test invocation, a `-runTests` CLI run, a one-tick harness, or a RunConfiguration that boots straight
to the repro — that you have **already run at least once** (paste the invocation and its output), and
that is:

- [ ] **Red-capable** — it drives the actual bug code path and asserts the **user's exact symptom**,
  so it can go red on this bug and green once fixed. Not "runs without erroring" — it must be able to
  _catch this specific bug_.
- [ ] **Deterministic** — same verdict every run (flaky bugs: a pinned, high reproduction rate).
- [ ] **Fast** — seconds, not minutes.
- [ ] **Agent-runnable** — you can run it unattended.

If you catch yourself reading code to build a theory before this command exists, **stop — jumping
straight to a hypothesis is the exact failure this skill prevents.** No red-capable command, no
Phase 2.

## Phase 2 — Reproduce + minimise

Run the loop. Watch it go red — the bug appears.

Confirm:

- [ ] The loop produces the failure mode the **user** described — not a different failure that
  happens to be nearby. Wrong bug = wrong fix.
- [ ] The failure is reproducible across multiple runs (or, for non-deterministic bugs, reproducible
  at a high enough rate to debug against).
- [ ] You have captured the exact symptom (error message, wrong output, slow timing) so later phases
  can verify the fix actually addresses it.

### Minimise

Once it's red, shrink the repro to the **smallest scenario that still goes red**. Cut inputs, callers,
config, data, and steps **one at a time**, re-running the loop after each cut — keep only what's
load-bearing for the failure.

Why bother: a minimal repro shrinks the hypothesis space in Phase 3 (fewer moving parts left to
suspect) and becomes the clean regression test in Phase 5.

Done when **every remaining element is load-bearing** — removing any one of them makes the loop go
green.

Do not proceed until you have reproduced **and** minimised.

## Phase 3 — Hypothesise

Generate **3–5 ranked hypotheses** before testing any of them. Single-hypothesis generation anchors
on the first plausible idea.

Each hypothesis must be **falsifiable**: state the prediction it makes.

> Format: "If <X> is the cause, then <changing Y> will make the bug disappear / <changing Z> will make
> it worse."

If you cannot state the prediction, the hypothesis is a vibe — discard or sharpen it.

**Show the ranked list to the user before testing.** They often have domain knowledge that re-ranks
instantly ("we just changed #3"), or know hypotheses they've already ruled out. Cheap checkpoint, big
time saver. Don't block on it — proceed with your ranking if the user is AFK.

## Phase 4 — Instrument

Each probe must map to a specific prediction from Phase 3. **Change one variable at a time.**

Tool preference:

1. **Debugger / breakpoint** (Rider) when the path is on the main thread — one breakpoint beats ten
   logs. (Burst-compiled jobs won't break — drop to logging, or temporarily disable Burst to step.)
2. **Targeted logs** at the boundaries that distinguish hypotheses.
3. Never "log everything and grep".

**Tag every debug log** with a unique prefix, e.g. `[DEBUG-a4f2]`. Cleanup at the end becomes a single
grep. Untagged logs survive; tagged logs die.

**Perf branch.** For performance regressions, logs are usually wrong. Instead: establish a baseline
measurement (a `Stopwatch` around the hot path, the Unity Profiler, or the Entities/Burst timings),
then bisect. Measure first, fix second.

## Phase 5 — Fix + regression test

Write the regression test **before the fix** — but only if there is a **correct seam** for it.

A correct seam is one where the test exercises the **real bug pattern** as it occurs at the call site.
If the only available seam is too shallow (single-caller test when the bug needs multiple callers, unit
test that can't replicate the chain that triggered the bug), a regression test there gives false
confidence.

**If no correct seam exists, that itself is the finding.** Note it. The codebase architecture is
preventing the bug from being locked down. Flag this for the next phase.

If a correct seam exists:

1. Turn the minimised repro into a failing test at that seam.
2. Watch it fail.
3. Apply the fix.
4. Watch it pass.
5. Re-run the Phase 1 feedback loop against the original (un-minimised) scenario.

## Phase 6 — Cleanup + post-mortem

Required before declaring done:

- [ ] Original repro no longer reproduces (re-run the Phase 1 loop)
- [ ] Regression test passes (or absence of seam is documented)
- [ ] All `[DEBUG-...]` instrumentation removed (`grep` the prefix)
- [ ] Throwaway prototypes deleted (or moved to a clearly-marked debug location)
- [ ] The hypothesis that turned out correct is stated in the commit / PR message — so the next
  debugger learns

**Then ask: what would have prevented this bug?** If the answer involves architectural change (no good
test seam, tangled callers, hidden coupling) hand off to the `/improve-codebase-architecture` skill
with the specifics. Make the recommendation **after** the fix is in, not before — you have more
information now than when you started.

---
*Fork of mattpocock `diagnosing-bugs` — see ADR-0001.*
