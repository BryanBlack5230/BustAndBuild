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

## Serialize Child-Component Refs on a Prefab — Don't `GetComponent` Per-Spawn

When a prefab is `Instantiate`d on a hot path (per pickup, per projectile, per VFX) and you need a child component on the clone, **do not** `GetComponent`/`GetComponentInChildren` on every instance. Put a small root component on the prefab that holds the child as a `[SerializeField]`, wire it once in the inspector, and read the field after Instantiate — Unity remaps internal prefab references to the clone automatically, so the reference already points at the cloned child. Zero runtime lookups.
```csharp
[RequireComponent(typeof(RectTransform))]
public sealed class FlyingPickup : MonoBehaviour
{
    [SerializeField] private Image _icon;            // drag the child Image in the inspector
    private RectTransform _rectTransform;
    public RectTransform RectTransform => _rectTransform;
    private void Awake() => _rectTransform = (RectTransform)transform;   // Awake runs *during* Instantiate
    public void SetIcon(Sprite s) { if (_icon != null && s != null) _icon.sprite = s; }
}
// caller: var f = Instantiate(_flyingPrefab, _root); f.RectTransform.position = p; f.SetIcon(icon);
```
**Why it matters:** `PickupMagnetController` originally did `GetComponentInChildren<Image>()` on every collected pickup (a very frequent event). Replacing it with the serialized-ref `FlyingPickup` component removed the per-spawn reflection walk. Type the prefab field as the component (`FlyingPickup`, not `RectTransform`/`GameObject`) so `Instantiate` returns it directly and the access is statically typed. Cache `transform`-derived values (like the `RectTransform` cast) in `Awake`, which runs synchronously inside `Instantiate` for an active object.
