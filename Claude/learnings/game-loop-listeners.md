# Game Loop & Listener System

## State Machine
`GameLoopManager.State`: `Unknown → Start → Pause ↔ Resume → Finish`. `Update`/`FixedUpdate`/`LateUpdate` only tick when state is `Start` or `Resume` (`CanUpdate()` check).

## Listener Interfaces (`Runtime/Infrastructure/GameLoop/GameListeners.cs`, ns `BarkingBird.Runtime.Infrastructure.GameLoop`)
Base marker: `IGameListener`. Concrete:
- `IGameStartListener.OnStartGame()` — called when `GameManager.StartGame()` runs (after 3-sec countdown).
- `IGameFinishListener.OnFinishGame()`
- `IGamePauseListener.OnPause()` / `IGameResumeListener.OnResume()`
- `IGameUpdateListener.OnUpdate(float deltaTime)`
- `IGameFixedUpdateListener.OnFixedUpdate(float fixedDeltaTime)`
- `IGameLateUpdateListener.OnLateUpdate(float deltaTime)`

Listeners may implement multiple interfaces; `GameLoopManager` dispatches to whichever ones match. Update listeners are also held in dedicated `List<>`s for fast iteration; Start/Finish/Pause/Resume iterate the full `_listeners` and pattern-match (no perf concern, runs once per state change).

## Mid-Game Listener Registration (`AddListeners`)
When a scene loads additively (Battle/World), the new flow calls `_gameLoopManager.AddListeners(_listeners)`. `ApplyState(collection)` then catches new listeners up to the current state — e.g. if state is `Resume`, every new `IGameStartListener` gets `OnStartGame()` first, then `OnResume()`. **Why:** avoids "I joined mid-game but missed StartGame" bugs.

## Removal Disposes
`RemoveListeners` calls `Dispose()` on any `IDisposable` listener. So if a service holds resources (CancellationTokenSource, EntityQuery, dictionary of in-flight tasks), implement `IDisposable` and registration drops them cleanly on scene unload.

## DotsGameLoopBridge — The ECS Hook
`DotsGameLoopBridge : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameFinishListener, IDisposable`. It flips `GameLoopSystemGroup.Enabled` on/off. **All gameplay ECS systems should live under `[UpdateInGroup(typeof(GameLoopSystemGroup))]`** to be pause-aware. Systems outside the group (e.g. `ApplyDamageSystem` is in `SimulationSystemGroup, OrderLast = true`) keep ticking even when paused — check intentionality.

`GameLoopSystemGroup` lives in `SimulationSystemGroup`, ordered `UpdateAfter(BeginSimulationEntityCommandBufferSystem)`.

**Planned pause direction (Bryan, 2026-06-10):** the long-term plan is a **"soft" pause** — time slowed to a very small scale rather than fully stopped. That's why "physics/damage/death keep running while paused" hasn't been fixed: the hard on/off `GameLoopSystemGroup.Enabled` toggle is interim. Factor this in before adding new pause-exempt systems or "fixing" pause behavior.

## GameState — Single Source of Truth (SSOT)
The lifecycle state is a **top-level public enum** `GameState { Unknown, Start, Finish, Pause, Resume }` (`Infrastructure/GameLoop/GameState.cs`), **owned by `GameLoopManager`** which exposes `public GameState State => _state;`. Read `GameLoopManager.State` rather than inferring state from UI or local flags. (Was a nested `GameLoopManager.State` enum until the SSOT refactor — same 5 values/order.)

Every transition (`StartGame/FinishGame/PauseGame/ResumeGame`) routes through private `SetState(GameState)`, which assigns `_state` **and** `EventBus.Raise(new GameStateChangedEvent(state))`. Funnelling through one method guarantees the value and the notification can never drift apart.

## Request vs. Notification Split (keeps UI in sync from any trigger)
Two directions, deliberately separated:
- **Request (UI → state):** buttons are wired via `GameManager` ctor → `_uiController.SubscribeButtons(StartGame, PauseGame, ResumeGame)`; clicks call `GameManager` methods → `GameLoopManager` transitions. The click handler does **not** repaint itself.
- **Notification (state → UI):** `GameLoopManager` raises `GameStateChangedEvent`; `GameManagerUIController` subscribes (`OnEnable`/`OnDisable`, holds the `IDisposable`) and repaints via one `Render(GameState state)` switch.

**Why it matters:** any path that mutates state — button, `GameLoopStateOverride`, or future caller — repaints the UI identically. This fixed a desync bug where `GameLoopStateOverride` called `GameLoopManager` directly (bypassing `GameManager`), so the buttons never updated. The UI used to be manually poked with `OnStart/OnPause/OnResume` from `GameManager` — those are gone; the UI is now purely reactive.

**The one intentional exception:** `GameManager.StartGameAsync()` calls `_uiController.Render(GameState.Start)` *before* the 3-2-1 countdown so Pause shows immediately (and Start can't be double-clicked). The post-countdown `GameLoopManager.StartGame()` raises the event and repaints to the same running layout — idempotent. The countdown itself (`Countdown`, cancellation-token async 3-2-1 + PrimeTween colour fade) only runs on the button path, not on override-triggered Start.

**Render mapping:** `Start`/`Resume` → show Pause; `Pause` → show Resume; `Unknown` → show Start (initial, set in `Start()`); `Finish` → hide Pause+Resume (new behaviour — finish used to leave the UI untouched since `FinishGame` never touched it).

## Pattern To Replicate
A service that needs to "do per-frame work that pauses with the game" should:
1. Register in installer as `typeof(IGameListener)`.
2. Implement `IGameUpdateListener` (or Fixed/Late).
3. Optionally `IGamePauseListener` / `IGameResumeListener` for explicit start/stop semantics (e.g. cancelling tasks).
4. Optionally `IDisposable` for cleanup.

`InteractController`, `PowerHitController`, `BattleCameraMovement` all follow this pattern: `OnStartGame()` and `OnResume()` register input callbacks; `OnPause()` and `Dispose()` unregister.

## Pause-Aware UniTask Loops (CTS + Phase Preservation)
When the work isn't a per-frame tick but a **long-running async loop** that must halt on pause (e.g. `DayNightCycle`'s day progression), `IGameUpdateListener` is the wrong tool — `GameLoopManager.Update()` will just skip the tick during pause, which makes per-frame `Time.deltaTime` accumulation drift if you also need the loop's internal scheduling. Use the CTS pattern instead:
1. `OnPause()` → cancel the CTS.
2. `OnResume()` → relaunch the loop with **preserved progress state** (`_elapsedSeconds`, current phase, etc.).
3. `Dispose()` → cancel CTS + unsubscribe events.
4. The `await UniTask.Yield(PlayerLoopTiming.Update, ct)` + `try / catch (OperationCanceledException) {}` shape matches `Countdown.cs` in `Runtime/Infrastructure/Utilities/`.

When the service has **multiple phases** that can be active when pause hits (e.g. normal day loop vs. sundown tween), track a `_phase` enum and have `OnResume()` switch on it to relaunch the right loop. Don't try to model "is sundown in progress" with a second bool alongside `_dayInProgress` — the enum keeps state transitions explicit and exhaustive.
