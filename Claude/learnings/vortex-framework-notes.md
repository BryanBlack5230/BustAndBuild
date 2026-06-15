# Vortex Framework — Harvested Ideas & Port Notes

Source: `Assets/!_Game/Vortex package/` (proprietary Unity framework, evaluated 2026-06-12).
The package itself is NOT part of the project — its bus/singleton architecture conflicts with our Reflex DI + EventBus + ECS stack. It currently compiles inside `Assets/` and should be moved out / excluded once cherry-picking is finished. Everything below is written to survive the package's deletion.

## What was ported (2026-06-12)

| Vortex source | Our location | Adaptations |
|---|---|---|
| `Unity/FormulaEvaluatorSystem/` | `Infrastructure/Formulas/` (+ `Editor/Formulas/` drawer) | Odin drawer → plain `PropertyDrawer` (project code never uses Sirenix); sibling-path + propertyPath-walk so `[Formula]` works in nested serializable types; `FormulaSlot` fields PascalCase; `#nullable`, English docs |
| `Core/System/Abstractions/Timers/DateTimeTimer.cs` | `Infrastructure/Utilities/DateTimeTimer.cs` | `GetTimeLeft()` actually returned *elapsed* time — renamed to `Elapsed`; `GetTimeRemains()` → `Remaining`; methods → properties |
| `Unity/UI/PoolSystem/Pool(+Item).cs` | `Infrastructure/Pooling/UiPool(+Item).cs` | Dropped Vortex `TimeController`/`IDataStorage`/Odin deps and LINQ; **dropped the `params object[]` key API** (array-instance identity was a removal footgun) — key is a single data object now; missing prefab throws instead of logging. Lifecycle hardened 2026-06-13: teardown-safe `OnDestroy`, no-`SetActive` OnDisable return path, fake-null skip in the free queue (general pattern: [[unity-csharp-patterns]]) |

`[Formula]` usage: `[Formula(nameof(_slots))] [SerializeField] string _wealthFormula; [SerializeField] FormulaSlot[] _slots;` — slots bind `{i}` tokens to numeric members of the declaring object; runtime eval via `FormulaEvaluator.Evaluate(formula, slots, owner)` / `TryEvaluate` (reflection, cached accessors — config-build time, not per-frame). Parser supports `+ - * / ^`, parens, `pi`/`e`, sqrt/abs/sin/cos/tan/log/floor/ceil/round/min/max/pow/clamp. Unary minus binds tighter than `^` (`-2^2` = 4, Excel-style — documented in the class doc). Target use: Wealth/Danger formulas, economy curves (TaskBreakdown §12).

**Parser redesign (2026-06-13):** `{i}` parameters are **parser atoms** (`ParserState` carries the `double[]`), not string substitution. The original substitution used `ToString("R")`, which emits scientific notation outside ~[1e-4, 1e15) ("1E-05", "1E+15") that the parser can't read — evaluation failed exactly at idle-economy magnitudes (our `NumberFormatter` goes to "Q" = 1e18). Atom parsing also kills the per-eval string allocations, makes negative values work without parenthesization, and yields real errors ("No value bound to parameter {3}" instead of "Unexpected character '{'"). If a formula-like DSL is ever extended: never substitute numbers into source text.

## Ideas worth rebuilding later (do NOT copy the code — it drags the whole Vortex core)

### AudioSystem channel model (for TaskBreakdown §10)
`Core/AudioSystem` + `Unity/AudioSystem`. The good idea: **named audio channels** as first-class objects — each channel has persisted volume/mute in settings; `MusicPlayer`/`AudioPlayer` MonoBehaviours hold a channel reference and re-apply `settingsVolume × clipVolume × runtimeMultiplier` whenever a global `OnSettingsChanged` fires; drop-in handler components (`AudioChannelVolumeSlider`, `AudioSwitcher`) bind UI controls to a channel by name. Rebuild as a Reflex-installed service + EventBus notification when audio work starts; don't port (welded to their bus/driver stack).

### TimeController queued-action dispatcher
`Unity/AppSystem/System/TimeSystem/TimeController.cs`. Central MonoBehaviour dispatcher holding a timestamp-ordered queue of `QueuedAction { Owner, Action, Timestamp }`; consumers schedule "call me at T", owners can bulk-cancel via `RemoveCall(owner)`. Relevant pattern for auto-save every 10 min, beacon 5-min auto-repair, wave timers. Rebuild as a game-loop-aware service (`IGameUpdateListener`, pause-aware) — their version ticks in raw `Update` and ignores pause. Pair with our `DateTimeTimer` for the offline/calendar cases.

### ExtensibleEnum
`Core/ExtensibleEnumSystem`. Enum-like classes where each value is a `static readonly` instance of the subclass; the base-class constructor self-registers into a per-type registry; eager init via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `[InitializeOnLoadMethod]` so `GetAll<T>()`/deserialization/inspector dropdowns work regardless of type-load order. Use case: designer-extensible def categories (resource types, building categories) without recompiling switch statements. Their version drags `SerializeController`; if we ever need it, re-derive the pattern (~100 lines). For ECS-side data, plain enums/blobs stay preferable.

## Architecture discipline worth keeping (from Vortex README)

- **Preset → Model**: defs are immutable presets (SO, `get;` only); runtime state lives in mutable model instances created *from* presets (`CopyFrom`), never in the asset itself. Distinguish **singleton** records (one shared instance, persisted) from **multi-instance** records (fresh copy per request, not persisted). Maps to us: SO config → baked blob/runtime instance; never mutate the SO at runtime. Already aligned with the ConfigHub ([[config-system]]); keep it that way when adding building/monster defs.
- **Call accumulation / notify-once**: batch all field changes, then fire one explicit `NotifyChanged()` — never an event per setter (prevents recursive correction cascades and N redundant UI refreshes per frame). Corollaries: data-modification logic lives only in the owning controller; UI never decides, it only reports input and re-reads. Matches our ECS batching; apply to Wallet/world-state Mono services too.

## DatabaseSystem conventions (code not portable; conventions are)

`Core/DatabaseSystem` + `Unity/DatabaseSystem` — GUID-keyed registry of def presets. Worth adopting when defs + saves arrive:

1. **Stable GUID identity per def asset.** Serialized `guid` generated once at asset creation; regeneration only via an explicit editor button, never silently. Saves and def-to-def references store the GUID — renames/moves can't corrupt worlds. (Our per-world saves must reference building/monster/resource defs this way.)
2. **Validated reference fields.** `[DbRecord(typeof(MonsterDef))]` on a string field draws a type-filtered dropdown of registered defs + link validation (`Database.TestRecord(guid)` at edit time) instead of raw ID strings. Reproduce with a small custom drawer over our config registry.
3. **Live runtime model in the preset inspector.** Editor-only field on the preset shows the runtime instance it spawned, in a "Debug" tab during play — click the def asset, see live state.
4. **Asset name auto-synced to record name** (rename asset on name-field change, collision-numbered) — keeps project window searchable; minor but cheap.
