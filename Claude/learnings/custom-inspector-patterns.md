# Custom Inspector Patterns

## Custom Editor for Plain-Unity SO (No Odin)
**Context:** Adding "Open Chain" / per-element "Open Scene" buttons to `SceneChain` inspector.  
**Finding:** Create `[CustomEditor(typeof(SceneChain))]` inheriting `UnityEditor.Editor` in the Editor folder. Use `SerializedProperty` manually — do NOT extend `OdinEditor` unless the target class uses Odin attributes.  
**Why it matters:** Using `OdinEditor` as base for a non-Odin class adds unnecessary overhead; plain `Editor` is sufficient and avoids interop issues.

## FindPropertyRelative Over Public Cached Properties
**Context:** Needed scene paths for each `SceneChainElement` inside the inspector buttons.  
**Finding:** Use `element.FindPropertyRelative("_sceneAsset")?.objectReferenceValue as SceneAsset` + `AssetDatabase.GetAssetPath(sceneAsset)` rather than `chain.ScenePaths[i]`. The public cached property depends on `OnValidate` having been called, which isn't guaranteed after a fresh editor session restart.  
**Why it matters:** Going through `SerializedProperty` directly is always current; cached properties may be stale.

## Inline Per-Element Buttons in a Manually Drawn List
**Context:** Each `SceneChainElement` row needed "Open additively" and "Open single" buttons.  
**Finding:** Wrap the `PropertyField` + buttons in `BeginHorizontal` / `EndHorizontal`. Works cleanly when each element has only one visible field (no child foldout). If the element expands into children, the expanded content will fall inside the horizontal group and look broken.
```csharp
EditorGUILayout.BeginHorizontal();
EditorGUILayout.PropertyField(sceneAssetProp, new GUIContent($"Element {i}"));
if (GUILayout.Button("Open additively", OpenAdditiveButtonWidth)) ...
if (GUILayout.Button("Open single", OpenSingleButtonWidth)) ...
EditorGUILayout.EndHorizontal();
```
**Why it matters:** Simple and correct for single-field elements; know the limitation for foldout elements.

## Checking if a Scene Is Already Open
**Context:** "Open additively" should skip scenes already loaded in the editor.  
**Finding:** `EditorSceneManager.GetSceneByPath(path).IsValid()` returns true if the scene is currently open. Use this guard before calling `OpenScene` with `Additive` mode to prevent duplicate scene loads.  
**Why it matters:** `OpenScene(Additive)` on an already-open path loads a second copy — always guard with `IsValid()`.

## Backwards Iteration When Closing Multiple Scenes
**Context:** "Open Chain" closes all scenes not in the chain.  
**Finding:** Iterate `EditorSceneManager.sceneCount - 1` down to `0` when calling `CloseScene`. Forward iteration shifts indices after each close, causing scenes to be skipped.  
**Why it matters:** Standard pattern whenever removing items from an indexed collection while iterating.

## SaveCurrentModifiedScenesIfUserWantsTo Returns Bool — Use It to Abort
**Context:** "Open Chain" and "Open single" need to prompt for unsaved changes before replacing scenes.  
**Finding:** `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` returns `false` when the user clicks "Cancel". Check the return value and early-return to abort the operation cleanly.
```csharp
if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
```
**Why it matters:** Ignoring the return value silently discards unsaved work when the user intended to cancel.
