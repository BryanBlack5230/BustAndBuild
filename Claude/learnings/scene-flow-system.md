# Scene Flow System

## SceneWorkflowRunner Is Stripped From Builds
**Context:** Analyzing what happens when the game is built while `SceneWorkflowRunner` is wrapped in `#if UNITY_EDITOR`.  
**Finding:** The entire class is stripped — `[RuntimeInitializeOnLoadMethod]` never fires, no scene sequencing happens. The game launches to whatever scene is first in Build Settings and stays there. `ISceneFlow` implementations still compile and initialize themselves fine, but `WaitForInit()` is never consumed.  
**Why it matters:** `SceneWorkflowRunner` is a dev-only tool by original design; builds need an explicit runtime path.

## RunConfiguration Assets Are in Resources — Loadable at Runtime
**Context:** Needed to find a RunConfiguration at runtime without `AssetDatabase`.  
**Finding:** All RunConfiguration assets live in `Assets/!_Game/Resources/SceneRunConfigurations/`. Use `Resources.LoadAll<RunConfiguration>("SceneRunConfigurations")` to get all of them at runtime.  
**Why it matters:** No wrapper SO needed — the assets are already Resources-accessible.

## #if UNITY_EDITOR / #else / #endif for Editor vs Build Split in Runtime Class
**Context:** `SceneWorkflowRunner` needed to preserve existing editor behavior while adding a build-only path.  
**Finding:** Use `#if / #else / #endif` (not just `#if`) to make the two paths mutually exclusive. The `#else` block only compiles into builds; the `#if` block only compiles in editor. The class stays in the runtime assembly (not Editor folder).
```csharp
#if UNITY_EDITOR
    // SessionState path — existing editor tool behavior, unchanged
    if (string.IsNullOrEmpty(path)) return;
    ...
#else
    // Build path — Resources scan
    var configs = Resources.LoadAll<RunConfiguration>("SceneRunConfigurations");
    ...
#endif
```
**Why it matters:** Cleaner than a fallthrough — editor play mode is completely isolated from the build path.

## UnityEditor APIs vs Editor-Assembly Classes Behind #if UNITY_EDITOR
**Context:** Considered referencing `SceneWorkflowHandoff` (Editor folder) from `SceneWorkflowRunner` (runtime assembly) behind a `#if UNITY_EDITOR` guard.  
**Finding:** `UnityEditor.*` APIs (SessionState, AssetDatabase) CAN be used from runtime assembly scripts behind `#if UNITY_EDITOR` — they compile fine. But classes defined in the Editor folder belong to the editor assembly and CANNOT be referenced from runtime assembly scripts at all, even behind the guard. The preprocessor strips the code, but the assembly reference doesn't exist.  
**Why it matters:** Inline editor-API calls directly in the runtime class rather than delegating to an Editor-folder helper.
