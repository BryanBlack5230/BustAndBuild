# Currency & Save System

The first persistence feature. Replaced the PoC `WorldCurrency` (in-memory, pearls-only) with a generalized `Wallet` + a save seam. Design agreed in `Claude/CurrencyTask.md`; this file records the *as-built* shape and the non-obvious decisions.

## Layer map (who lives where)
- **`Gameplay.Currency`** (`_MonoWorld/Currency/`): `CurrencyType` enum, `Wallet`, `CurrencyChangedEvent`, `PickupCollectedEvent`, `ResourceCounterView`, `PickupMagnetController`, `FlyingPickup`.
- **`Gameplay.GameWorld`** (`_MonoWorld/GameWorld/`): `WorldSaveData` (DTO), `WorldSaveService`.
- **`Infrastructure.Save`** (`Infrastructure/Save/`): `ISaveSystem`, `DummySaveSystem`, `ActiveSlot`.

**Namespace gotcha:** the world-save folder namespace is `GameWorld`, **not** `World` — `BarkingBird.Runtime.Gameplay.World` collides with `Unity.Entities.World` in any file that touches ECS, producing ambiguous-reference errors. Always use `GameWorld`.

## Wallet — standalone domain state, service-hydrated
**Context:** churned through three ownership designs before landing here.  
**Finding:** `Wallet` is a plain POCO bound as a bare singleton in `WorldSceneInstaller` (`AddSingleton(typeof(Wallet), typeof(Wallet))`) — it "exists by itself," parameterless, all currencies zero. It does **not** hydrate itself and the installer does **not** hydrate it (no factory). Instead `WorldSaveService` is injected with the wallet and calls `_wallet.Hydrate(_data.Currencies)` in its constructor.  
**Why it matters:** keep hydration out of the installer and out of the Wallet's ctor. The Wallet exposes `Hydrate(int[])` (silent — raises no `CurrencyChangedEvent`, since load is initialization not a gameplay change), `Get/Add/TrySpend(CurrencyType,…)`, and `Snapshot()`. Semantics preserved from `WorldCurrency`: `Add` ignores `amount <= 0`; `TrySpend` throws on `<= 0`, returns false when insufficient.

## Hydration ordering is safe via NonLazy
`WorldSaveService` is registered `NonLazy<WorldSaveService>()`, so Reflex constructs it (and thus hydrates the wallet) during scene injection in `SceneScope.Awake` (execution order `-1e9`) — **before** `PearlsUIView.OnEnable` reads `_wallet.Get(Pearls)`. That's why `Hydrate` can be silent: the UI reads the already-loaded total on enable, no refresh event needed. If you ever make hydration lazy, the UI will show stale zeros until the first change event.

## ISaveSystem is a dummy seam (no disk I/O yet)
**Decision (Bryan):** build the full architecture but back persistence with an in-memory `DummySaveSystem` — real atomic-JSON file writes are deferred. `DummySaveSystem` keeps a `Dictionary<string, WorldSaveData>`; `Load` returns the cached instance (or a fresh record), `Save` logs `Log.Default.W(… NOT persisted to disk)`. Consequence: currency **survives in-session world re-entry** but is **lost on app restart**. `WorldSaveData` carries `Version` (const `CurrentVersion`) from day one for future migration. It stores currencies as a raw `int[]` and stays enum-agnostic — the `Wallet` owns the `CurrencyType`↔index mapping, so `Infrastructure.Save` never depends on the gameplay enum.

## DI scoping
- **Bootstrap scope** (`BootstrapInstaller`): `ActiveSlot` + `DummySaveSystem` (as `ISaveSystem`). `ActiveSlot` hardcodes `"world_0"` — no world-select UI exists; `ActiveSlot.Select()` is the seam for when it does.
- **World scope** (`WorldSceneInstaller`): `Wallet` + `WorldSaveService` (`IDisposable`, `NonLazy`). World container inherits the Bootstrap bindings via the parent chain, so `WorldSaveService` resolves `ISaveSystem`/`ActiveSlot` from Bootstrap. This is the correct lifetime — wallet dies with the World scene, next world hydrates fresh.

## Save timing (what actually fires)
`WorldSaveService` marks dirty on `CurrencyChangedEvent`, flushes (debounced via `_dirty`) on `DayEndedEvent` and on `Dispose()`. **`Dispose` fires on scene unload** because Reflex disposes the World container on `SceneManager.sceneUnloaded` (`UnityInjector` → `container.Dispose()` → all `IDisposable` bindings). Deliberately **not** wired this pass: return-to-city (no `CityFlow`/installer exists yet) and `OnApplicationPause/Quit` (a no-op against in-memory storage — lands with real file I/O). Both marked `TODO` in the service.

## CurrencyChangedEvent replaced PearlsChangedEvent
One event for all currencies: `CurrencyChangedEvent(CurrencyType Type, int NewTotal, int Delta)`, owned by `Wallet`, lives in `Gameplay.Currency` (not `Infrastructure/EventBus/`) because it references the gameplay `CurrencyType` enum. Subscribers filter on `Type`. `PearlsUIView` filters `Type == Pearls`. `PearlMagnetController` still credits in the tween's `OnComplete` (Bryan accepts the "value lost if tween interrupted" flaw as negligible).

## CurrencyType enum is append-only
`enum CurrencyType { Pearls, Food, Wood, Stone, Iron, Faith }` with explicit values. Saves index by enum value, so **append new currencies at the end** — never reorder or remove without a save migration. `CurrencyType` is also the single key shared across the whole resource chain: it tags the ECS `Pickup`/`ResourceDrop`, indexes the spawner prefab map, routes the magnet animation, and indexes the `Wallet` — one enum, no parallel "resource id" type.

## Generic pickup / resource-drop pipeline (as-built, replaced pearl-only)
The original pearl-only spawn/pickup was generalized so any resource can drop and be collected the same way. "IPickable" is **not** an interface — in ECS it's a shared archetype: one `Pickup { CurrencyType Type; float Value; }` component (+ the lifetime/float/blink enableables), resource-agnostic systems, and one small prefab per visual.
- **Drop table = per-type, on the profile SO** (since ConfigHub Phase 5 landed, 2026-06-14 — see [[config-system]]). The table lives on `EnemyUnitProfile.Drops` (`List<DropTableEntry>`: `type, min, max, chance, value`); `EnemyAuthoring` keeps only a per-instance escape hatch — a `bool overrideDrops` + its own `drops` list. The baker bakes `overrideDrops ? authoring.drops : profile.Drops` into a `DynamicBuffer<ResourceDrop>` (empty if both are null). Each row is rolled **independently** on death (pearls = a 100%-chance row; wood = 0.20; etc.) — an enemy can drop several resources at once.
- **One prefab per resource.** `PickupSpawnerAuthoring` maps `CurrencyType → prefab` and bakes a `DynamicBuffer<PickupPrefabRef>` (indexed by enum; unassigned slots = `Entity.Null`, skipped). MUST be a buffer not a FixedList — see the entity-remap gotcha in [[ecs-patterns]]. Each prefab carries its own baked scale; the spawn util preserves it (no shared scale constant).
- **Spawn paths.** `PickupSpawnOnDeathSystem` (death) and `EnemyEscapeSystem` (scared escape → HALF counts at the base boundary) both iterate the drop buffer, roll per row, look up the prefab, and call the shared `PickupSpawnUtility.Spawn`.
- **Blink fix.** Lifetime blink now toggles `LocalTransform.Scale` relative to a per-pickup `BaseScale` (stored at spawn = prefab scale), not the old hardcoded `0.25/0.1` — so any-size resource prefab blinks correctly.
- **UI = one slot per resource.** `ResourceCounterView` (one per `CurrencyType`, exposes `Type`/`MagnetTarget`/`Icon`) replaced the single `PearlsUIView`; `PickupMagnetController` holds a `ResourceCounterView[]`, builds a `type→view` dict, and flies a `FlyingPickup` icon to the right counter, crediting `Wallet.Add(type, amount)` on landing (see the serialize-the-icon-ref pattern in [[unity-csharp-patterns]]).
- **Files:** `Components/PickupAuthoring.cs`, `Components/PickupSpawnerAuthoring.cs`, `Components/AI/{ResourceDrop,DropTableEntry}.cs`, `Systems/{PickupSpawnUtility,PickupSpawnOnDeathSystem,PickupLifetimeSystem,PickupFloatSystem,PickupCollectSystem}.cs`, `_MonoWorld/Currency/{PickupCollectedEvent,ResourceCounterView,PickupMagnetController,FlyingPickup}.cs`. All former `Pearl*` files were `git mv`'d (with `.meta`, GUIDs preserved) — the pearl-specific memory note is superseded by this section.
