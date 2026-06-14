# UGFW Harvest — Ported Utilities & Design Ideas

In June 2026 we evaluated UGFW (Unity Game Framework, MIT © 2026 MAK, github.com/invertibleMatrix/unity-game-framework) — a mobile-F2P-oriented framework briefly dropped into `Assets/!_Game/UGFW/` and **deleted after harvesting**. It could not be adopted wholesale (hard deps: DOTween, Addressables, Cinemachine 3, Reflex 14 — we use PrimeTween, Resources/AssetService, Cinemachine 2.10, Reflex 13). This file records what we took and the design ideas worth reusing. The source folder no longer exists — everything needed to rebuild an idea is written down here.

## What we ported (code, adapted)

| File | Where | Adaptation notes |
|---|---|---|
| `Timer` | `Infrastructure/Utilities/Timer/` | Ported earlier; UGFW original had a timeScale double-scaling bug (multiplied tick by `timeScale` on top of UniTask's already-scaled delay) and `Pause/Resume` only worked in countdown mode — fixed in our version |
| `TimeFormatter` | `Infrastructure/Utilities/TimeFormatter.cs` | Duration ("1h 30m" / "01:30:45"), relative ("5m ago"), arrival ("Tomorrow at 5pm"), parse-back. `TimeRounding.Ceil` exists specifically for cooldowns — never show "0m" while one is active. Added a `TimeSpan` overload. Post-port audit fixes (see below): invariant-culture parsing, Stopwatch skips whole-second rounding (ms were always 00), `ToRelativeTime` compares in UTC, `ParseTime` throws + `TryParseTime` added |
| `NumberFormatter` | `Infrastructure/Utilities/NumberFormatter.cs` | Merged UGFW's two redundant APIs into one `FormatAbbreviated` family. Key semantic kept: **owned resources floor, costs ceil** (`roundDown` param, default floor). Dropped two-letter idle-game suffixes (long can't exceed "Q") and the custom exception class. Post-port audit fix: floor/ceil applies to the **signed** value (floor-of-abs rounded negatives toward zero, overstating owned negative balances); ceil renormalizes at unit boundaries (999999 cost → "1M", not "1000K") |
| `PriorityQueue<TElement,TPriority>` | `Infrastructure/Utilities/DataStructures/PriorityQueue.cs` | Verbatim .NET quaternary-min-heap port (absent from Unity's .NET Standard 2.1). Local change: merged the BCL throw-helper polyfill classes into `ThrowHelper` because `internal static class ArgumentNullException` **shadowed `System.ArgumentNullException` for every file in the namespace**. Intended consumer: island pathfinding (A*/Dijkstra, sea spawn → beacon) |
| `MissingScriptsFinder` | `Editor/MissingScriptsFinder.cs` | Menu `BarkingBird/Missing Scripts/...`. Fixed: original's remove pass didn't recurse into children (find did). Post-port audit fix: prefab assets selected in the Project window now go through `LoadPrefabContents` → `SaveAsPrefabAsset` (in-place removal silently didn't persist — see [[unity-csharp-patterns]]) |

## Post-port audit (2026-06-13) — bug classes hiding in harvested code

**Context:** A standards review of the UGFW/Vortex ports found three shipping-grade bugs that demos never surface. The ports compiled and looked correct; every bug lived at a locale, numeric-range, or lifecycle edge.
**Finding — the checklist for any future harvest:**
1. **Culture-sensitive parsing**: every `double.Parse`/`TryParse` of game data needs `CultureInfo.InvariantCulture` — on comma-decimal locales (de-DE, ru-RU) `"1.5"` parses as **15**, so `"1.5h"` became 15 hours.
2. **Round-then-format ordering**: TimeFormatter floored total seconds before formatting, so the Stopwatch format's millisecond component was always "00".
3. **UTC/local mixing across utilities**: `ToRelativeTime` used `DateTime.Now` while `DateTimeTimer` anchors to `UtcNow` — combining them was silently off by the timezone.
4. **Display-rounding sign handling**: floor-of-absolute-value rounds negatives toward zero — "never overstate what the player owns" requires flooring the *signed* value.
5. **Silent-zero parse APIs**: `ParseTime` returned 0 on garbage; converted to throw + `TryParse` pair per studio rule 4.

**Why it matters:** harvested code must be reviewed at the same depth as new code — MIT pedigree and "it works in the demo" prove nothing about locale/range edges.

## What we deliberately skipped
- **GenericEventBus** — our static `EventBus` was already built *instead of* it (see [[events-and-services]]); ours is allocation-free, theirs isn't. Its consume/stop-propagation feature remains the one thing we lack, deliberately.
- **AppStateMachine** (SO-based app states with pause/push stack) — overlaps `GameLoopManager` + SceneWorkflow. Its one good trick: `ChangeState(state, pauseCurrent: true)` pushes and pauses, `TryGoBack()` pops and resumes — a navigation stack for coarse app states. Reconsider only if pause-menu flow outgrows GameLoop.
- **JobDispatcher** (double-buffered main/worker thread jobs) — we have ECS/Burst.
- **Services layer** (Ads/IAP/Analytics/Firebase/RemoteConfig/Notifications) and F2P metas (DailyRewards/SpinWheel/Gacha/Seasons) — wrong genre. RemoteVariable's three-tier resolution (Remote > Cached > Default) is a nice pattern if live-tuning ever matters.
- **CameraSystem, ResourceManagement, Shims, FreeList** — no consumer; FreeList's generational-handle idea (Handle = index + generation, stale handles fail assert) is worth remembering for stable references into pooled arrays.

## Idea 1: UI stack architecture (for the UI/menus milestone)
The best part of UGFW. One `UIView` class serves screens and fragments; a `UIViewChannel` *component on the prefab* (not a class hierarchy) decides which:
- **Screens** get their own Canvas and are pushed onto **channel stacks** sorted by enum value: `UIChannel { HUD = 0, Menu = 100, Overlay = 200 }`. Gaps left for insertion. HUD = persistent gameplay UI, Menu = navigation-following full screens/popups, Overlay = transient top-priority (toasts).
- **Fragments** (no channel component) live in a parent view's container with **per-parent history stacks** → `GoBack(parent)` navigation for free.

Each view declares a **`ViewStackBehaviour`** — what happens to the view below when this one is pushed:
| Value | Below view | Use for |
|---|---|---|
| `DoNothing` | untouched | toasts, overlays |
| `HideBelow` | hidden, re-shown on close | full-screen takeover |
| `PauseOnlyBelow` | visible, input blocked | dialogs dimming the background |
| `PauseAndHideBelow` | hidden + input blocked | replacement screens |
| `CloseBelow` | destroyed | no-return navigation |

Lifecycle contract (hooks in call order): `SetContext(ctx)` → `RegisterResources` (subscribe) → `OnPrepareShow` (before anim) → `OnShow` (after anim) → [`OnPause`/`OnResume` when covered/uncovered] → `OnPrepareHide` → `OnHide` → `UnRegisterResources` → `OnReset` (returned to pool). **Static** views (pre-placed in scene) survive close (just hidden); **dynamic** views (instantiated) are pooled or destroyed.

Two implementation details worth copying: a **per-parent pending-show `UniTask`** serializes concurrent Show() calls on the same parent so two animations never fight over one CanvasGroup; and a `_closingViews` HashSet guards double-close. They also had a **View Stack Visualizer** editor window (live channel/fragment stack inspection + consistency validation) — build one when we have stacks to debug.

**When we build §9 UI (main menu / pause / HUD / building placement), design to this shape.** Show/close API: `Show<T>()` fire-and-forget + `ShowAsync<T>(context)` awaitable; animations were strategy SOs assigned per view (we'd use PrimeTween).

## Idea 2: Pending-transaction pattern (offline progression & quit-during-battle)
UGFW's `GameModel` never grants rewards directly across sessions. Instead:
1. Event happens → append `Transaction { UID, timestamp }` to a persisted pending list, save immediately.
2. Next `Initialize()` (boot): resolve each transaction's UID against the registry, **credit it, remove it, commit**. Orphans (asset deleted, UID unresolvable) are logged and dropped — never crash.
3. Their `Transaction` also persisted the **asset name as fallback key** next to the GUID, so a regenerated GUID could still resolve by name with a warning.

**Why it matters for us (§8):** "on-quit-during-battle = beacon destroyed on next launch" and offline progression are exactly this shape — persist *what happened / what is owed*, resolve and apply at boot when all systems exist. Our `WorldSaveService` can grow a pending-list field in `WorldSaveData` without any UGFW code. Combine with [[currency-and-saves]] save timing.

## Idea 3: Registry-keyed pooled spawners (audio & particles, §10/§11)
Pattern: `ConfigBase` SO (prefab ref + playback params — fade in/out, pitch randomization for audio) → a **Registry SO** holding all configs, `BuildCache()` once at startup into dictionaries with **loud duplicate-key errors** → a spawner MonoBehaviour with one pool per prefab type.

Two-tier API worth keeping:
- `PlayAudio(id)` — loose, "just play it", covers 99% of call sites.
- `Spawn<T>(id)` — strict typed spawn that *errors* on config/prefab type mismatch, for when the caller needs the component handle.

Return-to-pool is baked into init: `component.Init(config, returnToPool: () => ...)` — the spawned thing returns *itself*, no manager polling. Theirs was keyed by UID assets + Odin; ours should key by enum (like `CurrencyType` keys the pickup pipeline) and reuse our pooling. Rebuild small when we hit the audio/VFX milestones.

## Idea 4: MetaData hub — GUID-identity definition SOs (for §1 data schemas)
UGFW's content backbone, relevant when we author **building/monster/ally/resource definition catalogs**:
- Identity chain: `UID` (SO with auto-generated GUID string, **value-equality by GUID** — `==` works across instances, compares to `string`/`Guid` too, `OnValidate` self-assigns) → `MetaDataAsset : UID` (adds `Name, DisplayName, Description, Icon`) → game-specific `Definition` (e.g. `CurrencyDefinition` adds `Type, MaxAmount, StartingAmount`).
- Per domain, a fixed four-part shape: `[Domain]Meta` (SO container) → `[Domain]Registry` (`TypedUIDRegistry<Definition>`, bidirectional UID↔object lookup) → `[Domain]Definition` → `[Domain]Type` enum (coarse categorization only — identity stays the GUID).
- One `MetaDataRepository` referencing all metas lives in the bootstrap scene, registered in DI, injected everywhere. **Structurally identical to our agreed ConfigTask hub** (promoted `PrototypeConfigSetter` + `[InlineEditor]`) — independent validation of that design.

**Design tension to resolve when authoring §1 schemas:** our save indexes currencies by append-only enum (fine for 6 currencies, see [[currency-and-saves]]). For a building/monster catalog that will grow and get renamed for years, GUID-keyed definition SOs are more save-proof than enum indices — saves store the GUID string, renames/reorders are free, deletions degrade to a logged orphan instead of corrupting offsets. Decide per-catalog; don't cargo-cult either way. **Note:** Vortex's DatabaseSystem reached the identical convention independently — see [[vortex-framework-notes]] "DatabaseSystem conventions" (stable GUID per def asset, explicit-button-only regeneration, validated reference drawers). Two frameworks converging on this is strong evidence it's the right call for the def catalogs.

## Idea 5: UniPrefs / PrefsProperty (cheap real persistence)
- `UniPrefs`: static wrapper making PlayerPrefs hold any `[Serializable]` type — `JsonUtility.ToJson(new DataWrapper<T>(data))` (the wrapper struct exists because JsonUtility can't serialize top-level primitives/lists). Plus key-set tracking and an `OnReset` event on `DeleteAll`.
- `PrefsProperty<T>`: instance wrapper with a save key + default; `Read()` lazy-loads once then serves cache, `Save(v)` writes through, `Reset()` deletes + reverts, implicit conversion to `T`. UGFW persisted its whole `GameModel` as ONE `PrefsProperty<GameModel>` — `Commit()` = serialize everything.
- Flaws if rebuilding: it called `PlayerPrefs.Save()` on every `Set` (disk flush per write) and unsubscribed `OnReset` in a finalizer (unreliable).

**Use for us:** a ~25-line `PlayerPrefsSaveSystem : ISaveSystem` (key = slot id, value = JSON `WorldSaveData`) would make currency survive app restarts *today*, as a stopgap before the planned atomic-file save system ([[currency-and-saves]] — real file I/O deliberately deferred). The whole-model-as-one-key approach is also the simplest shape for the eventual file version: one JSON document per world slot.
