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

## Odin [PropertyOrder] to Hoist Buttons Above Fields
**Context:** Moving "Set Default" / "Set Build Config" buttons to the top of `RunConfiguration` inspector.  
**Finding:** Add `[PropertyOrder(-1)]` to `[Button]` methods — fields without an explicit order default to 0, so any negative value places the button above them.  
**Why it matters:** No custom editor needed; one attribute line is enough to reorder any Odin-drawn member.

## Odin [ShowIf] Self-Reference on Bool Fields
**Context:** Hiding `_isDefault` / `_isBuildConfig` when their value is `false`.  
**Finding:** `[ShowIf("_isDefault")]` on `_isDefault` itself shows the field only when its own value is `true`. Self-referencing works for any bool field. When all members of a `[HorizontalGroup]` are hidden, the group row disappears entirely.  
**Why it matters:** Avoids cluttering the inspector with flags that are irrelevant most of the time.

## Odin [GUIColor] for Per-Field Tinting — User Colour Preferences
**Context:** Colouring flag fields green; user revised to distinct colours per flag.  
**Finding:** `[GUIColor(r, g, b)]` tints the full control (label + widget). User chose cyan `(0.5f, 1f, 1f)` for `_isDefault` and orange/gold `(1f, 0.7f, 0f)` for `_isBuildConfig` — different colours per flag, not uniform. Suggest distinct colours, not the same shade for all flags.  
**Why it matters:** Distinct colours make each flag instantly recognisable at a glance.

## Odin [HorizontalGroup] Collapses When All Members Hidden
**Context:** `_isDefault` and `_isBuildConfig` share `[HorizontalGroup("Flags")]` and both use `[ShowIf]`.  
**Finding:** When all fields in a `[HorizontalGroup]` are hidden by `[ShowIf]`, the row itself vanishes — no empty gap left behind.  
**Why it matters:** Safe to combine `[HorizontalGroup]` with `[ShowIf]`; no need to hide the group separately.

## One Unity-Serialized Class Per File — Silent Inspector Wiring Failure
> **Rule** lives in `unity-coding-standards` P1.10 — this learning is the *mechanism + recovery* behind it.

**Context:** Initial hit-feedback work put three `ScriptableObject` types (`FlashProfile`, `SquashProfile`, `PushProfile`) in one `HitFeedbackProfiles.cs` file. SO assets created from the Create menu. User dragged them into `HitFeedbackAuthoring`'s inspector slots — drops appeared to work. Squash and push blocks never fired at runtime; flash worked (because its baker reads color from material, independent of the SO reference).
**Finding:** Unity's serialization links inspector references to types by **file path matching the class name**. Multiple `MonoBehaviour` or `ScriptableObject` types in one file → references silently fail to persist through bake (no compile error, no runtime error, drag-drop appears to "stick" in the inspector but the link is dead).
```
HitFeedbackProfiles.cs   ← BROKEN: three SOs in one file
   ↓
FlashProfileSO.cs        ← FIXED: one SO per file
SquashProfileSO.cs
PushProfileSO.cs
```
Use `[FormerlySerializedAs("_oldName")]` on the new field names if you also rename the fields, to preserve already-assigned inspector references.

## SetBuildConfig Pattern — Global Exclusive Flag (No Scoping)
**Context:** `RunConfiguration.SetBuildConfig()` — only one config in the entire project can be the build entry point.  
**Finding:** Same pattern as `SetDefault` but omit the chain equality check — clears the flag on every other asset of the type globally.
```csharp
if (other == null || other == this) continue; // no chain check
other._isBuildConfig = false;
```
**Why it matters:** When a flag must be globally unique (not scoped to a sub-group), drop the equality guard on the grouping field.

## Config Inspector Readability Convention (Odin)
**Context:** Styling ConfigHub + ~17 config SO/struct types in one pass for readability.
**Finding:** Project vocabulary for tunable configs — reuse instead of re-deciding per field:
- Bounded `[0,1]` values (shares, elasticity, damage scale) → `[PropertyRange(0f, 1f)]`
- Real-world units → `[SuffixLabel("s"|"m"|"m/s"|"HP"|"dmg"|"x"|"u/px", Overlay = true)]` — **ASCII only** (avoid `°`/`²` in source; use `deg`/`m/s2`)
- Must-have object refs → `[Required]`; bools → `[ToggleLeft]`; small enums → `[EnumToggleButtons]`
- Section headers → `[Title]`; one `[InfoBox]` only where a mechanic is a non-obvious *sequence* (e.g. flash white→red→normal)
- Do NOT pair `SuffixLabel` with a slider (`Range`/`ProgressBar`) — right-edge alignment breaks.
**Why it matters:** Consistent look across all configs; new configs inherit the same drawer choices.

## Odin Attributes on ECS IComponentData Structs Are Safe
**Context:** Added `[PropertyRange]`/`[SuffixLabel]`/`[Title]` to `BounceConfig`/`SteeringConfig`/`BattleBrainConfig` (all `[Serializable] struct : IComponentData`, drawn inline on ConfigHub).
**Finding:** Sirenix attributes are editor-only metadata — zero impact on baking, Burst, or blittability. Adding `using Sirenix.OdinInspector;` to Component files compiles fine (the `Runtime` asmdef already references Sirenix).
**Why it matters:** Style ECS tuning structs exactly like SOs without fear of breaking DOTS.

## Reordering Serialized Fields Is Safe (Name-Keyed)
**Context:** Regrouped `CameraConfigSO` fields into Drag/Return/Border clusters to add clean `[Title]` dividers.
**Finding:** Unity serializes by field **name**, not declaration order — reordering C# fields keeps existing asset values intact; only inspector display order changes. (Renames still need `[FormerlySerializedAs]`; reorders don't.)
**Why it matters:** Free to cluster fields under section titles for readability with no data migration.
