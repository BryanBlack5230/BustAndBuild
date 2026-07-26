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

**Guard**:
An Ally Unit fielded by staffing a Special building. Classes: melee (blocker), archer (sniper),
mage (splash), repairman (non-combatant).
_Avoid_: soldier, defender, warrior.

**Assignment**:
The binding of a Unit to a Special building's staffing slot — it grants the building's class,
brain, and flee-home in one. Changed by grab & place (atomic transfer to another building with
an open slot; HP carries over by fraction) or, at HS, by UI dismissal.
_Avoid_: job, employment, recruiting.

## Structures

**Structure**:
A large, attackable building (Castle, Beacon, WallSection, tower). Marked by the
`Structure` component; targeting measures distance to its **surface** (`TargetBounds`), not its
transform pivot.
_Avoid_: building, prop.

**Castle**:
A breachable Structure guarded by WallSections; flips to `hasBeenBreached` once its perimeter
has a hole — a destroyed WallSection/tower, or a Socket left empty at Day start.
_Avoid_: keep, fortress, base (a Base is a region, not the Castle).

**WallSection**:
One segment of the wall shielding a Castle; destroying the section breaches the Castle.
_Avoid_: wall, barrier (the wall is many WallSections).

**Tower**:
The corner section of the wall line — a WallSection in every rule (destructible,
breach-relevant, repairable, unstaffed) except its taller Mount (bigger range bonus;
at 1.0, tier raises height and T3 adds a second Mount).
_Avoid_: archer tower, mage tower (towers are classless), turret.

**Mount**:
One mountable spot atop a WallSection or tower, taken by grab & place (one per top; grab
again to unmount). A mounted Guard is invulnerable — enemies hit the structure — and keeps
its Assignment; a Mount is a position, not a job.
_Avoid_: socket, slot, perch, post.

**Beacon**:
A high-health Structure scored as its **own** targeting category (`WeightBeacon`), separate from
Units and walls. Activated by inserting its Beacon Core; reaching invulnerability ends the Day.
_Avoid_: tower, totem, objective.

**Beacon Core**:
The grabbable sphere that activates the Beacon: gently placed into the Beacon it starts the Day,
and it drops back out when the Day ends. One per Beacon.
_Avoid_: orb, crystal, battery.

**Socket**:
A fixed slot on the wall line; geometry decides its structure — straight Sockets take a
WallSection, corner Sockets a Tower. An empty Socket is a hole in the perimeter.
_Avoid_: plot, cell, slot (staffing slots are a different thing).

**Special building**:
One of the four unkillable class buildings on fixed spots inside the Castle — Barracks
(melee), Archery Range (archer), Mage Academy (mage), Repair Shop (repairman). Holds all
staffing: 1–3 slots, each yielding one Guard of its class; projects a Heal Ring. No HP,
untargetable; a minor pathing obstacle only. Also called Interior building.
_Avoid_: tower (unstaffed Mount posts, not Special), guard house.

**Interior building**:
Synonym for Special building — all four stand inside the Castle, none in a Socket.
_Avoid_: passive building, support building.

**Heal Ring**:
The visible ground radius every Special building projects, slowly healing any damaged Ally
Unit standing inside it — universal, not class-gated (assignment decides where a Scared unit
flees, not where it may heal). Always on — Special buildings can't be destroyed.
_Avoid_: aura, healing area, regen zone.

## Combat & AI state

**Brain**:
The decision layer of the AI pipeline (brain → steering → mover) that picks a Unit's
ActionState. Assignment decides which brain a Unit runs: Special buildings grant the
battle brain (+ class kit); HS City work buildings grant the civilian brain.
_Avoid_: AI (the whole pipeline), controller.

**ActionState**:
A Unit's current behavior — Stunned, Moving, Attacking, or Evading — decided by the brain.
_Avoid_: mode, status.

**EmotionalState**:
A Unit's disposition, modifying how the brain weighs decisions. Values: **Aware** (default —
calm, eyes track the cursor), **Suspicious** (Enemy-only — cautious after witnessing a Grab),
**Scared** (flees toward its Base/home), **Angry** (enraged — faster, hits harder). Code's
`Emotion.Aware` matches the design term.
_Avoid_: mood, Normal (say Aware); AI state (that is ActionState).

**Grab / Throw**:
The two player verbs — Grab a Unit/object with the cursor, Throw it with mouse velocity.
_Avoid_: pick up, drag (drag is the camera gesture, not Grab).

**PowerHit**:
The player's second verb — a charged radial shockwave at the cursor that shoves Grabbable
bodies; deals no damage itself (collisions do).
_Avoid_: slam, shockwave (generic), push, AoE.

**Bounce / Land**:
Post-throw collision outcomes that deal damage — a thrown body Bounces off screen edges and
targets, then Lands.
_Avoid_: hit, impact (generic).

**Dunk**:
What happens to a Unit that lands in the sea — reduced splash damage, goes under, reappears
at the enemy spawn shore (both factions; HP and emotion persist). Pickups over water sink.
_Avoid_: drown (Dunked Units don't die of it), submerge (that's the Digger's state),
splash (that's the guard AoE).

**Courage**:
Nearby non-Scared allies lowering an Ally Unit's Scared threshold — company makes them braver.
_Avoid_: morale, bravery.

## Enemy roles

**Digger**:
The transport-class Enemy Unit that digs a Tunnel and anchors it, submerged at the entrance.
_Avoid_: transport monster, tunneler.

**Shaman**:
A support Enemy Unit that heals other Enemy Units via a cooldown-based single-target cast.
_Avoid_: healer, priest.

**Tunnel**:
The Digger's underground passage ferrying Enemy Units past the killing field; collapses when
its Digger dies.
_Avoid_: burrow, tube.

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

**Framing**:
One of the three camera views onto the single world space — Battle (side-on), City (iso),
Map (bird) — switched by pure camera+post transitions, never scene loads.
_Avoid_: scene (the additive .unity files are an editing convenience, not separate worlds).

**Visual Companion**:
The pooled GameObject that renders a Unit's presentation (skeletal sprite, juice, emote
icons); synced from the Unit's ECS entity. The sim never reads it.
_Avoid_: view, avatar, puppet.

**Wealth**:
The internal score of the player's holdings (defenses + city development) that sets how hard
the world hits back.
_Avoid_: score, power level.

**Danger**:
The difficulty budget a Day's assault is assembled to match — derived from Wealth in the full
game, from an authored curve in the prototype.
_Avoid_: difficulty, threat level.

**Group**:
A designer-authored squad preset of Enemy Units with a Danger cost; a Day's assault is
assembled from Groups summing ≈ the Day's Danger.
_Avoid_: wave, pack, squad.

**Day**:
The active combat window inside a Battle: begins when the Beacon Core is inserted and ends when the
Beacon is driven to invulnerability or the day timer expires. Between Days the cycle sits Idle (no
Enemy spawns). One Day per Beacon Core insertion.
_Avoid_: wave, round, night (night is just the Day's sundown tail).
