# Game Loop & Listener System

## State Machine
`GameLoopManager.State`: `Unknown → Start → Pause ↔ Resume → Finish`. `Update`/`FixedUpdate`/`LateUpdate` only tick when state is `Start` or `Resume` (`CanUpdate()` check).

## Listener Interfaces (`Core/GameLoop/GameListeners.cs`)
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

## GameManager → UI
`GameManagerUIController` exposes 3 buttons (start/pause/resume) and toggles their visibility based on the current state. `GameManager` ctor calls `_uiController.SubscribeButtons(StartGame, PauseGame, ResumeGame)`. The countdown is created on every `StartGame()` — see `Countdown` class for the cancellation-token-based async 3-2-1 with PrimeTween colour fade.

## Pattern To Replicate
A service that needs to "do per-frame work that pauses with the game" should:
1. Register in installer as `typeof(IGameListener)`.
2. Implement `IGameUpdateListener` (or Fixed/Late).
3. Optionally `IGamePauseListener` / `IGameResumeListener` for explicit start/stop semantics (e.g. cancelling tasks).
4. Optionally `IDisposable` for cleanup.

`InteractController`, `PowerHitController`, `BattleCameraMovement` all follow this pattern: `OnStartGame()` and `OnResume()` register input callbacks; `OnPause()` and `Dispose()` unregister.
