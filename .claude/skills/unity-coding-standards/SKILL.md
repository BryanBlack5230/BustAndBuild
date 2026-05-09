---
name: unity-coding-standards
description: Enforces BarkingBird studio Unity development standards including C# coding patterns, Unity architecture (Reflex DI, DOTS/ECS, Command pattern with CommandFactory), and code review guidelines. Triggers when writing, reviewing, or refactoring Unity C# code, implementing features, setting up dependency injection, working with commands/events, or reviewing code changes.
---

# BarkingBird Studio Unity Development Standards

You are a Unity C# architect-level developer. 
⚠️ **Unity 6 (C# 9):** All patterns and examples are compatible with Unity 6, which uses C# 9. No C# 10+ features are used.

## Skill Purpose

This skill enforces BarkingBird Studio's comprehensive Unity development standards with **CODE QUALITY FIRST**:

**Priority 1: Code Quality & Hygiene** (MOST IMPORTANT)
- Nullable reference types, access modifiers, fix all warnings
- Throw exceptions (never log errors)
- `Log.<Context>.D/W/E()` static utility — choose tag matching the system; `.D()` auto-stripped in PROD; no `#if` guards needed
- readonly/const, nameof, using directive scope
- No inline comments (use descriptive names)

**Priority 2: Modern C# Patterns**
- Expression bodies, null-coalescing operators, pattern matching
- Modern C# features (records, init, with)

**Priority 3: Unity Architecture**
- Reflex DI (scoped scene installers, container hierarchy)
- DOTS/ECS (GameLoopSystemGroup, game loop listener pattern)
- Command pattern + CommandFactory with pools for inter-system messaging
- Data Controllers pattern (NEVER direct data access)
- Resource management, UniTask async patterns

**Priority 4: Performance & Review**
- Allocation prevention in hot paths
- Code review guidelines

## When This Skill Triggers

- Writing or refactoring Unity C# code
- Implementing Unity features with dependency injection
- Working with commands and inter-system messaging
- Accessing or modifying game data
- Reviewing code changes or pull requests
- Setting up project architecture

## Quick Reference Guide

### What Do You Need Help With?

| Priority | Task | Reference |
|----------|------|-----------|
| **🔴 PRIORITY 1: Code Quality (Check FIRST)** | | |
| 1 | Nullable types, access modifiers, warnings, exceptions | [Quality & Hygiene](references/csharp/quality-hygiene.md) ⭐ |
| 1 | `Log.<Context>.D/W/E()` — tag selection, method choice, no `#if` guards | [Quality & Hygiene](references/csharp/quality-hygiene.md) ⭐ |
| 1 | readonly/const, nameof, using scope, no inline comments | [Quality & Hygiene](references/csharp/quality-hygiene.md) ⭐ |
| **🟡 PRIORITY 2: Modern C# Patterns** | | |
| 2 | Expression bodies, null-coalescing, pattern matching | [Modern C# Features](references/csharp/modern-csharp-features.md) |
| 2 | Records, init, with expressions | [Modern C# Features](references/csharp/modern-csharp-features.md) |
| **🟢 PRIORITY 3: Unity Architecture** | |
| 3 | Reflex dependency injection, scene installers | [Reflex DI](references/unity/reflex-di.md) |
| 3 | DOTS/ECS, GameLoopSystemGroup, listener interfaces | [DOTS Architecture](references/unity/dots-architecture.md) |
| 3 | Command pattern + CommandFactory (pooled) | [Command Pattern](references/unity/command-pattern.md) |
| 3 | Data Controllers pattern (NEVER direct data access) | [Data Controllers](references/unity/data-controllers.md) |
| 3 | Service/Bridge/Adapter integration | [Integration Patterns](references/unity/integration-patterns.md) |
| 3 | UniTask async/await patterns | [UniTask Patterns](references/unity/unitask-patterns.md) |
| **🔵 PRIORITY 4: Performance & Review** | | |
| 4 | Allocation prevention, hot path budgets | [Performance](references/csharp/performance-optimizations.md) |
| 4 | Architecture review (DI, commands, controllers) | [Architecture Review](references/review/architecture-review.md) |
| 4 | C# quality review (null handling, expressions) | [C# Quality](references/review/csharp-quality.md) |
| 4 | Unity-specific review (components, lifecycle) | [Unity Specifics](references/review/unity-specifics.md) |
| 4 | Performance review (allocations, hot paths) | [Performance Review](references/review/performance-review.md) |

## 🔴 CRITICAL: Code Quality Rules (CHECK FIRST!)

### ⚠️ MANDATORY QUALITY STANDARDS

**ALWAYS enforce these BEFORE writing any code:**

1. **Enable nullable reference types** - No nullable warnings allowed
2. **Use least accessible access modifier** - private by default
3. **Fix ALL warnings** - Zero tolerance for compiler warnings
4. **Throw exceptions for errors** - NEVER log errors, throw exceptions
5. **`Log.<Context>` for runtime** - Static utility, no injection needed. Pick the tag matching the system context (`Log.Battle`, `Log.Loading`, `Log.Boot`, `Log.World`, etc.). `.D()` is auto-stripped in PROD — no `#if` guards needed. NEVER log in constructors. `Debug.Log` ONLY for editor scripts.
6. **Use readonly for fields** - Mark fields that aren't reassigned
7. **Use const for constants** - Constants should be const, not readonly
8. **Use nameof for strings** - Never hardcode property/parameter names
9. **Using directive in deepest scope** - Method-level using when possible
10. **No inline comments** - Use descriptive names; code should be self-explanatory

**Example: Enforce Quality First**

```csharp
// ✅ EXCELLENT: All quality rules enforced
#nullable enable

using GameEngine.Utils.Logging;

public sealed class PlayerService
{
    private const int MaxHealth = 100;

    public Player GetPlayer(string id)
    {
        using System.Text.Json;

        return players.TryGetValue(id, out var player)
            ? player
            : throw new KeyNotFoundException($"Player not found: {id}");
    }

    private void LoadGameData()
    {
        Log.Default.D("Game data loaded"); // auto-stripped in PROD, no #if needed
    }
}

#if UNITY_EDITOR
public class EditorTool
{
    public void Process()
    {
        Debug.Log("Processing..."); // Debug.Log OK in editor
    }
}
#endif
```

## ⚠️ Unity Architecture Rules (AFTER Quality)

### Logging: `Log.<Context>` Static Utility

`Log` (`GameEngine.Utils.Logging`) is a static class — **no injection, no field, no constructor argument**. Pick the tag that matches the system context:

| Tag | Use for |
|-----|---------|
| `Log.Battle` | Combat, AI, units, ECS systems |
| `Log.Loading` | Async loading, asset loading |
| `Log.Boot` | Bootstrap, startup, DI setup |
| `Log.World` | World scene, day/night, environment |
| `Log.Default` | General / cross-cutting |

Methods on each `TagLog`:

| Method | When to use |
|--------|-------------|
| `.D(msg)` | Debug info — **auto-stripped in PROD** via `[Conditional]`, no `#if` guard needed |
| `.D(subTag, msg)` | Debug with extra sub-tag (e.g. `Log.Battle.D("Spawn", "unit created")`) |
| `.W(msg)` | Warning — always compiled in |
| `.E(msg)` | Error log — always compiled in, does **not** throw |
| `.E(exception)` | Log a caught exception |
| `.ThrowException(msg)` | Log-tagged throw — use instead of `throw new Exception` when the tag adds context |

```csharp
// ✅ CORRECT: static call, tag matches context, no injection
using GameEngine.Utils.Logging;

public sealed class BattleUnitRegistrationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Log.Battle.D("Registering new units");
    }
}

// ✅ CORRECT: sub-tag for finer categorisation
Log.Battle.D("Spawn", $"Spawned {count} enemies at {position}");

// ✅ CORRECT: warning stays in PROD
Log.Loading.W("Asset load took longer than expected");

// ❌ WRONG: injecting a logger
public sealed class SomeService
{
    private readonly ILogger logger; // don't do this — use Log.* static
}

// ❌ WRONG: wrapping D() in a guard
#if !PROD
Log.Battle.D("msg"); // redundant — [Conditional] already strips it
#endif
```

### DI: Reflex — Scoped Scene Installers

Use **Reflex** for all dependency injection. Each scene has a scoped installer class. Containers form a hierarchy: Bootstrap → World → Battle (additive loading order).

- `AddInterfacesAndSelf<T>()` — bind a type and all its interfaces
- `NonLazy<T>()` — force immediate construction at scene load

```csharp
// ✅ CORRECT: Reflex scoped installer
public sealed class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton<InteractController>()
               .AddInterfacesAndSelf();

        builder.AddSingleton<BattleCameraMovement>()
               .NonLazy();
    }
}
```

### DOTS/ECS — GameLoopSystemGroup + Listener Pattern

All simulation runs inside `GameLoopSystemGroup` (extends `SimulationSystemGroup`). MonoBehaviour classes participate via listener interfaces from `GameListeners.cs`:

- `IGameStartListener`, `IGamePauseListener`, `IGameResumeListener`, `IGameFinishListener`
- `IGameUpdateListener`, `IGameFixedUpdateListener`, `IGameLateUpdateListener`

`DotsGameLoopBridge` enables/disables `GameLoopSystemGroup` when game state changes.

```csharp
// ✅ CORRECT: MonoBehaviour participates in game loop via listener interface
public sealed class BattleCameraMovement : MonoBehaviour, IGameUpdateListener, IGameStartListener
{
    void IGameStartListener.OnGameStart() { ... }
    void IGameUpdateListener.OnUpdate() { ... }
}
```

### Commands — CommandFactory + Pools

Use the **Command pattern** with `CommandFactory` for all inter-system messaging and one-shot actions. Commands are pooled for zero-allocation dispatch.

- `CommandFactory.Get<T>()` — rent a command from its pool
- After `Execute()`, the command returns itself to the pool
- Subscribers register handlers per command type; they do NOT hold references to commands after the handler returns

```csharp
// ✅ CORRECT: Pooled command dispatch via CommandFactory
public sealed class AttackSystem
{
    private readonly CommandFactory CommandFactory;

    public AttackSystem(CommandFactory CommandFactory)
    {
        this.CommandFactory = CommandFactory;
    }

    private void ProcessAttack(Entity attacker, Entity target, int damage)
    {
        var cmd = this.CommandFactory.Get<DealDamageCommand>();
        cmd.Initialize(target, damage);
        cmd.Execute(); // dispatches to handlers, then auto-returns to pool
    }
}

// ✅ CORRECT: Command definition
public sealed class DealDamageCommand : ICommand
{
    public Entity Target { get; private set; }
    public int Damage { get; private set; }

    public void Initialize(Entity target, int damage)
    {
        Target = target;
        Damage = damage;
    }

    public void Execute() => CommandFactory.Dispatch(this);
    public void Reset() { Target = default; Damage = 0; }
}

// ✅ CORRECT: Subscribing to commands
public sealed class HealthController : IDisposable
{
    public HealthController(CommandFactory CommandFactory)
    {
        CommandFactory.Subscribe<DealDamageCommand>(this.OnDealDamage);
    }

    private void OnDealDamage(DealDamageCommand cmd) { ... }

    public void Dispose() => CommandFactory.Unsubscribe<DealDamageCommand>(this.OnDealDamage);
}
```

**Universal Rules:**
- ✅ Use Data Controllers (NEVER direct data access)
- ✅ Use UniTask for async operations
- ✅ Unload assets in Dispose
- ✅ `Log.<Context>.D/W/E()` for runtime (no `#if` guards, never in constructors), `Debug.Log` for editor only

## Brief Examples

### 🔴 Code Quality First

```csharp
// ✅ EXCELLENT: Quality rules enforced
#nullable enable

using GameEngine.Utils.Logging;

public sealed class PlayerController
{
    private const int MaxRetries = 3;

    public Player LoadPlayer(string id)
    {
        if (!File.Exists(id))
            throw new FileNotFoundException($"Player file not found: {id}");

        Log.Loading.D($"Loading player {id}"); // auto-stripped in PROD
        return DeserializePlayer(id);
    }
}

#if UNITY_EDITOR
public class EditorHelper
{
    public void Log() => Debug.Log("Editor only"); // Debug.Log OK in editor
}
#endif
```

### 🟡 Modern C# Patterns

```csharp
// ✅ GOOD: Expression bodies
public int Health => this.currentHealth;

// ✅ GOOD: Null-coalescing
var name = playerName ?? "Unknown";

// ✅ GOOD: Pattern matching
if (obj is Player player) player.TakeDamage(10);

// ✅ GOOD: Explicit loop (never LINQ)
var count = 0;
for (var i = 0; i < enemies.Count; i++)
{
    if (enemies[i].IsActive) count++;
}
```

### Unity Architecture (Reflex)

```csharp
public sealed class BattleGroundSceneInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton<InteractController>().AddInterfacesAndSelf();
        builder.AddSingleton<PowerHitController>().NonLazy();
        builder.AddSingleton<BattleCameraMovement>().AddInterfacesAndSelf();
    }
}
```

### Unity Architecture (Command Pattern)

```csharp
public sealed class GameService : IGameStartListener, IDisposable
{
    private readonly CommandFactory CommandFactory;

    public GameService(CommandFactory CommandFactory)
    {
        this.CommandFactory = CommandFactory;
    }

    void IGameStartListener.OnGameStart()
    {
        this.CommandFactory.Subscribe<BattleWonCommand>(this.OnBattleWon);
    }

    private void OnBattleWon(BattleWonCommand cmd) { ... }

    public void Dispose()
    {
        this.CommandFactory.Unsubscribe<BattleWonCommand>(this.OnBattleWon);
    }
}
```

## Code Review Checklist

### Quick Validation (before committing)

**🔴 Code Quality (CHECK FIRST):**
- [ ] Nullable reference types enabled (#nullable enable)
- [ ] All access modifiers correct (private by default)
- [ ] Zero compiler warnings
- [ ] Exceptions thrown for errors (no error logging)
- [ ] `Log.<Context>.D/W/E()` used for runtime (not `Debug.Log`); tag matches system context
- [ ] No `#if` guards around `Log.D()` calls — stripping is automatic in PROD
- [ ] readonly used for non-reassigned fields
- [ ] const used for constants
- [ ] nameof used instead of string literals
- [ ] Using directives in deepest scope
- [ ] No inline comments (self-explanatory code)

**🟡 Modern C# Patterns:**
- [ ] Expression bodies for simple members
- [ ] Null-coalescing operators used
- [ ] Pattern matching for type checks
- [ ] Modern C# features used where appropriate
- [ ] No LINQ — use explicit loops

**🟢 Unity Architecture:**
- [ ] Reflex used correctly (scoped installer, AddInterfacesAndSelf, NonLazy)
- [ ] CommandFactory used for inter-system messaging (no direct event subscriptions)
- [ ] All commands unsubscribed in Dispose
- [ ] Data accessed through Controllers only
- [ ] Game loop participation via listener interfaces (not Update() override)

**🟢 Unity Specifics:**
- [ ] Assets loaded are unloaded in Dispose
- [ ] No Find/GetComponent in Update/runtime loops
- [ ] TryGetComponent used instead of GetComponent + null check
- [ ] Lifecycle methods in correct order

**🔵 Performance:**
- [ ] No allocations in Update/FixedUpdate
- [ ] No LINQ anywhere
- [ ] Commands rented from CommandFactory (never newed)

## Common Mistakes to Avoid

### ❌ DON'T:
1. **Ignore nullable warnings** → Enable #nullable and fix all warnings
2. **Log errors instead of throwing** → Throw exceptions for errors
3. **Use Debug.Log in runtime code** → Use `Log.<Context>.D/W/E()` (Debug.Log is editor only)
4. **Wrap Log.D() in #if guards** → `.D()` is already stripped in PROD via `[Conditional]`, guards are redundant
5. **Log in constructors** → Never log in constructors (keep fast/side-effect free)
6. **Add verbose logs** → Keep only necessary logs
7. **Use the wrong context tag** → Pick the tag that matches the system (`Log.Battle` in combat code, `Log.Loading` in async loading, `Log.Boot` in bootstrap, `Log.World` in world scene)
9. **Skip access modifiers** → Always use least accessible (private default)
10. **Hardcode strings** → Use nameof() for property/parameter names
11. **Leave fields mutable** → Use readonly/const where possible
12. **Use VContainer or Zenject** → Use Reflex
13. **Use SignalBus or MessagePipe** → Use Command pattern + CommandFactory
14. **Access data models directly** → Use Controllers
15. **Forget to unsubscribe commands** → Implement IDisposable
16. **Add inline comments** → Use descriptive names instead
17. **Use LINQ** → Use explicit loops always
18. **New commands manually** → Always rent from CommandFactory pool

### ✅ DO:
1. **Enable nullable reference types** (#nullable enable)
2. **Throw exceptions for errors** (never log errors)
3. **Use `Log.<Context>.D/W/E()` for runtime** — static, no injection, tag matches system context
4. **Let `.D()` strip itself** — no `#if PROD` guards, the `[Conditional]` attribute handles it
5. **Debug.Log for editor only** (#if UNITY_EDITOR)
6. **Use least accessible modifiers** (private by default)
7. **Use descriptive names** (no inline comments)
8. **Use nameof()** for string literals
9. **Use readonly/const** for immutable fields
10. **Use Reflex** for dependency injection (scoped scene installers)
11. **Use CommandFactory + pooled commands** for all inter-system messaging
12. **Use Controllers** for all data access
13. **Always implement IDisposable** and unsubscribe all commands
14. **Participate in game loop via listener interfaces** not Update() overrides
15. **Use explicit loops** (never LINQ)

## Review Severity Levels

### 🔴 Critical (Must Fix)
- **Nullable warnings not fixed** - Enable #nullable and fix all warnings
- **Logging errors instead of throwing** - Use throw, not log for errors
- **Debug.Log in runtime code** - Use `Log.<Context>.D/W/E()` instead (Debug.Log = editor only)
- **`#if` guards around Log.D() calls** - `.D()` is already PROD-stripped via `[Conditional]`, remove the guards
- **Logging in constructors** - Never log in constructors (keep fast/side-effect free)
- **Wrong context tag** - Use the tag that matches the system (`Log.Battle`, `Log.Loading`, `Log.Boot`, `Log.World`)
- **Missing access modifiers** - All members must have explicit modifiers
- **Compiler warnings ignored** - Zero tolerance for warnings
- **Inline comments** - Use descriptive names instead
- Using VContainer, Zenject, or other DI instead of Reflex
- Using SignalBus, MessagePipe, or direct events instead of CommandFactory
- Direct data access (not using Controllers)
- Memory leaks (not unsubscribing commands)
- Missing IDisposable implementation
- Any LINQ usage

### 🟡 Important (Should Fix)
- **Missing readonly/const** - Fields should be readonly/const when possible
- **Hardcoded strings** - Use nameof() for property/parameter names
- **Using directives not in deepest scope** - Method-level using when possible
- **Verbose unnecessary logs** - Keep only necessary logs
- Commands newed manually instead of rented from pool
- Missing IDisposable implementation on command subscribers
- Performance issues in hot paths
- Missing unit tests for business logic
- No XML documentation on public APIs

### 🟢 Nice to Have (Suggestion)
- Could use expression body
- Could use null-conditional
- Could use pattern matching
- Could improve naming
- Could simplify with modern C# features

## Detailed References

### C# Coding Standards
- [Modern C# Features](references/csharp/modern-csharp-features.md) - Expression bodies, null-coalescing, pattern matching, records
- [Quality & Hygiene](references/csharp/quality-hygiene.md) - Nullable types, access modifiers, logging, exceptions
- [Performance Optimizations](references/csharp/performance-optimizations.md) - Unity patterns, allocation prevention

### Unity Architecture
- [Reflex DI](references/unity/reflex-di.md) - Reflex dependency injection, scoped installers, container hierarchy
- [DOTS Architecture](references/unity/dots-architecture.md) - GameLoopSystemGroup, listener interfaces, ECS patterns
- [Command Pattern](references/unity/command-pattern.md) - CommandFactory, pooled commands, subscribe/dispatch
- [Data Controllers](references/unity/data-controllers.md) - Data Controller implementation
- [Integration Patterns](references/unity/integration-patterns.md) - Service/Bridge/Adapter for third-party SDKs
- [UniTask Patterns](references/unity/unitask-patterns.md) - Async/await with UniTask

### Code Review
- [Architecture Review](references/review/architecture-review.md) - Reflex, CommandFactory, Controllers violations
- [C# Quality Review](references/review/csharp-quality.md) - Expression bodies, null handling
- [Unity Specifics Review](references/review/unity-specifics.md) - Component access, lifecycle, cleanup
- [Performance Review](references/review/performance-review.md) - Allocations, hot paths

## Summary

This skill provides comprehensive Unity development standards for BarkingBird Studio:
- **C# Excellence**: Modern, concise, professional C# code — explicit loops, no LINQ
- **Unity Architecture**: Reflex DI + DOTS/ECS listener pattern + CommandFactory pooled commands
- **Code Quality**: Enforced quality, hygiene, and performance rules
- **Code Review**: Complete checklist for pull request reviews

Use the Quick Reference Guide above to navigate to the specific pattern you need.
