---
name: tdd
description: Test-driven development for this Unity project. Use when building a feature or fixing a bug test-first, or when the user mentions "red-green-refactor" / wants a regression test.
---

# Test-Driven Development

The **red-green loop**, run against this project's NUnit + Entities test seam. This skill owns the
*loop*; `Claude/learnings/testing-methodology.md` owns test *design* (which layer, naming, techniques,
test doubles, the ECS one-tick pattern, the project's arrange traps) — read it before writing a test and
don't restate it here.

## Philosophy

Tests verify **behavior through public interfaces**, not implementation. Code can change entirely; a
good test shouldn't. A test that breaks when you rename an internal field — but the behavior is
unchanged — was testing implementation. In this project the public interface is usually a system's
observable component state, a pure function's return, or a service's method surface (see
`codebase-design` for the seam vocabulary).

## The loop is vertical — one tracer bullet at a time

**Never write all the tests first, then all the implementation.** That horizontal slice produces tests
of *imagined* behavior — they test the shape of things, pass when behavior breaks, and outrun your
understanding.

Go **vertical**: one test → one implementation → repeat. Each cycle learns from the last.

```
WRONG (horizontal):   RED: test1..test5   then   GREEN: impl1..impl5
RIGHT (vertical):     RED→GREEN: test1→impl1,  test2→impl2,  …
```

## Workflow

### 1. Plan

- Read `CONTEXT.md` (if present) so test names and interface vocab match the project's domain language;
  respect ADRs in the area.
- **Pick the layer** the SUT is cheapest to witness at (Edit Mode pure logic → Play Mode unit / ECS
  one-tick → integration) using `testing-methodology.md`'s layer map. Never split one SUT across Edit
  and Play mode.
- Spot deep-module opportunities (small interface, deep implementation) — run `codebase-design` for the
  vocabulary and the testability checks.
- List the **behaviors** to test (not implementation steps), and — if the user's around — confirm which
  matter most. You can't test everything; aim at critical paths and complex logic.

### 2. Tracer bullet

Write ONE test that confirms ONE behavior. `RED:` it fails. `GREEN:` minimal code to pass. This proves
the path works end to end — same role as the tracer in `diagnosing-bugs`' Phase 1.

### 3. Incremental loop

For each remaining behavior: `RED:` next test fails → `GREEN:` minimal code to pass. One test at a time;
only enough code to pass the current one; don't anticipate future tests.

### 4. Refactor — only once GREEN

After the tests pass: extract duplication, deepen modules (move complexity behind simple interfaces),
and let new code reveal what existing code should become. Re-run tests after each step. **Never refactor
while RED** — get to GREEN first.

### Checklist per cycle

```
[ ] Test describes behavior, not implementation
[ ] Test uses the public interface only (observable component state / return value / method surface)
[ ] Test would survive an internal refactor
[ ] Code is minimal for this test — no speculative features
```

## The test seam (now real)

Tests live in `Assets/!_Game/Tests/`:

- **`Tests`** asmdef — Play Mode, references `Runtime` + `Unity.Entities`(+`.Hybrid`). The home for unit
  tests and the ECS one-tick harness — the project's sweet spot. Seeded by `MathHelperTests`.
- **`Tests/Editor/Editor.Tests`** asmdef — Edit Mode, also references `Editor`. The home for pure
  Edit-Mode logic (`[Formula]` parsing is the highest-value first target) and asset/SO validation.

Run them from the **Test Runner** window (`Window ▸ General ▸ Test Runner`), or headless via
`-runTests -testPlatform EditMode|PlayMode`. No JetBrains MCP test runner is configured here.

---
*Fork of mattpocock `tdd` — see ADR-0001.*
