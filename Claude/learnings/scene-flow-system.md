# Scene Flow System

## Overview
Two parallel systems:
1. **`ISceneFlow` runtime contract** — every scene's `*Flow` MonoBehaviour exposes `WaitForInit()` so the runner can sequence loads.
2. **`RunConfiguration` / `SceneChain`** — designer-authored ScriptableObjects selecting which scenes run together, with `StateOverride[]` hooks to manipulate state on bootup.

## ISceneFlow Contract
```csharp
public interface ISceneFlow { UniTask WaitForInit(); }
```
Implementations (`BootstrapFlow`, `WorldFlow`, `BattleGroundSceneFlow`) hold a `UniTaskCompletionSource` and `TrySetResult()` at the end of `Start()`. `SceneWorkflowRunner.WaitForSceneInit(sceneName)` finds the flow via `GetComponentInChildren<ISceneFlow>(true)` and awaits it before loading the next scene.

## SceneWorkflowRunner — Editor vs Build Split
Uses `#if UNITY_EDITOR / #else / #endif` to make editor and build paths mutually exclusive:
- **Editor:** reads `SessionState` keys (`SceneWorkflow.RunConfigPath`, `IsSingleScene`, `SingleSceneTarget`). If unset → returns silently (default Unity play).
- **Build:** `Resources.LoadAll<RunConfiguration>("SceneRunConfigurations")` and finds the first `IsBuildConfig == true`.

The class lives in runtime assembly; `UnityEditor.*` API calls (SessionState, AssetDatabase) work fine when wrapped in `#if UNITY_EDITOR`. **But classes defined in Editor folder (like `SceneWorkflowHandoff`) cannot be referenced from runtime assembly even behind the guard** — the assembly reference doesn't exist. Inline editor API calls directly.

`SceneWorkflowRunner` spawns a `DontDestroyOnLoad` GameObject and self-destroys after applying all `StateOverride`s.

## RunConfiguration (ScriptableObject)
- `_chain` (SceneChain SO) — list of scenes to load in order.
- `_overrides` (List<StateOverride>, `[SerializeReference]`) — polymorphic list applied after scene chain is up.
- `_isDefault` flag — auto-picked by `SceneWorkflowHandoff.AutoResolveConfig(sceneName)` when no explicit selection. Scoped per chain.
- `_isBuildConfig` flag — globally exclusive; the one config used when running a build.

Both flags are `[ReadOnly]` + `[ShowIf("_flagName")]` so they only render when true (cleaner inspector). `Set Default` / `Set Build Config` buttons (Odin, editor-only) clear the flag on all sibling configs.

Resources directory: `Assets/!_Game/Runtime/Gameplay/Resources/Settings/SceneRunConfigurations/` — `CityDev`, `BattleDev`, `NormalDev`, `NormalProd`. (Sibling `Settings/SceneCollections/` holds `SceneChain` assets.)

## SceneChain (ScriptableObject)
List of `SceneChainElement` — each holds `_sceneAsset` (UnityEditor-only `SceneAsset` ref) and cached `SceneName` / `ScenePath` properties synced via `OnValidate` → `SyncFromAsset()`. Custom inspector (`SceneChainEditor`) adds "Open Chain" / per-element "Open additively"/"Open single" buttons.

## SceneWorkflow Toolbox (`Cmd+Shift+W`)
`SceneWorkflowToolbox : EditorWindow` (BarkingBird/Scenes/Scene Workflow). Lists all RunConfigurations as selection buttons, has "Single Scene Mode" toggle, shows inline editor preview of the selected config. Writes `SceneWorkflowHandoff.Set(...)` so the next Play tick activates the runner.

`SceneWorkflowPlayModeHandler` (`InitializeOnLoad` in Editor) hooks `EditorApplication.playModeStateChanged`:
- On `ExitingEditMode`, if the bootstrap (chain[0]) scene isn't the active scene, saves current scene setup, swaps to bootstrap, restarts Play. After Play exits → restores saved scenes.

## StateOverride (Polymorphic, [SerializeReference])
Abstract base with `UniTask Apply()`. Concrete:
- `GameLoopStateOverride` — finds `GameLoopManager` (MonoBehaviour) via `FindFirstObjectByType` and calls `StartGame/PauseGame/FinishGame`. Useful for "start in pause state" dev runs.
- `ActiveCameraOverride` — sends `ChangeSceneCommand(_switchUp)` via `CommandDispatcher` to flip between battle/world cams.
- `DayCycleStateOverride` — sends `StartDayCommand` or `ForceFinishDayCommand` based on its `TargetAction` enum.

To add a new override: subclass `StateOverride`, mark `[Serializable]`, implement `Apply()`. The Odin/Unity SerializeReference picker in `RunConfiguration` inspector exposes it.

**Targeting MonoBehaviours vs. DI-bound services:**
- Target is a scene MonoBehaviour → `FindFirstObjectByType<T>()` is fine (see `GameLoopStateOverride`).
- Target is a pure-C# Reflex singleton (e.g. `DayNightCycle`, `WorldCameraHandler`) → it can't be found via `FindObjectByType`. **Don't** dig the Reflex container out of the active scene. **Do** define a `readonly struct XCommand : ICommand`, resolve `CommandDispatcher` from `Reflex.Core.Container.ProjectContainer` inside `Apply()`, and `Send` the command. The service registers a handler via `dispatcher.Register<XCommand>(...)` in its constructor and stores the returned `IDisposable`. See `ActiveCameraOverride` / `DayCycleStateOverride` for the sender pattern and [[events-and-services]] for the notifications-vs-commands split.

## Open-In-Editor Toolbox
`ToolBox.cs` exposes Alt+1..5 shortcuts to open individual scenes single-mode (`BarkingBird/Scenes/Bootstrap &1` etc.). Uses `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` before swap.

## Common Pitfalls
- `SceneWorkflowRunner` is stripped from builds when no `IsBuildConfig` flag set. Game launches to whatever scene is first in Build Settings.
- Need a new scene to participate in the workflow? Add a `*Flow : MonoBehaviour, ISceneFlow` with a `UniTaskCompletionSource` and `_initCompleted.TrySetResult()` in `Start()`. Register its container parent via `SceneScope.OnSceneContainerBuilding`.
- Backwards iteration `for (int i = sceneCount-1; i >= 0; i--)` required when closing multiple scenes in editor — forward iteration shifts indices.
- `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` returns `false` on user Cancel — always check and abort.
- `EditorSceneManager.GetSceneByPath(path).IsValid()` returns true if scene is already loaded — guard `OpenScene(Additive)` calls with this or you'll load a second copy.
