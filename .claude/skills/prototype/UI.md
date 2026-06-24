# UI Prototype

Generate **several radically different UI variations** in one **vanilla HTML/JS** file, switchable from a
floating bottom bar. The user flips between variants in the browser, picks one (or steals bits from
each), then throws the rest away.

If the question is about logic/state rather than what something looks like — wrong branch, use
[LOGIC.md](LOGIC.md). If it's about how something *feels* in space — wrong branch, scaffold a scene (see
[SKILL.md](SKILL.md)).

## When this is the right shape

- "What should this menu / HUD / settings screen look like?"
- "I want to see a few options for this dashboard before committing."
- "Try a different layout for the upgrade panel."
- Any time the user would otherwise spend a day picking between three vague mockups in their head.

## Fidelity boundary — HTML is not Unity

This game's real UI is Unity (uGUI / Odin / whatever the screen uses), **not** a web page. An HTML mock
answers **layout, hierarchy, density, and affordance** questions cheaply — "should the cost sit above or
beside the buy button," "does a sidebar or a top-tab read better." It does **not** reproduce Unity's
renderer, fonts, input feel, or animation. Use it to *decide the structure*, then rebuild the winner
properly in Unity. State this at the top of the prototype so the mock isn't mistaken for the real thing.

(No web-app routing here, so the upstream skill's "embed in an existing `?variant=` route" sub-shape
doesn't apply — these are standalone throwaway files.)

## Process

### 1. State the question and pick N

Default to **3 variants**. More than 5 stops being radically different and starts being noise — cap
there. Write the plan in one line, as a comment at the top of the HTML file or in a `NOTES.md` beside it:

> "Three layouts for the upgrade panel, switchable via `?variant=`, vanilla HTML."

### 2. Generate radically different variants

Variants must be **structurally different** — different layout, different information hierarchy,
different primary affordance, not just different colours. Three slightly-tweaked card grids isn't a
prototype, it's wallpaper. If two drafts come out too similar, redo one with explicit "do not use a card
grid" guidance. Hold each variant to the screen's purpose and the data it would realistically have
(stub the data — hard-coded sample values standing in for the real source).

### 3. One file, plain JS, `?variant=` switching

Everything in a single `.html` file — no framework, no build step, no React/Next. Each variant is a
function that returns markup (or builds DOM); a switcher reads `?variant=` and renders one:

```html
<!-- Claude/prototypes/<name>/index.html -->
<div id="stage"></div>
<script>
  const variants = {
    A: () => `<!-- layout A: top-tabs … -->`,
    B: () => `<!-- layout B: sidebar … -->`,
    C: () => `<!-- layout C: single column … -->`,
  };
  const keys = Object.keys(variants);
  const cur = new URLSearchParams(location.search).get('variant') ?? keys[0];
  document.getElementById('stage').innerHTML = variants[cur]();
  // … render the floating bar (step 4) …
</script>
```

### 4. Build the floating switcher

A small `position: fixed` bar at the bottom-centre with three pieces: **left arrow** (previous, wraps),
**variant label** (`B — Sidebar layout`), **right arrow** (next, wraps).

- Clicking an arrow updates `?variant=` and reloads (`location.search = '?variant=' + key`), so the
  variant is shareable and reload-stable.
- Keyboard `←` / `→` also cycle. Don't intercept arrow keys when an `<input>`, `<textarea>`, or
  `[contenteditable]` is focused.
- Visually distinct from the page (high-contrast pill, subtle shadow) so it's obviously **not** part of
  the design being evaluated.

### 5. One command to run

It's a static file — the user just opens it:

```
start Claude/prototypes/<name>/index.html      # Windows; or double-click
```

If a variant needs to `fetch()` something (CORS blocks `file://`), serve it instead:

```
python -m http.server --directory Claude/prototypes/<name>
```

Surface the URL (and the `?variant=` keys) so the user can flip through whenever they get to it.

### 6. Capture the answer and clean up

The interesting feedback is usually **"I want the header from B with the sidebar from C"** — that's the
actual design they want. Once a variant (or a Frankenstein of two) wins, write down which and why: a
**decision with a trade-off** → an ADR (`domain-modeling`); a **layout gotcha** → a learning
(`knowledge-save`); or `NOTES.md` if running AFK. Then **rebuild the winner in Unity** and delete the
HTML — don't promote the mock, and don't leave variants rotting.

## Anti-patterns

- **Variants that differ only in colour or copy.** That's a tweak. Real variants disagree about
  structure.
- **Reaching for a framework or build step.** One vanilla `.html` file. If you're running `npm install`,
  you've overshot the prototype.
- **Wiring variants to real data or mutations.** Read-only, stubbed data. The question is "what should
  this look like," not "does the backend work."
- **Mistaking the mock for the implementation.** It decides structure; Unity gets the real build (no
  tests, no error handling were written here).

---
*Fork of mattpocock `prototype` (UI) — see ADR-0001.*
