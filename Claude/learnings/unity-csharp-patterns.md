# Unity C# Patterns

Project-specific C# patterns for Unity 6 that go beyond the general coding standards.

## #nullable enable — Unity Fake-Null Check

With `#nullable enable`, comparing a non-nullable `UnityEngine.Object`-derived field to `null` raises a warning ("expression is always non-null"). But Unity's lifecycle can destroy objects before `Dispose` runs, so a raw check is still needed.  
**Finding:** Use Unity's implicit `bool` operator instead of `!= null` — it performs the fake-null check without triggering the nullable analyzer:
```csharp
// ✅ Correct — uses operator bool, no nullable warning, catches Unity-destroyed objects
if (_trajectoryLine) Object.Destroy(_trajectoryLine.gameObject);

// ❌ Wrong — CS8073 warning on non-nullable field
if (_trajectoryLine != null) Object.Destroy(_trajectoryLine.gameObject);
```
**Why it matters:** `Dispose` is called during scene unload; by that point Unity may have already destroyed the GameObjects. The bool operator is the idiomatic Unity-safe solution.

## ScriptableObject Settings Pattern

ScriptableObjects used as settings blobs should use private `[SerializeField]` fields with public expression-body properties. Required prefab/asset references should throw on null access rather than silently returning null.
```csharp
#nullable enable

[CreateAssetMenu(...)]
public sealed class FooSettings : ScriptableObject
{
    [SerializeField] private int _simSteps = 100;
    [SerializeField] private LineRenderer? _prefab;

    public int SimSteps => _simSteps;
    public LineRenderer Prefab => _prefab
        ?? throw new InvalidOperationException($"{nameof(Prefab)} is not assigned.");
}
```
**Why it matters:** Public mutable fields on ScriptableObjects allow external mutation and give no null-safety at the point of use. The throw surfaces misconfigured inspector assignments at construction time rather than deep in simulation.

## Reflex Installer — Required SerializeField Guard

If a `[SerializeField]` field is required for DI binding, do not fall back to `Resources.Load` silently. Throw immediately so the misconfiguration is obvious at scene load.
```csharp
// ✅ Correct
if (_settings == null)
    throw new InvalidOperationException($"{nameof(_settings)} is not assigned in the inspector.");
builder.AddSingleton(_settings, typeof(FooSettings));

// ❌ Wrong — silent fallback masks inspector misconfiguration
if (_settings == null)
    _settings = Resources.Load<FooSettings>("FooSettings");
```
**Why it matters:** Silent fallbacks make inspector setup optional when it should be mandatory, leading to hard-to-diagnose runtime failures.
