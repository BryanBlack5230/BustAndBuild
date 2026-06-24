# Enter Play Mode with Domain Reload disabled; reset static state explicitly

**Status:** accepted

Both Enter-Play-Mode options (Reload Domain, Reload Scene) are **off** for fast play-mode entry,
alongside the Hot Reload asset for in-Play code edits. The cost we accept: static state, static
event subscriptions, and cached `UnityEngine.Object` references **persist across Play sessions**,
so every mutable static must reset itself via `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`
— and those resets live **next to the state they clear**, not in a central teardown.

## Considered options

- **Leave Domain Reload on (Unity default)** → rejected: the per-enter reload cost dominates the
  tight code-test loop this project relies on; the reset discipline is a one-time per-field tax.
- **A central "reset everything" hook** → rejected: it drifts out of sync with the fields it must
  clear; co-locating each reset with its state keeps them honest.

## Consequences

- This is *why* `EventBus`, `CoreHelper`, `GizmoManager`, etc. each carry a `SubsystemRegistration`
  reset — without them subscriptions ghost-fire and caches hand back last-run objects.
- **Authoring rule:** default to instance state on a Reflex-injected service (the container is
  rebuilt each Play); if static is genuinely required, add the reset in the same file.
- Symptoms, the reset recipe, and the audit list of existing resets live in
  `Claude/learnings/play-mode-and-hot-reload.md`.
