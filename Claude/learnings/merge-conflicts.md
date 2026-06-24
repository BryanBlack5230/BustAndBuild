# Merge Conflicts in Unity Files

**Never hand-merge `.unity` (scenes) or `.prefab` (prefabs).** Claude is read-only on both — the user
hand-authors them, the same rule that governs all scene/prefab work. A line-based merge of these files
is neither a safe operation nor a reviewable one.

## Why a text merge corrupts them
Unity scene/prefab YAML is a **graph, not a document**: objects are keyed by `fileID`, cross-reference
each other by those IDs, and order is significant. Git's 3-way merge has no model of that graph, so a
"clean" auto-merge (or a hand-edit of the conflict markers) silently produces dangling or duplicated
`fileID`s, components reparented onto the wrong object, dropped prefab overrides, or a file Unity
refuses to load. The damage never shows as a conflict marker — it shows as a broken scene three commits
later.

## What to do instead
- **`.unity` / `.prefab` conflicts → hand to the user.** Surface exactly which files conflict; do **not**
  open them to "fix" the markers. The user resolves them in Unity (re-applying their authoring) or via
  **UnityYAMLMerge** (Unity's *Smart Merge*, ships with the editor), which merges along the object graph
  instead of by line.
- **Everything else → resolve normally.** C#, `.asmdef`, `.json`, docs — ordinary 3-way merges; review
  and resolve as usual.
- **`.asset` / `.mat` YAML → defer too, unless it's a single scalar** the [[unity-yaml-editing-guide]]
  would already let you touch. ScriptableObject/material YAML is flatter than a scene but still
  `fileID`-referenced; when in doubt, treat it like a scene.

## Current state
This repo has **no `.gitattributes` and no `merge.unityyamlmerge` mergetool configured** — so there is
currently *no* automated Smart Merge guardrail; a naive `git merge`/`rebase` across a branch that touched
a scene will line-merge it. Until the user wires UnityYAMLMerge (a `.gitattributes` entry plus a `merge`
driver pointing at `UnityYAMLMerge.exe`), the only safe path for a scene/prefab conflict is **defer to
the user**. Flag this setup gap the first time such a conflict bites.

---
*Guardrail for the `resolving-merge-conflicts` skill (mattpocock pack, left global) on this Unity
project — pairs with the read-only-on-`.unity`/`.prefab` rule. See `Claude/docs/adr/0001` for why that
skill stayed global.*
