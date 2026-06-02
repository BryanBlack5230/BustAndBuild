# Input System

## Action Maps
`InputActions.inputactions` → auto-generated `InputActions.cs`. Two maps:
- **Gameplay**: Interact (LMB), PowerHit (RMB), MousePosition, ScrollUp, ScrollDown.
- **UI**: Navigate/Submit/Cancel/Point/Click/etc. (Unity's UI Toolkit defaults).

`InputManager` (Project scope singleton) enables `UI` + `Gameplay.MousePosition` at construction; everything else is enabled by its controller (`InteractController`, `PowerHitController`, `ScrollController`) on `OnStartGame()`/`OnResume()`, disabled on `OnPause()`/`Dispose()`.

## Grab → Drag → Release Flow
1. **`InteractController.OnClick(LMB performed)`**: raycasts via `PhysicsWorld.CastRay` from `mouseRay.GetPoint(9f)` to `GetPoint(40f)` with filter `Grabbable | Ground`. Hits a `Grabbable` entity (has enableable `Grabbed` component) → invokes `EventManager.Input.ObjectGrabbed` and calls `_grabbingInteractor.Grab(entity)`. Hits ground → invokes `EventManager.Input.GroundGrabbed(false)` (start-of-hold).
2. **`GrabbingInteractor.Grab(entity)`**: stores `_originalMass`, sets `PhysicsMass.InverseMass = 0` (freezes), zeroes `PhysicsVelocity`, enables `Grabbed`, disables `InAir`, starts `GrabbedEntityMover` and `ThrowTrajectoryPredictor`.
3. **`GrabbedEntityMover.OnUpdate`**: each frame snaps entity to `mouseScreenPos → world plane at entityZ`, clamps to ground (`y >= groundY + halfHeight`), then re-clamps to camera viewport (4-corner check). Two-pass ground clamp because the viewport-clamp may have pushed Y back below ground.
4. **`InteractController.OnCanceled(LMB up)`**: invokes `EventManager.Input.Release` and calls `_grabbingInteractor.Release()`.
5. **`GrabbingInteractor.Release()`**: computes throw impulse from cursor velocity, calls `ReleaseCoordinator.HandleRelease(entity, impulse, isFastSpeed, originalMass)`, calls `_throwSettingsSetter.OnThrow` (for diagnostics).

## ReleaseCoordinator — Overlap Strategies
- `isFastSpeed = rawPower > ThrowThreshold` (default 100).
- **Slow speed + overlapping** → `OverlapResolver.ResolveAsync` (displacement push step, max 0.5s).
- **Fast speed + overlapping** → `TunnelTeleporter.Teleport` (computes AABB exit distance along throw dir, teleports past the obstruction). If destination still overlapping → fallback to displacement.
- **Not overlapping** → `ClampToViewportAndGround` then `RestorePhysicsWithImpulse`.

`_inflight` dictionary tracks per-entity `CancellationTokenSource`; releasing the same entity twice cancels the previous resolve task.

## OverlapResolver Details
- `_nonGroundFilter` = everything except ground. Ground filter is now owned by `BoundaryConstraints` (lazily initialized static).
- `CheckOverlap(entity)` → AABB overlap query with `_nonGroundFilter`, then XY-AABB overlap test (`PhysicsOverlapHelper.AabbsOverlapXY`, ignores Z because game plane is XY).
- `DisplaceStep` averages "away" direction from all overlapping bodies, falls back to perpendicular if vectors cancel (`(-y, x)` rotation). Moves at `DisplaceSpeed = 15f`.

## ThrowSettingsSetter (inspector-driven)
MonoBehaviour in Bootstrap scene, registered as `IGameUpdateListener`. Exposes inspector knobs: `ThrowScale`, `ThrowThreshold`, `MinMaxVelocity` (with `[MinMaxSlider]`), `_velocityPowerCurve`, `Gravity`. Writes a `ThrowVelocitySettings` singleton each frame (64 curve samples baked) and updates `PhysicsStep.Gravity`.
**Why it matters:** Any system needing velocity-power scaling should `TryGetSingleton<ThrowVelocitySettings>` and use its `MinVelocity / MaxVelocity / CurveSamples`. `ScreenBounceSystem` and `InAirCollisionSystem` both do this.

## Shared Boundary Helpers — BoundaryConstraints + EntityPhysicsHelper
**Context:** `ThrowTrajectoryPredictor`, `OverlapResolver`, and `GrabbedEntityMover` all had duplicated ground-raycast and AABB-read logic.  
**Finding:** Extracted to two `internal static` helpers. `BoundaryConstraints` lives in `Runtime/Gameplay/!_Scripts/_MonoWorld/Input/` (namespace `BarkingBird.Runtime.Gameplay.Input`, same as its consumers); `EntityPhysicsHelper` lives under `Runtime/Infrastructure/Utilities/` (`BarkingBird.Runtime.Infrastructure.Utilities`), matching `PhysicsOverlapHelper` precedent:
- `BoundaryConstraints.GetGroundY(float3, in PhysicsWorldSingleton)` — raycasts ±200 relative to position; ground `CollisionFilter` is lazily initialized via `CollisionFilter? _groundFilter ??=` (safe after Unity scene load).
- `BoundaryConstraints.ClampToViewport(float3 pos, quaternion rot, float2 halfExtents, Camera cam, bool clampBottom)` — returns clamped `float3`; `clampBottom: false` for `GrabbedEntityMover` (drag-to-floor is valid), `clampBottom: true` for `OverlapResolver` (release clamp is absolute).
- `EntityPhysicsHelper.GetEntityHalfExtentsXY(Entity, EntityManager)` — one AABB read returning `float2(hw, hh)`, fallback `(0.5, 0.5)`.  
**Why it matters:** The ray-distance was inconsistent before (±500 / ±100 depending on file). Always use `BoundaryConstraints` — don't write inline ground raycasts again.

## ThrowTrajectoryPredictor — Screen Boundary Checks Must Use Entity Edges
**Context:** Implementing a trajectory arc predictor that mirrors `ScreenBounceSystem` behavior.  
**Finding:** All frustum boundary checks must use entity **edges**, not center. `ScreenBounceSystem` checks the four entity corners; using center makes wall bounces trigger too late (entity slides offscreen) and ceiling bounces trigger too early.  
**Why it matters:** Every time you replicate or extend screen-boundary logic, check whether you're using edges.

```csharp
var entityHalfWidth  = _entityHalfWidth;
var entityHalfHeight = _entityHalfHeight * math.abs(camUp.y); // compressed by camera tilt

if (camPos.x - entityHalfWidth < -halfWidth ...) { camSnap.x = -halfWidth + entityHalfWidth; }
if (camPos.x + entityHalfWidth >  halfWidth ...) { camSnap.x =  halfWidth - entityHalfWidth; }
if (camPos.y + entityHalfHeight > topLimit  ...) { camSnap.y =  topLimit  - entityHalfHeight; }
```

## ThrowTrajectoryPredictor — Impact Circle Height
The circle is drawn at `groundY` (the raw raycast hit surface), not at `groundFloor` (`groundY + entityHalfHeight`). The simulation stops at `groundFloor` (entity center touches ground), but the visual indicator belongs at the actual surface.

## ThrowTrajectoryPredictor — Camera-Space Half-Extents for Tilted Camera
Horizontal extent is unchanged (`ehw = entityHalfWidth`), but vertical extent must be scaled by `abs(camUp.y)` because the camera's up axis is compressed toward the horizon by the tilt angle.

## ThrowTrajectoryPredictor — Reflex Registration Order
`GrabbingInteractor` receives `ThrowTrajectoryPredictor` by constructor injection via Reflex. `ThrowTrajectoryPredictor` must be registered in `BattleGroundSceneInstaller` **before** `GrabbingInteractor`. Silent injection failure at scene load if order is wrong. See [[di-architecture]].

## CursorMovementCalculations — Velocity and Acceleration Properties
`CursorMovementCalculations` exposes `public Vector2 Velocity { get; private set; }` and `public Vector2 Acceleration { get; private set; }` (PascalCase properties, **not** fields). Acceleration = `(newVelocity - Velocity) / deltaTime`. Predictor uses `Velocity + Acceleration * LookAheadTime` as its seed velocity. `OnResume()` resets both to zero to avoid stale spikes after pause.
**Why it matters:** Raw un-smoothed acceleration is noisy — keep `LookAheadTime` small (≤0.05 s) or the arc will jitter on micro-movements.

## LineRenderer — Avoid Per-Frame Material Allocation
Access `lineRenderer.material` once at construction and store it; modifying `.color.a` on the stored instance is allocation-free. Accessing `.material` every frame creates a new material instance each time.

## Camera Drag (BattleCameraMovement)
Hold ground for `config.timeToHold` (1s default, configurable via `ConfigContainer.Battle.CameraConfig`) → `Countdown` ticks while waiting → `CameraInputHandler` fires `EventManager.Input.GroundGrabbed(true)` once → drag starts. `CameraDragHandler` reads mouse delta, scales by `moveSpeed`, applies to `CinemachineTransposer.m_FollowOffset`. `CameraBorderHandler` applies soft resistance via `borderPushCurve` when offset is outside `BorderRange` (derived from `BattleSceneData.sceneBoundary*` transforms). Release → snap back if outside bounds via `returnCurve`.

## ScrollController — Bird's-Eye Switch
Scroll up/down → `CommandDispatcher.Send(new ChangeSceneCommand(bool switchUp))`. `WorldCameraHandler` (in World scene) registers a handler that toggles bird-view GO active. Used to switch between top-down strategic and tilted battle view. `ActiveCameraOverride` (a `StateOverride`) sends the same command from a `RunConfiguration` startup. See [[events-and-services]] for the notifications-vs-commands split.

## Cursor Textures
`CursorSetter` (ILoadUnit, namespace `BarkingBird.Runtime.Gameplay.Cursor`) preloads textures from `Cursors/Textures/{OpenHandCursor, HoldingObjectCursor, HoldingGroundCursor}` (relative to `Runtime/Gameplay/Resources/`) and subscribes to `EventManager.Input.*` to switch cursor via `Cursor.SetCursor(..., CursorMode.ForceSoftware)`.
