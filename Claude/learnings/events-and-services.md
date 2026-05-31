# Events, Services, Utilities

## EventManager — Static Event Hub
`BarkingBird.Runtime.Infrastructure.EventManager` (at `Runtime/Infrastructure/EventManager.cs`) exposes static `Action`s:
- `OnEnemyDied(float3 pos)` — gameplay event for FX/score.
- `Input.ObjectGrabbed`, `Input.GroundGrabbed(bool actuallyHolding)`, `Input.Release`, `Input.SceneChangeRequest(bool isDown)`.

These are **not Reflex-injected** — anyone can subscribe by importing the namespace. `CursorSetter`, `BattleCameraMovement`, `WorldCameraHandler` all hook directly. Pattern: subscribe in ctor or `Register()`, unsubscribe in `Dispose()`.

`EnemyDiedEvent : IBufferElementData` is the ECS-side analogue (buffer on coordinator entity), but no producer in current code uses it — the Action is the active path.

## Log Tags
`Log.Default`, `Log.Loading`, `Log.Battle`, `Log.World`, `Log.City`, `Log.Boot` — each a `TagLog` instance with category prefix. Methods: `D` (debug, stripped under `PROD` define via `[Conditional("DUMMY_UNUSED_DEFINE")]` trick), `W` (warning), `E` (error/exception), `ThrowException`. All marked `[HideInCallstack]` so the stack frame skips the TagLog layer.

**Pattern:** prefer `Log.Battle.D("...")` over raw `Debug.Log`. Compile-out semantics for production come for free.

## LoadingService
Wraps `ILoadUnit.Load()` and `ILoadUnit<T>.Load(param)` with stopwatch timing + main-thread switch + exception logging. Has a `CompositeDisposable Disposable` field for collecting `IDisposableLoadUnit`. Used by `BootstrapFlow` to load `ConfigContainer` (JSON from `Runtime/Gameplay/Resources/Settings/Config.json`) and `CursorSetter` (cursor textures).

## AssetService — Required Wrapper For All Resources Loads
**Convention:** every `Resources.Load*` call in the project goes through `AssetService.R.Load<T>(path)` / `AssetService.R.LoadAll<T>(path)`. Direct `UnityEngine.Resources.Load*` calls are only allowed inside `AssetService.cs` itself.
**Why it matters:** Centralizes the Resources entry point so future caching/profiling/Addressables migration touches one file. New `LoadAll<T>` was added when `SceneWorkflowRunner` was routed through it — extend `AssetService.Resources` if you need another Resources API.

**Naming-collision gotcha:** the wrapper class is `BarkingBird.Runtime.Infrastructure.Utilities.Resources` (intentionally shadows `UnityEngine.Resources`). Files that need both must alias one (`using Resources = UnityEngine.Resources;`) or fully qualify — but consumer code should never need `UnityEngine.Resources` at all once it uses `AssetService.R`.

## CoreHelper
- `CoreHelper.MainCamera` — cached `Camera.main`. **Camera must be tagged MainCamera.** Cache lazily; reset when nulled.
- `CoreHelper.GetWait(float)` — cached `WaitForSeconds` dictionary to avoid allocation.
- `CoreHelper.TimeNow()` — `"HH:mm:ss.fff"` formatted timestamp.

## MathHelper
- `GetHeading(objPos, targetPos)` — atan2-based heading in radians.
- `GetForwardFromHeading(float heading)` — `(sin, 0, cos)` unit vector.

## PhysicsUtility
- `GetRandomPointInsideCollider(em, entity, ref Random)` — samples a random point inside an AABB of a `PhysicsCollider` (transformed to world). Used by `SpawningSystem` area spawner.

## Config Pipeline
1. `BarkingBird/Generate Configs` (editor menu) — `ConfigGenerator.Generate()` writes a hardcoded `ConfigContainer` to `Assets/!_Game/Runtime/Gameplay/Resources/Settings/Config.json` via Newtonsoft.
2. At runtime, `ConfigContainer : ILoadUnit` is registered as singleton; `BootstrapFlow` calls `LoadingService.BeginLoading(_configContainer)` → `Resources.Load<TextAsset>("Config")` → `JsonConvert.PopulateObject`.
3. `BlobContainer.Initialize()` (called after configs loaded) converts `TargetProfile` lists into a `BlobAssetReference<TargetProfilesBlob>` stored on a singleton entity named `Global_Target_Profiles`.
4. Currently `BlobContainer` reads from `PrototypeConfigSetter` (MonoBehaviour) not `ConfigContainer.Battle.EnemyProfiles` — that path is commented out. **Don't be surprised** if config-JSON edits don't change AI behaviour; edit the Bootstrap scene's `PrototypeConfigSetter` instead.

## BlobConfigConverter (Reflection)
`BlobConfigConverter.CreateBlob<T>(source)` walks public instance fields by name match and copies values into a blob root struct via `__makeref` / `SetValueDirect`. Skips fields whose types differ. Used as generic converter for `[BlobConfig]`-annotated config classes. Currently not invoked anywhere — `BlobContainer` builds its profile blobs manually. Keep in mind if you see `[BlobConfig]` on a class but no allocation site.

## RuntimeConstants
At `Runtime/Infrastructure/Settings/RuntimeConstants.cs`, namespace `BarkingBird.Runtime.Infrastructure.Settings`. **All Resources paths and Assets-relative paths must live here** — no string literals at call sites.
- `Scenes.Bootstrap/Loading/World/Battle/City` — int build indices, resolved at static init via `SceneUtility.GetBuildIndexByScenePath`.
- `PhysicLayers.Unit/Grabbable/Ground/Obstacle` — string names; resolved via `LayerMask.NameToLayer` at use sites.
- `Configs.ConfigFileName = "Settings/Config"` — Resources-relative path consumed by `ConfigContainer.Load()`.
- `Configs.AssetsResourcesFolder = "!_Game/Runtime/Gameplay/Resources"` — Assets-relative path for editor-side writes (`ConfigGenerator` uses it with `Application.dataPath`). Separate constant because `Path.Combine(Application.dataPath, …)` is not a Resources.Load.
- `Cursors.Open/ObjectHold/GroundHold/Dummy` and `Cursors.All` — texture filenames (just the basename).
- `Cursors.RootPath = "Cursors/"`, `Cursors.SpritesPath`, `Cursors.TexturesPath`, `Cursors.DummyPrefabPath` — composed from `RootPath`. Pattern: define the folder once, build subpaths via `const` concatenation, so a folder rename is a one-line change.
- `SceneWorkflow.RunConfigurationsPath = "Settings/SceneRunConfigurations"` — Resources-relative; consumed by `SceneWorkflowRunner` in build mode via `AssetService.R.LoadAll<RunConfiguration>`.

**Naming-collision gotcha:** `RuntimeConstants.SceneWorkflow` (nested class) shadows the `BarkingBird.Runtime.Infrastructure.SceneWorkflow` namespace inside that namespace's own files. Always go through `RuntimeConstants.SceneWorkflow.X` so the compiler routes via the type, not the namespace.
