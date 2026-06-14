---
name: scene-debug
description: >-
  Inspect Unity scene/prefab structure during debugging AUTOMATICALLY, without being pointed at a specific file. Use this skill whenever diagnosing a bug or unexpected behavior that could involve scene/prefab authoring: an object missing or at the wrong position, a missing or misconfigured component, a null/broken SerializeField or object reference, wrong serialized/inspector values, hierarchy or parenting problems, an unexpectedly inactive GameObject, MonoBehaviour wiring, prefab-instance overrides, or any "why isn't X working in the scene/prefab" question. It runs the read-only Claude/tools/scene_inspect.py to dump the relevant scene/prefab tree (hierarchy, transforms, components, values) instead of reading raw YAML or using MCP. Load it whenever a debugging task plausibly touches what lives in a .unity or .prefab — figure out which file yourself from the bug, don't make the user name it.
---

# Scene/Prefab Debug Inspection

When a bug might live in scene/prefab **authoring** (placement, components, values, references, hierarchy, active state), inspect structure with `Claude/tools/scene_inspect.py` **before** theorizing from code alone. The user does NOT need to point you at a file — resolve it yourself (Step 1). Read-only: never edit scenes/prefabs — the user authors those (see memory `no-scene-editing`). Token-aware: scope every dump.

## When this fires (and when it doesn't)
- **Fires:** "X is at the wrong place / missing / not reacting", a null `[SerializeField]`/reference after load, a value that looks wrong in the inspector, parenting or active-state oddities, a component that should be on an object but isn't, prefab-instance overrides, "works in scene A but not B".
- **Doesn't:** pure C# logic bugs with no scene surface (formula math, save serialization) → skip the inspector. Pure ECS **runtime** failures (ECB/query/enableable/stale flags) → that's the `dots-troubleshoot` skill; the inspector only helps there if a baking/authoring object is the suspect (missing authoring component, unbaked field).

## Step 1 — find the target yourself (don't ask)
1. **Object named** ("the Castle isn't taking damage") → locate the file(s):
   `grep -rln "m_Name: <Object>" Assets --include=*.unity --include=*.prefab`
   (also try the quoted form for names with brackets/leading digits, e.g. `"m_Name: '[INTERFACE]'"`).
2. **Component / script named** → find its guid, then where it's used:
   `g=$(grep -m1 'guid:' Path/To/Comp.cs.meta | awk '{print $2}'); grep -rln "$g" Assets --include=*.unity --include=*.prefab`
3. **Area implied** → map to the scene (under `Assets/!_Game/Runtime/Gameplay/Scenes/`):
   bootstrap→`0.Bootstrap` · loading→`1.Loading` · world→`2.World`/`WorldECS` · battle→`3.BattleGroundScene`/`BattleGroundSceneECS` · city→`4.City`.
4. **Recently touched** → `git status --short` / `git diff --name-only` for changed `.unity`/`.prefab`.
5. Still ambiguous across files → inspect the most likely 1–2; only ask the user if genuinely stuck.

## Step 2 — run it, scoped
- **Default to targeted, not whole-scene** (saves tokens): `python Claude/tools/scene_inspect.py <file> -o <suspect>` (`-o` is case-insensitive substring).
- Whole tree only when you need the overall picture: `python Claude/tools/scene_inspect.py <file>` (add `-d 2`/`-d 3` to cap depth on big scenes).
- **Value/config bug** → add `-f` to dump serialized fields (compare against what the code assumes).
- **Suspect shows `Script<guid8>`** (a package component — Cinemachine, ugui Button/Image, TMP, Reflex scope) → add `--deep` to resolve its real name.
- A prefab is the suspect → pass the `.prefab` directly.

## Step 3 — read structure, then diagnose
- Object exists? **active** (`+` vs `-`)? parented where expected? right position/rotation/scale?
- Component list: is the expected authoring/MonoBehaviour actually present? A missing component is a Baker/wiring bug, not a logic bug.
- With `-f`: unassigned references read `{fileID: 0}`; watch for off-by-one profile indices, wrong enum ints, zeroed tunables.
- **Cross-check with code.** The inspector shows **saved/authored** state, not runtime. If authoring looks correct but behavior is wrong, the bug is in code/runtime — pivot to logging, `dots-troubleshoot` (ECS), or the relevant `Claude/learnings/` file. State plainly when the scene rules authoring *out* as the cause.

## Limits
- Saved state only — not play-mode/runtime values.
- Nested **prefab instances** render as one `~ … [prefab instance, N overrides]` node; to see inside, inspect the source `.prefab` it references.
- Never edit the scene to "test a fix" — report the finding and hand the change to the user.
