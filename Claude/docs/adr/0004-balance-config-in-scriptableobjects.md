# Balance config lives in ScriptableObjects (ConfigHub), not JSON

**Status:** accepted

Balance/AI tuning is authored in ScriptableObject profiles plus flat config groups on a single
Bootstrap-scene `ConfigHub`; the former JSON pipeline (`ConfigContainer`, `Config.json`,
`ConfigGenerator`, `[BlobConfig]`) was deleted entirely. **Delivery is chosen per value by whether
it is mutated at runtime:** a *runtime blob / ECS singleton* (baked by `BlobContainer.Initialize()`
at bootstrap, live-`Rebake`-able) for values systems read each frame, versus *bake-time* (the
Baker reads the SO into the prefab; `DependsOn(so)` re-bakes on edit) for once-stamped-then-mutated
unit/structure stats — a live Rebake adds nothing to a wounded unit.

## Considered options

- **Keep JSON for hot-editability** → rejected: SOs give typed, inspector-authored, Odin-decorated
  config with no (de)serialization layer, and `Rebake` covers live editing for the values that
  actually benefit.
- **One uniform delivery (all-blob or all-bake)** → rejected: blob-baking once-mutated stats is
  wasted plumbing, and bake-timing per-frame tunables would block live tuning.

## Consequences

- Bakers run at subscene-bake time with **no DI / runtime-singleton access**, so bake-time delivery
  is mandatory for authoring stats regardless of preference.
- Systems read flat singletons as `TryGetSingleton<T>(out c) ? c : T.Default`, where `Default` is
  behavior-preserving — so systems still run in scenes that never went through bootstrap.
- The full system map (profile SOs, enum-slot baking contract, every migrated surface) lives in
  `Claude/learnings/config-system.md`.

## Scope widened (2026-07-11)

The blessing covers **all designer-authored definition assets**, not just balance tuning — the
prototype's upcoming asset kinds (socket modules, Groups, throwables like the tar barrel,
Digger/Shaman profiles) are ScriptableObjects too. JSON was re-weighed against modding, external
balancing tools, and diff-friendly review and rejected on those axes as well: none appear anywhere
in the design docs, so if modding ever becomes a 1.0+ goal it is a fresh decision then, not a hedge
carried now. The JSON-era word "def" is retired with it — a definition asset is a **Profile**
(per-type stat/identity block), **Config** (flat tunables group), or **Group** (squad preset).
