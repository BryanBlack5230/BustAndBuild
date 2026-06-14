---
name: unity-coding-standards
description: Enforces BarkingBird studio Unity development standards including C# coding patterns, Unity architecture (Reflex DI, DOTS/ECS, EventBus notifications, CommandDispatcher requests), and code review guidelines. Triggers when writing, reviewing, or refactoring Unity C# code, implementing features, setting up dependency injection, working with commands/events, or reviewing code changes.
---

# BarkingBird Studio Unity Development Standards

You are a Unity C# architect-level developer.
⚠️ **Unity 6 (C# 9):** All patterns and examples must be compatible with Unity 6, which uses C# 9. No C# 10+ features.

**Deep references:** project-specific discoveries and system maps live in `Claude/learnings/` (start at `README.md` there). Pending design decisions live in `Claude/ConfigTask.md` and `Claude/CurrencyTask.md` — check them before touching config/balance or currency code.

## 🔴 PRIORITY 1: Code Quality & Hygiene (check FIRST)

1. **Enable nullable reference types** (`#nullable enable`) in new files — and fix all warnings.
2. **Least accessible access modifier** — private by default, explicit everywhere.
3. **Zero compiler warnings.**
4. **Throw exceptions for errors — never log errors and continue.** A required `[SerializeField]` that is null at install time → throw `InvalidOperationException`, no silent `Resources.Load` fallback.
5. **`Log.<Context>.D/W/E()` for runtime logging** — static utility, no injection, never in constructors. `Debug.Log` is for editor-only code. Tags: `Log.Battle` (combat/AI/ECS), `Log.Loading`, `Log.Boot`, `Log.World`, `Log.City`, `Log.Default`. `.D()` is auto-stripped in PROD via `[Conditional]` — never wrap it in `#if` guards. `.E(exception)` for caught exceptions; `.ThrowException(msg)` for tagged throws.
6. **readonly for non-reassigned fields, const for constants, nameof over string literals.**
7. **No inline comments** — use descriptive names. Comment only constraints the code can't express. The one sanctioned comment is a **"why-not"**: when a natural/standard approach was tried and rejected (an API misbehaves here, a guard exists for a framework constraint, a slower path is chosen for correctness), leave a short note so reviewers and a future Claude don't re-suggest the dead end.
8. **No LINQ anywhere** — explicit loops, always.
9. **No magic numbers** (agreed 2026-06-10): tunable values do NOT get hardcoded in systems/jobs. They live in config (see `Claude/ConfigTask.md`: `*Config` ScriptableObjects → blob/singleton components; `*Constants` static classes are reserved for true compile-time invariants). If the config plumbing for a value doesn't exist yet, put the value in a clearly named field/SO rather than inline, and flag it.
10. **One Unity-serialized class per file, filename = class name** — applies to every `MonoBehaviour` and `ScriptableObject`. Multiple serialized types in one file silently break inspector reference wiring (no error — references just don't persist through bake). Plain ECS structs, enums, and POCOs may share files. Project SO filename convention: `%Name%SO.cs` for profile-style SOs.
11. **Unity fake-null with `#nullable`:** use the implicit bool operator (`if (_thing)`) instead of `!= null` on `UnityEngine.Object` fields — it performs the destroyed-object check without CS8073 warnings.

## 🟡 PRIORITY 2: Modern C# (C# 9 ceiling)

- Expression bodies for simple members; null-coalescing; pattern matching (`is T x`).
- `readonly struct` for events/commands; `in` parameters for struct payloads.
- Private serialized fields `_camelCase`; properties PascalCase. Match the file you're editing (some older files use tabs — keep them).

## 🟢 PRIORITY 3: Unity Architecture

### DI: Reflex — scoped scene installers
Container hierarchy: **Project → Bootstrap → World → Battle** (additive load order; flows stitch parents via `SceneScope.OnSceneContainerBuilding`). Each scene has a `{Scope}Installer : MonoBehaviour, IInstaller`.

```csharp
// Registration with contracts — the project's standard form:
builder.AddSingleton(typeof(MyService), typeof(MyService), typeof(IGameListener), typeof(IDisposable));

// Scene-object/SO instances:
builder.AddSingleton(_sceneData, typeof(BattleSceneData));

// Helpers (ReflexExtensions):
builder.AddInterfacesAndSelf(instance);
builder.NonLazy<T>();
```

Rules:
- **`NonLazy<T>()` is mandatory for self-sufficient singletons** — anything not injected into another class (e.g. a bridge that only subscribes to events in its constructor) is never constructed without it.
- **Registration order matters** for constructor injection — register a dependency before its consumer.
- Services that need the ECS world or scene objects use an `Initialize()` method called by the scene's `*Flow`, not the constructor.
- Anything holding resources (CTS, EntityQuery, subscriptions) registers `typeof(IDisposable)`; scene-flow `RemoveListeners` disposes on unload.

### Game loop participation — listener interfaces (NOT Update() overrides)
Interfaces in `BarkingBird.Runtime.Infrastructure.GameLoop` (exact names matter):

```csharp
public sealed class MyService : IGameStartListener, IGamePauseListener, IGameResumeListener, IGameUpdateListener, IDisposable
{
    void IGameStartListener.OnStartGame() { }
    void IGamePauseListener.OnPause() { }
    void IGameResumeListener.OnResume() { }
    void IGameUpdateListener.OnUpdate(float deltaTime) { }
    public void Dispose() { }
}
```

Also: `IGameFinishListener.OnFinishGame()`, `IGameFixedUpdateListener.OnFixedUpdate(float)`, `IGameLateUpdateListener.OnLateUpdate(float)`. Register with `typeof(IGameListener)` contract — `GameLoopManager` auto-collects them. For long-running async work that must pause, use the CTS + phase-preservation pattern (see `Claude/learnings/game-loop-listeners.md`), not a per-frame tick.

### Messaging: three mechanisms — pick by who-knows-whom
Full decision guide: `Claude/learnings/events-and-services.md`. Summary:

1. **Direct DI injection (the default).** If the sender can hold the service and no layering boundary is crossed, inject and call the method. A dispatcher hop that resolves to one same-layer handler is ceremony — delete it.
2. **`CommandDispatcher.Send(in cmd)` — imperative "do X", strictly 1-to-1.** Reflex-injected (Project scope), NOT static. Use when crossing a layer boundary (Input → Gameplay, SO/`SerializeReference` → runtime service) or when intent must be reified as data. Commands are `readonly struct X : ICommand` — plain data, **no pooling, no Execute() method**. `Register<T>` throws on duplicate registration; handlers unregister via the returned `IDisposable`.
3. **`EventBus.Raise(in evt)` — past-tense notification, 1-to-many, static.** Events are `readonly struct X : IEvent`. Subscribe in ctor/`Register()`, unsubscribe in `Dispose()`. Priorities supported; unhandled events are normal. Safe to call from managed `SystemBase.OnUpdate` (NOT from Burst `ISystem`).

```csharp
// Command (request):           // Event (notification):
public readonly struct StartDayCommand : ICommand { }
public readonly struct DayStartedEvent : IEvent { }

_dispatcher.Send(new StartDayCommand());
EventBus.Raise(new DayStartedEvent());
```

File placement: one type → own file next to the owner; multiple types per owner → `<Owner>_EventsAndCommands.cs`; shared domain → `<Area>_EventsAndCommands.cs`. No `Events/`/`Commands/` folders.

### DOTS/ECS
- Gameplay systems go in `[UpdateInGroup(typeof(GameLoopSystemGroup))]` to be pause-aware; placing a system outside it (physics groups, `OrderLast`) is a deliberate decision — say why.
- Namespace buckets: root `Components/`/`Systems/` files = **global namespace**; `Components/AI/` + `Systems/AI/` = `BarkingBird.Runtime.Gameplay.AI`.
- Mono↔ECS via bridge classes (`DaylightEcsBridge`, `CursorEcsBridge` pattern); ECS→Mono notifications via `EventBus` from a managed `SystemBase`.
- See the `dots-new-system` skill for the full new-system checklist and `Claude/learnings/ecs-patterns.md` for query/ECB/aspect gotchas.

### Async: UniTask
- **Never `async void`** — use `async UniTaskVoid` + `.Forget()` for fire-and-forget; exceptions must surface.
- Pause/cancel via `CancellationTokenSource`; `try / catch (OperationCanceledException) {}` around loops.
- **Re-throw `OperationCanceledException` before any general `catch`** when the token came from a caller (you don't own the CTS) — swallowing it mid-chain hides the cancellation from the owner. Only the loop that *owns* the CTS swallows it (see `Claude/learnings/game-loop-listeners.md`).

### IL2CPP (shipping builds)
- **No `System.Reflection.Emit`** — unsupported under IL2CPP; runtime codegen throws on device.
- Methods invoked **only via reflection** need `[UnityEngine.Scripting.Preserve]` so managed-code-stripping doesn't drop them.

### Resources
- All `Resources.Load*` goes through `AssetService.R.Load<T>(path)`.
- All Resources/asset path strings live in `RuntimeConstants` — no string literals at call sites.

## Review Severity

**🔴 Critical:** logging errors instead of throwing; `Debug.Log` in runtime code; `async void`; LINQ; multiple serialized classes per file; static event subscription without a `SubsystemRegistration` reset (domain reload is OFF — see `Claude/learnings/play-mode-and-hot-reload.md`); missing `Dispose`/unsubscribe; new magic numbers in systems/jobs; inventing patterns the codebase doesn't have (pooled commands, controllers, service locators).

**🟡 Important:** missing readonly/const/nameof; `#if` guards around `Log.D`; wrong Log tag; missing `NonLazy` on self-sufficient singletons; registration-order bugs; per-frame allocations (cache `GUILayoutOption`s, materials, queries).

**🟢 Suggestion:** expression bodies, pattern matching, naming improvements.

## Diagnostics & Review Feedback

A diagnostic (IDE inspection, analyzer) or review comment is *general* guidance — it isn't always right for the code at hand. Apply it when it fits; when it doesn't (hurts readability, conflicts with a deliberate local design):
- **Decline and say why** — report the declined item + reason to the user, and if the surrounding code is non-obvious leave a "why-not" comment (see Priority 1, rule 7) so the same note isn't re-raised next review.
- **Suppress a mis-firing diagnostic at the narrowest scope** — a single line/member via `[SuppressMessage]` or `// ReSharper disable once <Inspection>`, never a file- or assembly-wide blanket.

## Research
- Verify Unity APIs against the local editor docs or primary source before implementing — don't guess from memory.
- Unity Discussions (Discourse) render badly for scraping — append `/print` to a thread URL to retrieve the full text.
