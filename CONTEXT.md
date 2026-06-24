# Bust and Build — Glossary

The ubiquitous language for this project. **Meanings only** — what a term *is*, not how it
works (how-it-works lives in `Claude/learnings/`; decisions in `Claude/docs/adr/`).

Bust and Build is a physics-based "throw-em-up" by BarkingBird Studio: the player grabs Units
and objects with the cursor and flings them with mouse velocity to damage enemies, across a
two-phase Battle/City loop.

## Core

**Faction**:
The Ally/Enemy axis every Unit and Base carries (`Faction` enum).
_Avoid_: team, side.

**Unit**:
A single combatant spawned into a Battle and driven by the AI pipeline (brain → steering →
mover). Carries a Faction. Qualify as "Ally Unit" / "Enemy Unit" when it matters.
_Avoid_: enemy (overloaded — reserve for the opposing-side sense), mob, agent, NPC.

**Base**:
A faction's home **region** (AllyBaseBounds / EnemyBaseBounds AABB) that Units spawn from and
retreat to. A region, not a building — distinct from a Structure.
_Avoid_: spawn, nest, home.

## Structures

**Structure**:
A large, attackable building (Castle, Beacon, WallSection; barracks planned). Marked by the
`Structure` component; targeting measures distance to its **surface** (`TargetBounds`), not its
transform pivot.
_Avoid_: building, prop.

**Castle**:
A breachable Structure guarded by WallSections; flips to `hasBeenBreached` once its protecting
wall is destroyed.
_Avoid_: keep, fortress, base (a Base is a region, not the Castle).

**WallSection**:
One segment of the wall shielding a Castle; destroying the section breaches the Castle.
_Avoid_: wall, barrier (the wall is many WallSections).

**Beacon**:
A high-health Structure scored as its **own** targeting category (`WeightBeacon`), separate from
Units and walls.
_Avoid_: tower, totem, objective.

## Combat & AI state

**ActionState**:
A Unit's current behavior — Stunned, Moving, Attacking, or Evading — decided by the brain.
_Avoid_: mode, status.

**EmotionalState**:
A Unit's disposition (currently only Normal; Scared is planned, no writer yet). Drives
flee/escape behavior.
_Avoid_: mood; AI state (that is ActionState).

**Grab / Throw**:
The two player verbs — Grab a Unit/object with the cursor, Throw it with mouse velocity.
_Avoid_: pick up, drag (drag is the camera gesture, not Grab).

**Bounce / Land**:
Post-throw collision outcomes that deal damage — a thrown body Bounces off screen edges and
targets, then Lands.
_Avoid_: hit, impact (generic).

## Economy & loop

**Pearl**:
The primary currency (`CurrencyType.Pearls`, index 0); dropped by defeated Enemy Units and
collected as Pickups. Other currencies: Food, Wood, Stone, Iron, Faith.
_Avoid_: coin, gold, gem.

**Pickup**:
A spawned collectible that grants currency when gathered (magnet-flies to the counter).
_Avoid_: loot; drop (a "drop" is the roll that spawns a Pickup).

**Battle / City**:
The two macro phases — Battle (Battleground combat) and City (build/economy).
_Avoid_: level, stage, round.
