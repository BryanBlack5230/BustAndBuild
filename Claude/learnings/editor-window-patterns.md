# Editor Window Patterns

## Odin OVDF Does Not Apply to EditorWindows
**Context:** Asked to improve `SceneWorkflowToolbox` inspector using Odin.  
**Finding:** OVDF files only configure MonoBehaviour/ScriptableObject inspectors. `EditorWindow` uses raw IMGUI — must modify C# directly.  
**Why it matters:** Don't waste time generating OVDF for any class that extends `EditorWindow`.

## IMGUI Button Highlight via GUI.backgroundColor
**Context:** Replacing a Popup dropdown with a list of selection buttons.  
**Finding:** Set `GUI.backgroundColor` **before** drawing the button, restore **after**. Cache `GUILayoutOption` as a static field to avoid per-frame allocation.
```csharp
private static readonly Color SelectedColor = new Color(0.33f, 0.34f, 0.73f);
private static readonly GUILayoutOption LocateButtonWidth = GUILayout.Width(55f);

var prev = GUI.backgroundColor;
GUI.backgroundColor = isSelected ? SelectedColor : prev;
if (GUILayout.Button(label)) { ... }
GUI.backgroundColor = prev;
```
**Why it matters:** Setting color after the button call has no effect — IMGUI reads state at draw time.

## Locate / Ping Asset in Project Window
**Context:** Adding a "Locate" button next to each RunConfiguration in the toolbox.  
**Finding:** `PingObject` alone only flashes the item; `Selection.activeObject` is needed to actually select it.
```csharp
Selection.activeObject = asset;
EditorGUIUtility.PingObject(asset);
```
**Why it matters:** Without `Selection.activeObject`, the Project window doesn't navigate to the asset.

## SceneWorkflowHandoff — Null Guard Preserves Unity Default Play
**Context:** Removing "Auto" mode — when no config is selected, Play should behave like stock Unity.  
**Finding:** `UpdateHandoff()` guards `if (config == null) return` before writing to SessionState. When `_selectedIndex == 0` and no configs exist, `GetEffectiveConfig()` returns null → handoff is never written → Unity runs normally. `SceneWorkflowHandoff.Clear()` exists if an explicit reset is ever needed.  
**Why it matters:** No need to call `Clear()` defensively; the null guard is sufficient.
