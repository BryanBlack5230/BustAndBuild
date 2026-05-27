# ScriptableObject Patterns

## Editor-Only Buttons in ScriptableObjects
**Context:** Adding "Set Default" button to `RunConfiguration` with cross-asset logic.  
**Finding:** Wrap the method in `#if UNITY_EDITOR` and use fully-qualified `UnityEditor.*` names — no `using UnityEditor` needed at file top, keeping runtime compilation clean.
```csharp
#if UNITY_EDITOR
[Button("Set Default")]
private void SetDefault()
{
    _isDefault = true;
    UnityEditor.EditorUtility.SetDirty(this);
    // ...
    UnityEditor.AssetDatabase.SaveAssets();
}
#endif
```
**Why it matters:** Avoids adding a runtime dependency on `UnityEditor` assembly.

## [ReadOnly] vs [HideInInspector] for Non-Editable Fields
**Context:** `_isDefault` should be visible but not directly editable.  
**Finding:** User prefers Odin's `[ReadOnly]` — field stays visible in inspector but greyed out. Unity's `[HideInInspector]` hides it completely. Both keep `[SerializeField]` so the value persists.  
**Why it matters:** Use `[ReadOnly]` when the value is informational; use `[HideInInspector]` only when the field is truly internal.

## SetDefault Pattern — Exclusive Flag Across Assets (Chain-Scoped)
**Context:** `RunConfiguration.SetDefault()` must clear the flag on all sibling configs sharing the same chain.  
**Finding:** Load all assets of the same type, compare the shared reference field for equality, mark dirty, then call `SaveAssets()` once at the end.
```csharp
var guids = UnityEditor.AssetDatabase.FindAssets("t:RunConfiguration");
foreach (var guid in guids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var other = UnityEditor.AssetDatabase.LoadAssetAtPath<RunConfiguration>(path);
    if (other == null || other == this || other._chain != _chain) continue;
    other._isDefault = false;
    UnityEditor.EditorUtility.SetDirty(other);
}
UnityEditor.AssetDatabase.SaveAssets();
```
**Why it matters:** Reusable pattern for any "exclusive selection" flag on ScriptableObject assets.

## SetBuildConfig Pattern — Global Exclusive Flag (No Scoping)
**Context:** `RunConfiguration.SetBuildConfig()` — only one config in the entire project can be the build entry point.  
**Finding:** Same pattern as `SetDefault` but omit the chain equality check — clears the flag on every other asset of the type globally.
```csharp
if (other == null || other == this) continue; // no chain check
other._isBuildConfig = false;
```
**Why it matters:** When a flag must be globally unique (not scoped to a sub-group), drop the equality guard on the grouping field.
