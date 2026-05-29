# Events, Services, Utilities

## EventManager — Static Event Hub
`Game.Core.Events.EventManager` exposes static `Action`s:
- `OnEnemyDied(float3 pos)` — gameplay event for FX/score.
- `Input.ObjectGrabbed`, `Input.GroundGrabbed(bool actuallyHolding)`, `Input.Release`, `Input.SceneChangeRequest(bool isDown)`.

These are **not Reflex-injected** — anyone can subscribe by importing the namespace. `CursorSetter`, `BattleCameraMovement`, `WorldCameraHandler` all hook directly. Pattern: subscribe in ctor or `Register()`, unsubscribe in `Dispose()`.

`EnemyDiedEvent : IBufferElementData` is the ECS-side analogue (buffer on coordinator entity), but no producer in current code uses it — the Action is the active path.

## Log Tags
`Log.Default`, `Log.Loading`, `Log.Battle`, `Log.World`, `Log.City`, `Log.Boot` — each a `TagLog` instance with category prefix. Methods: `D` (debug, stripped under `PROD` define via `[Conditional("DUMMY_UNUSED_DEFINE")]` trick), `W` (warning), `E` (error/exception), `ThrowException`. All marked `[HideInCallstack]` so the stack frame skips the TagLog layer.

**Pattern:** prefer `Log.Battle.D("...")` over raw `Debug.Log`. Compile-out semantics for production come for free.

## LoadingService
Wraps `ILoadUnit.Load()` and `ILoadUnit<T>.Load(param)` with stopwatch timing + main-thread switch + exception logging. Has a `CompositeDisposable Disposable` field for collecting `IDisposableLoadUnit`. Used by `BootstrapFlow` to load `ConfigContainer` (JSON from `Resources/Config.json`) and `CursorSetter` (cursor textures).

## AssetService
Trivial wrapper over `UnityEngine.Resources`: `AssetService.R.Load<T>(string path)`. Mostly used in `ConfigContainer.Load()`. Not heavily used elsewhere.

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
1. `BarkingBird/Generate Configs` (editor menu) — `ConfigGenerator.Generate()` writes a hardcoded `ConfigContainer` to `Assets/!_Game/Resources/Config.json` via Newtonsoft.
2. At runtime, `ConfigContainer : ILoadUnit` is registered as singleton; `BootstrapFlow` calls `LoadingService.BeginLoading(_configContainer)` → `Resources.Load<TextAsset>("Config")` → `JsonConvert.PopulateObject`.
3. `BlobContainer.Initialize()` (called after configs loaded) converts `TargetProfile` lists into a `BlobAssetReference<TargetProfilesBlob>` stored on a singleton entity named `Global_Target_Profiles`.
4. Currently `BlobContainer` reads from `PrototypeConfigSetter` (MonoBehaviour) not `ConfigContainer.Battle.EnemyProfiles` — that path is commented out. **Don't be surprised** if config-JSON edits don't change AI behaviour; edit the Bootstrap scene's `PrototypeConfigSetter` instead.

## BlobConfigConverter (Reflection)
`BlobConfigConverter.CreateBlob<T>(source)` walks public instance fields by name match and copies values into a blob root struct via `__makeref` / `SetValueDirect`. Skips fields whose types differ. Used as generic converter for `[BlobConfig]`-annotated config classes. Currently not invoked anywhere — `BlobContainer` builds its profile blobs manually. Keep in mind if you see `[BlobConfig]` on a class but no allocation site.

## RuntimeConstants
- `Scenes.Bootstrap/Loading/World/Battle/City` — int build indices, resolved at static init via `SceneUtility.GetBuildIndexByScenePath`.
- `PhysicLayers.Unit/Grabbable/Ground/Obstacle` — string names; resolved via `LayerMask.NameToLayer` at use sites.
- `Cursors.Open/ObjectHold/GroundHold/Dummy` and `Cursors.All` — texture filenames under `Resources/Cursors/Textures/`.
- `Configs.ConfigFileName = "Config"`.
