# OVDF File Format Specification (v1.1)

Odin Visual Designer Files (.ovdf) store inspector attribute configurations for Unity C# types without modifying source code.

## File Location

`Assets/Plugins/Sirenix/Odin Inspector/Visual Designer/Saved/`

## File Naming

`{FullTypeName with dots replaced by underscores}.ovdf`

Examples:
- `Samples.SampleBehaviour` -> `Samples_SampleBehaviour.ovdf`
- `MyClass` (no namespace) -> `MyClass.ovdf`
- `Game.Player.PlayerController` -> `Game_Player_PlayerController.ovdf`

## Structure

### Header (lines 1-4)

```
OVDF v1.1
{Namespace.ClassName}, {AssemblyName}
MetaGuid:{guid}

```

- Line 1: Format version, always `OVDF v1.1`
- Line 2: Fully qualified type name + comma + space + assembly name
  - Assembly is `Assembly-CSharp` for scripts in `Assets/` (unless inside an Assembly Definition)
  - If the class has no namespace, just `ClassName, Assembly-CSharp`
- Line 3: `MetaGuid:` followed by the `guid` value from the `.cs.meta` file (no spaces)
- Line 4: Empty line (separator before member entries)

### Member Entries

Each entry starts with `# {member reference}`:

```
# fieldName                    -- regular field ([SerializeField] private int _health)
# <PropertyName>k__BackingField -- auto-property backing field ([field:SerializeField] public int Health { get; set; })
# MethodName()                 -- method with no parameters
# MethodName(string)           -- method with one parameter
# MethodName(int, string)      -- method with multiple parameters
```

**Auto-property backing fields:** When a property uses `[field:SerializeField]`, the OVDF must reference the compiler-generated backing field name `<PropertyName>k__BackingField`, NOT the property name. This is critical -- using the property name will not work.

Parameter types use C# short names: `string`, `int`, `float`, `bool`, etc.

### Attribute Directives

Add an attribute to a member with `+ [FullAttributeName]`:

```
# MethodName()
+ [Sirenix.OdinInspector.ButtonAttribute]
```

### Attribute Parameters

Tab-indented key-value pairs on lines following the attribute:

```
# MethodName()
+ [Sirenix.OdinInspector.ButtonAttribute]
	Name = "Custom Label"
	Style = CompactBox
	buttonHeight = 100
	HasDefinedButtonHeight = True
```

Value formats:
- Strings: quoted with double quotes (`Name = "My Button"`)
- Booleans: `True` or `False` (capital first letter)
- Numbers: unquoted (`buttonHeight = 100`)
- Enums: unquoted enum member name (`Style = CompactBox`)

**Important:** Some parameters have a companion `HasDefined{ParamName} = True` flag that MUST be set when the parameter is specified. Without the flag, the parameter is ignored.

### Positioning

Controls member display order in the inspector:

```
# _sampleString
Position: $root:0
```

- `$root:N` -- position N at the root level (0-indexed)
- `$groupId:N` -- position N within a group

### Groups

Groups are virtual entries (not tied to a C# member) that use generated IDs. The ID is a `$` followed by a 22-character random alphanumeric string.

**Step 1: Define the group entry:**
```
# $6tt3ETTBQRrzVP7AMAwHbR
Position: $root:3
+ [Sirenix.OdinInspector.TitleGroupAttribute]
	GroupName = "String Buttons"
```

**Step 2: Place members inside the group** using `Position: $groupId:index`:
```
# ChangeString(string)
Position: $6tt3ETTBQRrzVP7AMAwHbR:0
+ [Sirenix.OdinInspector.ButtonAttribute]

# RandomString()
Position: $6tt3ETTBQRrzVP7AMAwHbR:1
+ [Sirenix.OdinInspector.ButtonAttribute]
```

**Tab groups** use a three-level structure with a nested class `TabSubGroupAttribute`:

```
# $parentTabGroupId
Position: $root:0
+ [Sirenix.OdinInspector.TabGroupAttribute]
	GroupName = "MyTabs"

# $tab1Id
Position: $parentTabGroupId:0
+ [Sirenix.OdinInspector.TabGroupAttribute+TabSubGroupAttribute]
	GroupName = "Tab1InternalName"
	Name = "Display Label"

# $tab2Id
Position: $parentTabGroupId:1
+ [Sirenix.OdinInspector.TabGroupAttribute+TabSubGroupAttribute]
	GroupName = "Tab2InternalName"
	Name = "Second Tab"

# _someField
Position: $tab1Id:0
```

Note: Tab entries use `TabGroupAttribute+TabSubGroupAttribute` (the `+` indicates a nested C# class), NOT `TabGroupAttribute` directly. TabSubGroupAttribute supports `Icon` and `TextColor` parameters.

**Generate a group ID:**
```bash
python3 -c "import random, string; print('$' + ''.join(random.choices(string.ascii_letters + string.digits, k=22)))"
```

**Multiple attributes on one member** -- list them consecutively:
```
# _health
+ [Sirenix.OdinInspector.ReadOnlyAttribute]
+ [Sirenix.OdinInspector.ProgressBarAttribute]
	Min = 0
	Max = 100
```

### Blank Lines

Member entries are separated by blank lines. Within a single member entry, there are no blank lines between Position, attributes, and parameters.

## Complete Example

Given this C# script:

```csharp
namespace Samples
{
    public class SampleBehaviour : MonoBehaviour
    {
        [SerializeField] private int _sampleNumber;
        [SerializeField] private string _sampleString;

        public void IncreaseNumber() { _sampleNumber++; }
        public void ChangeString(string newString) { _sampleString = newString; }
        public void RandomString() { _sampleString = Guid.NewGuid().ToString(); }
    }
}
```

The OVDF file `Samples_SampleBehaviour.ovdf`:

```
OVDF v1.1
Samples.SampleBehaviour, Assembly-CSharp
MetaGuid:87c47d39c647426bb35c710dd217bfc7

# _sampleString
Position: $root:0

# IncreaseNumber()
+ [Sirenix.OdinInspector.ButtonAttribute]

# ChangeString(string)
+ [Sirenix.OdinInspector.ButtonAttribute]
	Name = "Set Sample String"
```
