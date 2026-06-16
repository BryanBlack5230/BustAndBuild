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

## Odin Visual Designer (OVDF) — Layout Without Touching C#
**Context:** Tabbed inspectors for `ConfigHub` + `EnemyUnitProfile`/`AllyUnitProfile` via `.ovdf` files in `Assets/Plugins/Sirenix/Odin Inspector/Visual Designer/Saved/`.
**Finding:** OVDF configures a type's inspector without editing source (reselect the object to reload after a write). Naming/structure rules that bite:
- **Global-namespace classes** (most ECS/SO files here) → filename + header use the **bare class name**, assembly `Runtime` (e.g. `EnemyUnitProfile.ovdf`, header `EnemyUnitProfile, Runtime`). Namespaced types use dots→underscores (`BarkingBird_Runtime_Infrastructure_Settings_ConfigHub.ovdf`). `MetaGuid:` line is optional but include it.
- **Tabs** = three-level groups: parent `TabGroupAttribute` → one `TabGroupAttribute+TabSubGroupAttribute` per tab → members positioned `$tabId:N`. Indent params with **real tabs**, not spaces.
**Why it matters:** Layout work stays read-only on C#; gets file naming right the first time.

## OVDF Colors Must Be Hex on This Machine (comma-decimal locale)
**Context:** Three "Could not parse red '0.400'..." errors on the ConfigHub tab `TextColor`s.
**Finding:** This machine's locale uses a **comma** decimal separator. Sirenix's `RGBA(r,g,b,a)` parser splits on commas, then parses each float with the current culture — so `0.400` (period) fails, and the comma can't be both decimal point and separator. Use **hex** (`TextColor = "#RRGGBBAA"`), which is culture-invariant, for all OVDF colors.
**Why it matters:** The Odin skill's reference suggests `RGBA(...)`; that form silently errors here. Always reach for hex.

## OVDF Only ADDS Attributes — Cannot Remove/Override Inline Ones
**Context:** Inline `[Title("Throw & Daylight")]` stayed visible after `DaylightConfig` moved to a different tab.
**Finding:** OVDF layers attributes on top of the C# source; it can't suppress or rewrite an inline attribute. A hardcoded `[Title]`/`[Header]` text leftover is only fixable by a C# edit (outside the Odin skill's scope).
**Why it matters:** Plan tab/section splits around the inline attributes already in the source; flag unavoidable label warts rather than trying to OVDF them away.

## Empty OVDF Tab/Group Entries Are Culled
**Context:** A placeholder "City" tab with no fields, and an Ally "Drops" tab (the SO has no `Drops` field).
**Finding:** A `TabSubGroup` (or any group) with zero positioned members does NOT render — the tab button never appears, and its `TextColor` isn't even parsed (3 colors errored, not 4). It auto-appears once a member is positioned into it.
**Why it matters:** You can't reserve a *visible* empty placeholder tab via OVDF; anchor at least one member or accept it shows up only later.

## OVDF Layout Propagates Into [InlineEditor]
**Context:** Profile SOs render inline in the ConfigHub Battle tab via `[InlineEditor(Foldout)]`.
**Finding:** OVDF config is type-level, so a type's tabs/layout also render when it's drawn inside another inspector's `[InlineEditor]` (nested, a bit cramped). Opening the asset directly gives full width.
**Why it matters:** Configure the type once; the layout shows everywhere it's drawn — no per-host work.

## Odin [MinMaxSlider] — a String Arg Is a Value-Resolver, Not a Literal
**Context:** `[MinMaxSlider(1f, "Max", true)]` on `DropTableEntry.minMaxCount` threw "Could not match the given string 'Max' to any possible value resolution in the context of the type 'DropTableEntry'".
**Finding:** `MinMaxSlider` has overloads where a **string** argument is a *getter* (member name or `@`-expression), NOT a literal. `(1f, "Max", true)` binds to `(float minValue, string maxValueGetter, bool showFields)`, so Odin tries to resolve `"Max"` as a member and fails. Fixes: numeric literal `[MinMaxSlider(1f, 10f, true)]`, an `@`-expression `[MinMaxSlider(1f, "@100f", true)]` (literal-via-expression), or a real member name. Same trap on any Odin param that accepts "value OR member name" (`ProgressBar.Max/MinMember`, `PropertyRange.Min/MaxMember`, `GUIColor.GetColor`, etc.).
**Why it matters:** The error renders the inspector inert with a cryptic message; recognise the value-resolver pattern on sight.

## C# Odin Attribute Floats Are Locale-Safe (Only OVDF Strings Aren't)
**Context:** Worried the comma-decimal locale (see OVDF color note) would break `[GUIColor(0.4f, 0.8f, 1f)]` in ConfigHub.cs.
**Finding:** The locale bug only hits Sirenix's **runtime string parser** (`RGBA("0.4,...")` written in OVDF). C# float literals compile culture-invariantly, so `[GUIColor(0.4f, 0.8f, 1f)]`, `[PropertyRange(0f, 1f)]`, etc. in source are always safe. Prefer the C# attribute when you can; only OVDF needs the hex workaround.
**Why it matters:** Don't avoid C# colour/number attributes out of locale fear — the hex workaround is OVDF-only.

## OVDF Tabs: MultiRow Works Here; Nested TabGroups Need a GroupName
**Context:** Rebuilding the ConfigHub OVDF; the Odin skill ref warns "don't set TabLayouting".
**Finding:** `TabLayouting = MultiRow` is valid and renders fine in this project's Sirenix version (worth it for 5+ tabs) — the skill's warning is about *unknown* enum values, and MultiRow is now known-good here. A **nested** `TabGroupAttribute` (a tab group placed inside another tab) still needs its own `GroupName`; a group entry without one is a latent bug. The Visual Designer also writes some `Position:` refs quoted-without-`$` (`Position: "groupId":0`) alongside the documented `$groupId:N`; both resolve, but normalise to `$id:N` when hand-authoring.
**Why it matters:** MultiRow is safe to use here despite the generic warning; the missing GroupName and Position-syntax drift are the real foot-guns.
