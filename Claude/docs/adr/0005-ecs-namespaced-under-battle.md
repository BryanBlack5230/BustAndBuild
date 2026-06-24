# ECS battle-sim code is namespaced under Gameplay.Battle, not the global namespace

**Status:** accepted — convention governs new ECS files now; existing files not yet migrated

All battle-simulation ECS types leave the global/root namespace for
`BarkingBird.Runtime.Gameplay.Battle`: the **un-foldered** `Components/` + `Systems/` root holds the
**shared core** — homeless cross-domain primitives and shared contracts (`Health`/`HealthAspect`,
`Faction`, the Battle-wide singletons `BattleCoordinator`/`FactionBases`, `BattleCenter`,
`TargetBounds`, `HitFeedbackBuffer`, plus `GizmoDrawSystem`) — and each cohesive feature lives in a
**nested** sub-namespace mirrored by a subfolder: `Battle.{Combat, Throwing, Structures, Economy,
Feedback, Spawning, Director, Bridge, AI}` (this is the existing `Infrastructure/` pattern — root
files = the bare namespace, subfolders = sub-namespaces). Nesting features *under* the core is the
crux: a nested namespace sees its enclosing namespace with **no `using`**, so the high-frequency
feature→core reads stay import-free — preserving exactly the zero-import ergonomics that the old
global namespace bought, while removing the consistency wart of an un-namespaced island in an
otherwise `BarkingBird.Runtime.*` codebase.

**Placement keys on ownership/intent, never readership.** A component lives in the one feature that
conceptually *owns* it (`WallSection` → `Structures`, even though AI targeting reads it). A system
lives in the feature of its primary *intent*, not what it touches (`DamagePushSystem` → `Feedback`
as a hit-reaction, not Physics; `PickupSpawnOnDeathSystem` → `Economy`, not Combat). The core is the
narrow exception: types with no single home, or written-by-many / read-by-many *contracts*. Being
read across a feature boundary is paid with an honest `using` — it documents a real dependency
(`AI` → `using …Battle.Structures`) — and never promotes a type to core.

## Considered options

- **Keep ECS in the global namespace** (status quo; the rationale in
  `learnings/project-overview.md` → "ECS root-namespace convention") → rejected: global bought only
  short unqualified names + zero-import reach across the dense core, and nesting under
  `Gameplay.Battle` keeps *both* (enclosing-namespace visibility) while ending the lone
  un-namespaced island.
- **One flat `Gameplay.Battle` bucket, no features** → rejected: satisfies consistency but buys no
  findability; nested features cost little more (only cross-feature `using`s) and answer "where does
  Throwing / Structures / Economy live."
- **A named `Battle.General` leaf for the shared atoms** → rejected: as a *sibling* of the feature
  namespaces it forces `using …Battle.General` on the busiest (feature→core) edges; making the
  shared core the *parent* namespace instead makes those reads free.
- **Per-kind namespaces (`Components.*` / `Systems.*`)** → not adopted: namespace tracks *domain*,
  not data-vs-behaviour — `Components/X` and `Systems/X` share the one `Battle.X` namespace, exactly
  as `AI/` already does.

## Consequences

- **Acyclic by construction.** Core depends on no feature; features depend *down* onto core. When
  two features look mutually dependent, the thing between them is a *contract* and belongs in core —
  e.g. `HitFeedbackBuffer` (written by `Combat`'s `AttackSystem` and `Throwing`'s
  `ScreenBounce`/`InAirCollision`, read by `Feedback`'s dispatch) sits in core, so all four reference
  only the parent and no cross-feature cycle forms. Namespaces are not compilation units anyway (one
  `Runtime.asmdef` → one DLL), so there is **no build-level cycle risk** regardless; the layering
  rule is for human clarity, not the compiler.
- **`Gameplay.AI` becomes `Gameplay.Battle.AI`** (nested), so AI's heavy core reads (`Faction`,
  `TargetBounds`, the coordinator singletons) stay import-free; on migration the ~13 current
  `using …Gameplay.AI` importers switch to `…Gameplay.Battle.AI`.
- **Decided, not yet migrated.** Files and `.meta` are unmoved as of this ADR. The convention
  governs *new* ECS files immediately; existing root files stay global until a migration pass moves
  them into the feature subfolders. `learnings/project-overview.md`'s "ECS root-namespace
  convention" therefore describes the **legacy** state, superseded here.
- Migration is mechanical but wide: every moved file needs its namespace set + subfolder move (with
  its `.meta`), and any out-of-feature reference gains a `using`. The compiler catches every miss
  loudly, so risk is low; system update-order is keyed on system *types*, unaffected by namespace.
