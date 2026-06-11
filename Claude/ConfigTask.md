# Task: Config/Balance Refactor — SO hub, per-unit-type profiles, no magic numbers

Status: **designed, not started**. Decisions below were agreed with Bryan (2026-06-10 review session).
Schedule note: land this **before** the health/barracks/wall tasks in `Assets/!_Game/Tasks.md` —
each of those adds tunables and should be born into the new system.

## Goal

Kill the config split-brain (problem 4) and scattered magic numbers (problem 9):
- `Config.json` / `ConfigContainer.Battle` / `ConfigGenerator` path is dead code — **delete it**.
  ScriptableObjects become the single source of truth (Option A: fastest prototyping).
- All balance tunables move out of Burst jobs / systems into config SOs, baked into ECS at bootstrap.

## Agreed Decisions

### Naming convention
- `*Constants` = true compile-time invariants only (static classes; e.g. existing `SteeringConstants`
  with the 8 direction vectors **keeps its name**, stays static).
- `*Config` = tunable ScriptableObjects (e.g. `SteeringConfig`, `InAirCollisionConfig`,
  `BattleBrainConfig`, `ScreenBounceConfig`).
- Rule: "constants never change; configs are balance knobs."

### The hub
- Promote `PrototypeConfigSetter` into the single config holder: stays a **MonoBehaviour in the
  Bootstrap scene** (DI binding in `BootstrapInstaller` and injection into `BlobContainer` stay
  structurally the same).
- It references all config SO assets (separate files on disk, single editing surface).
- Use **Odin `[InlineEditor]`** on the SO reference fields so every sub-config is editable in place —
  this is the fix for "annoying to jump between SO files".
- Add a **"Rebake" button** on the hub (Odin `[Button]`): play-mode hot reload — dispose old blob,
  recreate blob + re-push singleton components. `BlobContainer` is now `IDisposable` and
  `Initialize()` already disposes a previous blob if present (added 2026-06-10) — Rebake can reuse that.

### Per-unit-type balance
- **One profile SO per unit type** (enemy types and ally types).
- Each profile SO carries its own `UnitType` enum field — **implicit tie to the enum**; the old
  "array index = enum value" convention was temporary and must go. Baker maps `SO.UnitType → blob slot`.
- Hub `OnValidate` warns on missing or duplicate unit types.

### Field naming fix (the unit-confusion trap)
- Authoring fields: `DetectionRadius` (meters), `ViewAngleDegrees` (degrees).
- `BlobContainer.ConvertToStruct` is the ONLY place computing derived forms (`DetectionRadiusSq`,
  `ViewAngleCos`). Current bugs to fix while renaming:
  - `BlobContainer.cs:61` squares an already-"Sq"-named field: `DetectionRadiusSq * DetectionRadiusSq`.
  - `BlobContainer.cs:62` treats `ViewAngleCos` as degrees: `math.cos(math.radians(source.ViewAngleCos * 0.5f))`.

### Delivery into ECS
- **Arrays → blob**: per-unit-type profiles stay in `TargetProfilesBlob` (extended as needed).
- **Flat scalar groups → `IComponentData` singletons** created at bootstrap — same pattern as
  `ThrowSettingsSetter` / `PearlSettings` already use.

## Magic-number inventory to migrate (from the 2026-06-10 review)

| Current location | Value | Destination |
|---|---|---|
| `InAirCollisionSystem.cs` | ally damage ×0.25 (appears multiple times) | **per-unit-type profile** (decided) |
| `InAirCollisionSystem.cs` | soft-land damage split 0.8 / 0.2 | `InAirCollisionConfig` |
| `InAirCollisionSystem.cs` | rebound 0.15, knockMagnitude 0.5 | `InAirCollisionConfig` |
| `ScreenBounceSystem.cs` | unit half-extents rect 0.25 × 0.5 | **per-unit-type profile** (decided) |
| `ScreenBounceSystem.cs` + `InAirCollisionSystem.cs` | duplicated fallback minVel/maxVel 5 / 10 | `InAirCollisionConfig` (single source) |
| `BattleBrainSystem.cs` (~line 136) | evade quick-fix `attackRangeSq * 32` (has TODO) | `BattleBrainConfig` |
| `EnemyEscapeSystem.cs` | `EscapeDwellSeconds = 2f` | `BattleBrainConfig` (or own `EscapeConfig` if it grows) |
| `Steer_ResolveSystem.cs` | `DangerMultiplier = 1f`, `LookAheadDistance = 2.0f` | `SteeringConfig` |
| `InteractController.cs` | raycast `GetPoint(9f)` / `GetPoint(40f)` | candidate — Mono-side, plain serialized fields or config; decide in PR |

Out of scope here: `SpawningSystem` RNG seed `(uint)ElapsedTime * 10007` (correctness bug, not tuning —
fix separately with stored Random state).

## Delete list (JSON path)

- `Config.json` (under `Runtime/Gameplay/Resources/Settings/`).
- `ConfigGenerator.cs` (in `Assets/!_Game/Editor/`).
- `ConfigContainer`'s `Battle` profile section + the commented-out `_container.Battle.*` lines in
  `BlobContainer.CreateProfilesBlob` (lines 38, 46). **Check what else `ConfigContainer` still serves**
  (it's loaded in `BootstrapFlow` via `LoadingService`) — only delete the balance part; if nothing else
  uses it, remove the whole load step.
- `TargetProfile` POCO class may be replaced by the SO type — grep `TargetProfile` before deleting.

## Key files

| File | Role |
|---|---|
| `Assets/!_Game/Runtime/Infrastructure/Settings/BlobContainer.cs` | Blob baker; now `IDisposable`; fix unit math here; reads `_prototypeConfig.EnemyProfiles/AllyProfiles` today |
| `PrototypeConfigSetter` (grep; `BarkingBird.Runtime.Gameplay.Settings`, serialized on Bootstrap scene, bound in `BootstrapInstaller`) | becomes the hub |
| `.../_MonoWorld/Scenes/BootstrapInstaller.cs` | hub + `BlobContainer` (with `IDisposable` contract) bindings |
| `.../_MonoWorld/Scenes/BootstrapFlow.cs` | calls `_blobContainer.Initialize()` after loading chain |
| `.../Systems/AI/TargetScorerJob.cs` | consumes blob profiles (`TargetProfilesBlob`); note: `LowHealthBonus` is baked but **never used** in scoring — implement or drop during refactor |
| `ThrowSettingsSetter` / `PearlSettings` | reference pattern for SO → singleton component |
| `Assets/!_Game/Runtime/Gameplay/!_Scripts/Components/AI/` + `Systems/AI/` | systems listed in the inventory table |

## Project conventions to follow
- One class per file, file name = class name (hard rule — inspector wiring breaks otherwise).
- SO assets live under `Runtime/Gameplay/Resources/Settings/` next to the existing settings assets.
- Odin available — use `odin-visual-designer` skill when building the hub inspector.
- No LINQ; `Log.<Context>.D/W/E()`; throw for errors; UniTask (`UniTaskVoid` + `.Forget()`, no `async void`).

## Implementation order (suggested)
1. Define config SO classes + per-unit-type profile SO (with `UnitType` field), create assets matching current live values from `PrototypeConfigSetter`.
2. Promote hub: reference fields + Odin inline editors + `OnValidate` enum completeness check.
3. Rename/fix `DetectionRadius` / `ViewAngleDegrees` end-to-end; bake derived values only in `ConvertToStruct`.
4. Extend `BlobContainer` to bake from hub SOs; add singleton-component creation for the system configs.
5. Migrate the magic-number inventory; systems read singletons instead of literals (per-unit values → profile blob).
6. Add Rebake button (dispose + re-Initialize path).
7. Delete the JSON path (after grep-verifying `ConfigContainer` usage).
8. Verify in play mode: behaviour unchanged with same values; Rebake reflects SO edits live.
