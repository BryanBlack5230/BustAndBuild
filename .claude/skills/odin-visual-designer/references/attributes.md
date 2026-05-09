# Odin Inspector Attributes Reference for OVDF

All attributes use the fully qualified name: `Sirenix.OdinInspector.{Name}Attribute`

## Buttons

### ButtonAttribute
Adds a clickable button for a method in the inspector.

| Parameter | Type | HasDefined Flag | Description |
|-----------|------|-----------------|-------------|
| `Name` | string | -- | Custom button label |
| `Style` | enum | -- | `FoldoutButton`, `CompactBox`, `Box` |
| `Expanded` | bool | -- | Show parameters without foldout |
| `DirtyOnClick` | bool | -- | Mark object dirty on click (default: true) |
| `Icon` | enum | -- | SdfIconType icon name (see safe list below) |
| `buttonIconAlignment` | enum | `HasDefinedButtonIconAlignment` | `LeftOfText`, `RightOfText` |
| `buttonHeight` | int | `HasDefinedButtonHeight` | Height in pixels |
| `buttonAlignment` | float | `HasDefinedButtonAlignment` | 0=left, 0.5=center, 1=right |
| `stretch` | bool | `HasDefinedStretch` | Fill available width |
| `drawResult` | bool | `drawResultIsSet` | Show method return value |

### InlineButtonAttribute
Draws a small button next to a property.

| Parameter | Type | Description |
|-----------|------|-------------|
| `MemberMethod` | string | Method to invoke |
| `Label` | string | Button label |
| `Icon` | enum | SdfIconType icon |
| `ShowIf` | string | Condition for visibility |

## Conditional Display

### ShowIfAttribute / HideIfAttribute
Show or hide a member based on a condition.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Condition` | string | Member name, expression, or `@` expression |
| `Value` | object | Optional value to compare against |
| `Animate` | bool | Animate visibility change |

Example: `Condition = "_isAdvanced"` or `Condition = "_mode"` with `Value = 1`

### EnableIfAttribute / DisableIfAttribute
Enable or disable editing based on a condition.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Condition` | string | Member name or expression |
| `Value` | object | Optional comparison value |

### ShowInAttribute / HideInAttribute / DisableInAttribute / EnableInAttribute
Control visibility based on context (PrefabAssets, PrefabInstances, NonPrefabs, etc.).

| Parameter | Type | Description |
|-----------|------|-------------|
| `PrefabKind` | enum | Which prefab contexts to apply to |

## Validation

### RequiredAttribute
Error/warning if value is null or missing.

| Parameter | Type | Description |
|-----------|------|-------------|
| `ErrorMessage` | string | Custom message |
| `MessageType` | enum | `Info`, `Warning`, `Error` |

### ValidateInputAttribute
Custom validation via a method.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Condition` | string | Validation method name |
| `DefaultMessage` | string | Message on failure |
| `MessageType` | enum | `Info`, `Warning`, `Error` |
| `IncludeChildren` | bool | Validate child properties too |
| `ContinuousValidationCheck` | bool | Validate every frame |

### MinValueAttribute / MaxValueAttribute
Clamp numeric values.

| Parameter | Type | Description |
|-----------|------|-------------|
| `MinValue` / `MaxValue` | double | Limit value |
| `Expression` | string | Member/expression for dynamic limit |

### PropertyRangeAttribute
Slider between min and max.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Min` | double | Minimum |
| `Max` | double | Maximum |
| `MinMember` | string | Member for dynamic min |
| `MaxMember` | string | Member for dynamic max |

## Visual / Display

### ReadOnlyAttribute
Prevents editing in inspector. No parameters.

### InfoBoxAttribute
Displays a message box above a property.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Message` | string | Text (supports `$member` references) |
| `InfoMessageType` | enum | `None`, `Info`, `Warning`, `Error` |
| `VisibleIf` | string | Condition for showing the box |
| `GUIAlwaysEnabled` | bool | Show even when GUI disabled |
| `Icon` | enum | SdfIconType override |
| `IconColor` | string | Color (name, hex, RGB) |

### LabelTextAttribute
Changes the display label.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Text` | string | New label text |
| `NicifyText` | bool | Auto-format text |
| `Icon` | enum | SdfIconType icon |
| `IconColor` | string | Icon color |

### PropertyTooltipAttribute
Adds a hover tooltip.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Tooltip` | string | Tooltip text |

### TitleAttribute
Draws a title above a property.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Title` | string | Title text |
| `Subtitle` | string | Subtitle text |
| `TitleAlignment` | enum | `Left`, `Centered`, `Right` |
| `HorizontalLine` | bool | Draw line (default: true) |
| `Bold` | bool | Bold title (default: true) |

### HideLabelAttribute
Hides the property label. No parameters.

### PropertyOrderAttribute
Controls display order.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Order` | float | Lower = earlier |

### PropertySpaceAttribute
Adds spacing.

| Parameter | Type | Description |
|-----------|------|-------------|
| `SpaceBefore` | float | Pixels before (default: 8) |
| `SpaceAfter` | float | Pixels after |

### IndentAttribute
Indents a property.

| Parameter | Type | Description |
|-----------|------|-------------|
| `IndentLevel` | int | Indent level |

### GUIColorAttribute
Changes the GUI color for a property.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Color` | Color | **DO NOT SET FROM OVDF.** This is a literal `UnityEngine.Color`, not a string. Assigning a string to it (e.g. `Color = "RGBA(...)"`) throws `InvalidCastException` in `MutablePropertyInfo.ApplyPatch` and the inspector renders BLANK with no Unity console error. |
| `GetColor` | string | **Use this from OVDF.** Either a member name on the class returning a Color, OR a literal in the form `"RGBA(r, g, b, a)"` (Sirenix parses it). Example: `GetColor = "RGBA(0.3, 0.85, 1.0, 1.0)"`. |

**Verified working OVDF form** (round-tripped through the Visual Designer GUI):
```
+ [Sirenix.OdinInspector.GUIColorAttribute]
	GetColor = "RGBA(0.000, 1.000, 1.000, 1.000)"
```

The Visual Designer also writes a `Color = #FFFFFF00` line alongside `GetColor`, but that line is the unset literal field (alpha 0 = inactive); only `GetColor` carries the actual color when written by hand.

**General lesson -- the `Get*` pattern (NOT universal).** Some Sirenix attributes split a color/icon into TWO fields: a literal-typed field (`Color`, `Icon`, `IconColor`) and a sibling string `Get*` field. From OVDF you can only set strings -- assigning a string to a literal-typed field throws `InvalidCastException` inside `MutablePropertyInfo.ApplyPatch` and the inspector renders blank. But the pattern is NOT universal: `TabSubGroupAttribute.TextColor` is already a plain string field and accepts `"RGBA(...)"` directly, no `Get*` needed.

Confirmed via Visual Designer round-trips:
- `GUIColorAttribute`: literal `Color` is a `Color`-type field (do not set); use `GetColor = "RGBA(...)"` instead.
- `TabSubGroupAttribute.TextColor`: already a `string` field; set directly as `TextColor = "RGBA(...)"`.

Unconfirmed (omit until round-tripped):
- `LabelTextAttribute.Icon` / `IconColor`
- `InfoBoxAttribute.Icon` / `IconColor`

Authoritative discovery procedure: add the attribute via the Visual Designer GUI in Unity, save, then read the resulting `.ovdf` -- the field names and value format the designer wrote are the answer.

### ProgressBarAttribute
Shows a progress bar.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Min` | double | Minimum value |
| `Max` | double | Maximum value |
| `MinMember` | string | Dynamic min member |
| `MaxMember` | string | Dynamic max member |
| `ColorMember` | string | Dynamic color member |
| `BackgroundColorMember` | string | Dynamic background color |
| `Segmented` | bool | Show as segmented bar |

### SuffixLabelAttribute
Shows a suffix after the value field.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Label` | string | Suffix text |
| `Overlay` | bool | Overlay on field (default: false) |

## Groups

All group attributes inherit from PropertyGroupAttribute. They require a `GroupName` parameter.

### BoxGroupAttribute
Boxed container with optional label.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier/label |
| `ShowLabel` | bool | Show the label (default: true) |
| `CenterLabel` | bool | Center the label |
| `LabelText` | string | Override display label |

### FoldoutGroupAttribute
Collapsible section.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier/label |
| `Expanded` | bool | Default expanded state |
| `HasDefinedExpanded` | bool | Must be True if Expanded is set |

### TabGroupAttribute
Tabbed interface. Uses a **three-level structure**: parent group -> tab sub-groups -> members.

**Parent group** (`TabGroupAttribute`):

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier |

**Do NOT set `TabLayouting`** -- the valid enum values are unknown and wrong values crash Unity.

**Tab entries** (`TabGroupAttribute+TabSubGroupAttribute` -- note the `+` nested class syntax):

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Internal group identifier for this tab |
| `Name` | string | Display label for the tab |
| `Icon` | enum | SdfIconType icon on the tab |
| `TextColor` | string | Tab text color, e.g., `"RGBA(0.156, 0.581, 0.156, 1.000)"` |

**Structure:**
```
# $parentId          <- TabGroupAttribute (GroupName = "MyTabs")
# $tab1Id            <- TabSubGroupAttribute, Position: $parentId:0
# $tab2Id            <- TabSubGroupAttribute, Position: $parentId:1
# field1             <- Position: $tab1Id:0
# field2             <- Position: $tab2Id:0
```

**IMPORTANT:** Tab entries use `TabGroupAttribute+TabSubGroupAttribute`, NOT `TabGroupAttribute`. Using `TabGroupAttribute` directly on tab entries will crash Unity.

### TitleGroupAttribute
Titled section with optional subtitle and line.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Title text |
| `Subtitle` | string | Subtitle text |
| `Alignment` | enum | `Left`, `Centered`, `Right` |
| `HorizontalLine` | bool | Draw line (default: true) |
| `BoldTitle` | bool | Bold title (default: true) |
| `Indent` | bool | Indent members |

### HorizontalGroupAttribute
Side-by-side layout.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier |
| `Width` | float | 0-1=percentage, >1=pixels, 0=auto |
| `MarginLeft` | int | Left margin |
| `MarginRight` | int | Right margin |
| `PaddingLeft` | int | Left padding |
| `PaddingRight` | int | Right padding |
| `MinWidth` | float | Min width constraint |
| `MaxWidth` | float | Max width constraint |
| `Gap` | float | Space between columns |
| `Title` | string | Optional header |

### VerticalGroupAttribute
Vertical layout container.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier |
| `PaddingTop` | float | Top padding |
| `PaddingBottom` | float | Bottom padding |

### ToggleGroupAttribute
Group with a toggle to enable/disable.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier |
| `ToggleMemberName` | string | Bool member controlling the toggle |
| `CollapseOthersOnExpand` | bool | Collapse other toggle groups |

### ButtonGroupAttribute
Groups buttons horizontally.

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupName` | string | Group identifier |

## Type-Specific

### ValueDropdownAttribute
Custom dropdown values.

| Parameter | Type | Description |
|-----------|------|-------------|
| `MemberName` | string | Member returning values |
| `AppendNextDrawer` | bool | Show field after dropdown |
| `DisableGUIInAppendedDrawer` | bool | Disable appended field |
| `DoubleClickToConfirm` | bool | Require double-click |
| `FlattenTreeView` | bool | Flat list vs tree |
| `NumberOfItemsBeforeEnablingSearch` | int | Auto-search threshold |

### EnumToggleButtonsAttribute
Shows enum as toggle buttons. No parameters.

### PreviewFieldAttribute
Shows image/object preview.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Height` | float | Preview height in pixels |
| `FilterMode` | enum | Image filter mode |
| `PreviewGetter` | string | Resolved value for custom preview texture |

**Note:** `Alignment` and `AlignmentHasValue` are read-only properties and CANNOT be set via OVDF. Only `Height`, `FilterMode`, and `PreviewGetter` are settable.

### FilePathAttribute / FolderPathAttribute
Path picker.

| Parameter | Type | Description |
|-----------|------|-------------|
| `ParentFolder` | string | Starting folder |
| `Extensions` | string | Allowed extensions |
| `AbsolutePath` | bool | Use absolute paths |
| `RequireExistingPath` | bool | Must exist |

### MultiLinePropertyAttribute
Multi-line text input.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Lines` | int | Number of lines |

## Collections

### ListDrawerSettingsAttribute
Customize list display.

| Parameter | Type | Description |
|-----------|------|-------------|
| `ShowFoldout` | bool | Show foldout |
| `ShowPaging` | bool | Enable paging |
| `ShowItemCount` | bool | Show count |
| `NumberOfItemsPerPage` | int | Items per page |
| `DraggableItems` | bool | Allow drag reorder |
| `IsReadOnly` | bool | Prevent modification |
| `HideAddButton` | bool | Hide add button |
| `HideRemoveButton` | bool | Hide remove button |

### TableListAttribute
Display list as table.

| Parameter | Type | Description |
|-----------|------|-------------|
| `IsReadOnly` | bool | Prevent editing |
| `MinScrollViewHeight` | int | Min scroll height |
| `MaxScrollViewHeight` | int | Max scroll height |
| `ShowPaging` | bool | Enable paging |
| `NumberOfItemsPerPage` | int | Items per page |

### DictionaryDrawerSettingsAttribute
Customize dictionary display.

| Parameter | Type | Description |
|-----------|------|-------------|
| `IsReadOnly` | bool | Prevent editing |
| `DisplayMode` | enum | Display format |

## Callbacks

### OnValueChangedAttribute
Invoke method when value changes.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Action` | string | Method name to call |
| `IncludeChildren` | bool | Track child changes |

### OnInspectorInitAttribute / OnInspectorDisposeAttribute
Run code when inspector opens/closes.

| Parameter | Type | Description |
|-----------|------|-------------|
| `Action` | string | Method or `@` expression |
