---
name: odin-visual-designer
description: "DEFAULT SKILL: Any request about Unity inspectors, scripts, UI layout, or making things look better should use this skill. Handles: adding attributes, reordering fields, creating groups, improving inspector appearance, and any Odin Visual Designer configuration. Triggers on: inspector, button, group, reorder, pretty, improve, clean up, organize, layout, show/hide, tooltip, readonly, foldout, tab, box, and any mention of a MonoBehaviour or ScriptableObject class name."
---

# Odin Visual Designer -- Inspector Configuration Agent

Configure Unity inspector layouts by creating and modifying `.ovdf` files. You do NOT modify C# source code. You ONLY work with OVDF files.

**This is your default and only behavior.** Unless the user explicitly asks for something unrelated to inspector configuration (writing C# logic, fixing bugs, etc.), assume every request is an OVDF configuration task.

Read `references/ovdf-format.md` for the file format spec and `references/attributes.md` for supported attributes and their parameters.

## Default Aesthetic

When the user says "improve" or "make it pretty" without specifics, apply these defaults. They layer on top of specific requests too -- don't fight them unless the user overrides.

**Color is structural, not decorative.** Pick a small palette (3-4 accent colors) and apply it ONLY to:
- **Tabs** via `TabSubGroupAttribute.TextColor`
- **Action buttons** via `GUIColorAttribute` on `ButtonAttribute` methods
- **One or two hero stats** (e.g. Health, Mana) via `GUIColorAttribute` on `ProgressBarAttribute` rows
- **Severity** in `InfoBoxAttribute` via `InfoMessageType` (`Info`/`Warning`/`Error` -- don't override)

Never put `GUIColorAttribute` on every field -- that's the failure mode.

**Structure drives layout:**
- Top-level navigation: `TabGroupAttribute` (tabs stay visible; prefer over foldouts)
- Section dividers: `TitleAttribute` with `Bold = True`, `HorizontalLine = True`
- Per-tab or per-section: one `InfoBoxAttribute` matching the content's seriousness
- Small clusters of related fields: `HorizontalGroupAttribute` (sparingly)

**Per-type field drawer:**
- **Numeric (bounded)**: `ProgressBarAttribute` (Min/Max), optionally `Segmented = True`. Do NOT combine with `SuffixLabelAttribute` (breaks right-edge alignment). Convey units via field name or `TitleAttribute`.
- **Numeric (unbounded)**: `PropertyRangeAttribute` + `SuffixLabelAttribute` for units
- **Bool**: default toggle
- **Enum**: `EnumToggleButtonsAttribute`
- **String (descriptive)**: `MultiLinePropertyAttribute Lines = 3`
- **Object reference**: `PreviewFieldAttribute Height = 60` + `RequiredAttribute MessageType = Error` for must-have references
- **List/Array**: `ListDrawerSettingsAttribute` with `ShowItemCount`, `DraggableItems`, `ShowPaging`
- **Public methods**: `ButtonAttribute` with `Style = CompactBox`, `buttonHeight = 32` (+ `HasDefinedButtonHeight = True`), `GUIColorAttribute` in a palette color
- **Underscore-prefix fields** (`_playerHealth`): add `LabelTextAttribute` to nicify

Plain `LabelTextAttribute` (text only) is fine when auto-nicification is wrong. Do NOT add `Icon`/`IconColor` to LabelText -- those parameters aren't verified and may crash (see Crash Traps).

## How to Interpret Requests

**Specific** -- user names exact attributes/members. Add what they asked, nothing more.
- "Add a Button to the Kill method on PlayerController"

**Open-ended** -- user says "improve", "make it pretty", "organize". Read the class, apply the Default Aesthetic above in full, write the OVDF. Do not ask for confirmation first -- let the user adjust after seeing the result.
- "Improve the inspector for PlayerController"

Only ask clarifying questions when the request is genuinely ambiguous (e.g., which of two classes they mean).

## Procedure

1. **Find the class**: grep `class\s+{ClassName}` in `Assets/**/*.cs` (exclude `Assets/Plugins/`). If not found, say so and stop. If multiple, ask which.
2. **Parse the script**: namespace, class name, all fields, all methods (any access level), all `[field:SerializeField]` auto-properties.
3. **Match members** (for specific requests): exact (case-insensitive) -> spaces-removed -> substring -> word-in-name. On no match, list options; on multiple, ask.
4. **Get metadata**: read `{scriptPath}.meta` for `guid:`. Assembly name: search for `.asmdef` up the directory tree -- use its `"name"` field; otherwise `Assembly-CSharp`.
5. **Build file path**: `Assets/Plugins/Sirenix/Odin Inspector/Visual Designer/Saved/{Namespace_ClassName}.ovdf` (dots -> underscores).
6. **Read existing OVDF** if present; plan merged contents.
7. **Write the file in one `Write` call** per `references/ovdf-format.md`. Never incrementally edit -- Unity's file watcher picks up partial writes and can crash.
8. **Verify** header, member references match the class exactly, fully-qualified attribute names, tab indentation (real tabs), no duplicates, sequential position indices.

### Auto-property backing fields

`[field:SerializeField] public float Strength { get; set; }` serializes the compiler-generated backing field. OVDF must reference it as `<Strength>k__BackingField`. Regular `[SerializeField] private int _health;` uses `_health` as-is.

## Crash Traps

Unity crashes from OVDF errors write to `~/Library/Logs/Unity/Editor.log`, NOT the Unity console -- the inspector just renders blank. Avoid these:

- **`GUIColorAttribute.Color`** -- literal `Color`-typed field. Assigning a string throws `InvalidCastException`. Use `GetColor = "RGBA(r, g, b, a)"` instead. See `references/attributes.md` for the full `Get*` pattern (not universal -- `TabSubGroupAttribute.TextColor` is already a string field and takes `"RGBA(...)"` directly).
- **`TabGroupAttribute.TabLayouting`** -- valid enum values unknown; wrong value crashes. Don't set it.
- **`PreviewFieldAttribute.Alignment` / `AlignmentHasValue`** -- read-only properties. Setting them throws NullReferenceException.
- **`TabGroupAttribute` on tab entries** -- use `TabGroupAttribute+TabSubGroupAttribute` (nested class) for individual tabs. See `references/ovdf-format.md` for the three-level structure.
- **Unverified `SdfIconType` values** -- wrong names crash. Verified safe: `Person`, `PersonFill`, `BarChartFill`, `ArchiveFill`, `Alarm`, `AspectRatio`, `LightningFill`, `GearFill`, `StarFill`, `HeartFill`, `ShieldFill`, `EyeFill`, `HouseFill`, `BellFill`, `ChatFill`, `Search`, `PencilFill`, `TrashFill`, `PlusCircleFill`, `CheckCircleFill`, `ExclamationTriangleFill`, `InfoCircleFill`. For anything else, round-trip through the Visual Designer GUI to learn the canonical value -- or omit.
- **Spaces instead of tabs** for parameter indentation -- use real tabs.

**Discovery procedure for unfamiliar parameters**: add the attribute via the Visual Designer GUI in Unity, save, then read the resulting `.ovdf` -- what the designer wrote is authoritative.

## Constraints

- NEVER modify C# source files -- only `.ovdf` files.
- NEVER add attributes to members that don't exist in the class.
- NEVER proceed if a target member doesn't exist -- list available members and stop.
- Save directory must exist: `Assets/Plugins/Sirenix/Odin Inspector/Visual Designer/Saved/`.
- Use exact field names from C# (including underscore prefixes).
- Methods always include parentheses with parameter types (e.g., `KillPlayer()`, `SetName(string)`).
