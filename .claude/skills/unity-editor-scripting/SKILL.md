---
name: unity-editor-scripting
description: >-
  Gotchas and reusable patterns for writing Unity Editor C# in this project — custom inspectors
  (CustomEditor / UnityEditor.Editor), EditorWindows, PropertyDrawers, Odin [Button] / [MenuItem]
  methods, and any edit-time asset/prefab/scene manipulation through UnityEditor APIs (AssetDatabase,
  PrefabUtility, EditorSceneManager, Selection, IMGUI). Use this whenever authoring or modifying
  anything under Assets/!_Game/Editor/, adding an editor-only [Button] or #if UNITY_EDITOR block to a
  runtime ScriptableObject/MonoBehaviour, or scripting Unity to mutate assets/prefabs/scenes. It
  encodes silent-data-loss and IMGUI traps that compile fine and fail quietly. For inspector *layout*
  via Odin OVDF files, use odin-visual-designer instead — this is the C# editor-scripting side.
---

# Unity Editor Scripting — Gotchas & Patterns

Editor code lives in `Assets/!_Game/Editor/` (assembly `Editor`, namespace `BarkingBird.Editor`) or behind `#if UNITY_EDITOR` in a runtime file. Logging: use **`Log.Editor.D/W/E`** (it auto-prefixes `[ClassName]`), never raw `Debug.Log` — see memory `editor-logging-and-style` and `unity-coding-standards`. Deep dives: `Claude/learnings/custom-inspector-patterns.md`, `editor-window-patterns.md`, `scriptable-object-patterns.md`, and the prefab-asset note in `unity-csharp-patterns.md`.

## Silent data loss — check these first
- **Mutating a prefab *asset* in place does NOT persist.** Editing a Project-window prefab (`EditorUtility.IsPersistent(go)` is true) via `SetDirty` *appears* to work in-session, then the change is lost on reimport/restart. Route persistent objects through prefab-contents:
  ```csharp
  var root = PrefabUtility.LoadPrefabContents(path);
  try   { /* mutate root hierarchy */ if (changed) PrefabUtility.SaveAsPrefabAsset(root, path); }
  finally { PrefabUtility.UnloadPrefabContents(root); }
  ```
  Branch on `EditorUtility.IsPersistent` — scene objects keep the `Undo` path; prefab contents are not undoable. Live example: `Editor/MissingScriptsFinder.cs`.
- **`SaveCurrentModifiedScenesIfUserWantsTo()` returns `false` on Cancel.** Honor it or you silently discard the user's unsaved work: `if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;`.
- **Never edit `.unity` / `.prefab` files by hand** (memory `no-scene-editing`) — the user authors scenes. Inspect them read-only via the `scene-debug` skill, never to "test a fix".

## Custom inspectors
- Base on plain `UnityEditor.Editor`, **not** `OdinEditor`, unless the target type actually uses Odin attributes — `OdinEditor` on a non-Odin class adds overhead and interop risk.
- Read values through `SerializedProperty` / `FindPropertyRelative`, **not** cached public properties — cached props depend on `OnValidate` having run, which isn't guaranteed after a fresh editor session restart; the `SerializedProperty` is always current.
- Inline per-element buttons: wrap `PropertyField` + buttons in `BeginHorizontal`/`EndHorizontal`. Clean only for single-field elements — a foldout that expands into children falls inside the horizontal group and renders broken.

## IMGUI (EditorWindow / OnInspectorGUI)
- **OVDF does not apply to `EditorWindow`** — it only configures MonoBehaviour/ScriptableObject inspectors. Editor windows are raw IMGUI in C#; the `odin-visual-designer` skill can't help here. Don't generate OVDF for an `EditorWindow`.
- Set `GUI.backgroundColor` **before** drawing the control and restore it after — IMGUI reads state at draw time, so setting color after the `GUILayout.Button` call has no effect.
- Cache `GUILayoutOption`s (`GUILayout.Width(...)`) as `static readonly` fields — they allocate per call otherwise (a per-frame allocation in `OnGUI`).
- `EditorGUIUtility.PingObject(asset)` only *flashes* the item; also set `Selection.activeObject = asset` to actually navigate the Project window to it.

## Editor logic inside runtime files
- Wrap editor-only members (e.g. an Odin `[Button]` that calls `AssetDatabase`) in `#if UNITY_EDITOR` and use **fully-qualified** `UnityEditor.*` names — no `using UnityEditor;` at the file top — so the runtime assembly never takes a `UnityEditor` dependency.
- Reordering serialized fields is **safe** (Unity serializes by name, not declaration order) — free to cluster fields under `[Title]` dividers for readability with no data migration. Renames still need `[FormerlySerializedAs("_oldName")]`.

## Reusable patterns
- **Exclusive flag across assets** ("only one can be the default / build config"): `AssetDatabase.FindAssets("t:Type")` → `GUIDToAssetPath` → `LoadAssetAtPath` → clear the flag on every other asset → `EditorUtility.SetDirty(other)` → a single `AssetDatabase.SaveAssets()` at the end. Scope it by comparing a grouping field (`other._chain != _chain → continue`); omit that check for a globally-unique flag. (`scriptable-object-patterns.md` → SetDefault / SetBuildConfig.)
- **Closing multiple scenes**: iterate `EditorSceneManager.sceneCount - 1` down to `0` — forward iteration skips entries as indices shift on each `CloseScene`.
- **Guard additive scene open**: `EditorSceneManager.GetSceneByPath(path).IsValid()` is true when the scene is already loaded; skip it, or `OpenScene(..., Additive)` loads a second copy.
