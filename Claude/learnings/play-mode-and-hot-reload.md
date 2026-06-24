# Play Mode Options & Hot Reload

> **Decision:** the "Domain Reload off + reset static state explicitly" trade-off is recorded in `Claude/docs/adr/0003-play-mode-domain-reload-off.md`. This file is the how-it-works.

## Current Project Settings
**Enter Play Mode Options** — both flags **disabled**:
- Reload Domain: **OFF**
- Reload Scene: **OFF**

This means static state, static event subscriptions, cached `UnityEngine.Object` references, and the previous scene's `GameObject`s **persist across Play sessions**. Any new static state added to the codebase must be explicitly reset on Play, or it will leak between runs.

The project also uses the **Hot Reload** asset for in-Play code edits without exiting Play Mode.

## The Reset Pattern (mandatory for new static mutable state)
Mirror `EventBus`'s approach: a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method that nulls/clears every mutable static field. `SubsystemRegistration` fires before `Awake` and before any scene `OnEnable`, so subscribers can never observe stale state.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetOnPlayModeEnter()
{
    _cachedThing = null;
    _someList.Clear();
}
```

### Existing resets in the codebase (audit before adding new static state)
- `EventBus.ResetOnPlayModeEnter` (`Infrastructure/EventBus/EventBus.cs:17`) — clears `_depth` + every `Listeners<T>.Sorted/Pending` via the `Clearers` list. Each generic `Listeners<T>` self-registers in its static ctor.
- `CoreHelper.ResetOnPlayModeEnter` (`Infrastructure/Utilities/CoreHelper.cs`) — nulls `_mainCamera` cache, clears `_waitDictionary`.
- `GizmoManager.ResetOnPlayModeEnter` (`Gameplay/_MonoWorld/Settings/GizmoSystemHandler.cs`) — nulls `_handler` so the next Play re-finds/creates the editor GameObject.

### Static state that does NOT need a reset
- **Immutable computed values**: `BoundaryConstraints._groundFilter`, `RuntimeConstants.SceneIndex.*`, `Log.Default/Loading/...` — value never changes, no risk.
- **Stateless wrappers**: `AssetService.R` — just a `new Resources()` whose methods are stateless `Resources.Load` calls.
- **Instance services injected via Reflex**: `CommandDispatcher`, `LoadingService`, etc. — the Reflex container is rebuilt every Play, so injected instances are fresh.

### Things that look risky but aren't
- `SceneWorkflowRunner` uses `DontDestroyOnLoad` and a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` spawner. Unity destroys *all* Play-mode-created GameObjects on stop regardless of `DontDestroyOnLoad`, so no leak between Play sessions.

## Hot Reload — What It Can and Cannot Do

### Patches cleanly (use freely)
- Method body edits in MonoBehaviours / regular C# classes
- Expression / statement-level changes
- New local variables / locals reordering
- Logging / `Debug.Log` insertion

### Requires a full Play restart (Hot Reload will silently skip or break)
- **Adding/removing fields, methods, properties, attributes** on any type
- **Generic type parameter changes**, base class / interface list changes
- **`ScriptableObject` schema changes** — runtime field layout mismatch
- **Burst-compiled code** (most files under `Gameplay/!_Scripts/Systems/`) — Burst code is AOT-compiled; Hot Reload swaps IL, Burst still runs old version
- **ECS source generators** — anything under `Gameplay/!_Scripts/Components/` (`IComponentData`, `IAspect`, `Authoring`/`Baker`, `[BakingType]`) regenerates code at compile time; the running World has the old generated code
- **Reflex bindings** — `ProjectInstaller.InstallBindings` is called once when the container is built; binding changes won't be picked up by an existing container
- **Static field initializers / static ctors** — they ran once; new defaults won't apply

### Rough rule of thumb
- Editing inside `Gameplay/_MonoWorld/**` or `Infrastructure/**` method bodies → Hot Reload is fine.
- Editing under `Gameplay/!_Scripts/Components/` or `/Systems/` → restart Play.
- Editing `ProjectInstaller` / any `*Installer` → restart Play.
- Adding/removing fields anywhere → restart Play.

## Symptoms That a Reset Is Missing
- A subscription "ghost-firing" twice on second Play — listener list wasn't cleared.
- `MissingReferenceException` on a cached Unity object reference at frame 0 — `Object` was destroyed when Play stopped but the C# reference survives.
- A static singleton `Instance` returning the previous run's now-disabled object.
- A counter / timer starting at a non-zero value on Play.

When you see any of these, grep for `static` in the relevant file and add the reset.

## Authoring Guidance for Future Code
- **Default to instance state on a Reflex-injected service**, not static fields. The container takes care of lifecycle.
- If static is genuinely required (allocation-free generic dispatch like `EventBus.Listeners<T>`, cheap utility caches like `CoreHelper`), **always** add a `SubsystemRegistration` reset in the same file. Don't centralize the resets — they live next to the state they reset.
- For caches of `UnityEngine.Object` references (Camera, Material, GameObject), null them on reset. The fake-null check (`_x == null`) catches the "Unity destroyed it" case but **not** the "still alive but wrong scene" case, which becomes possible with Reload Scene off.
