Legend: `[x]` = Done | `[ ]` = Not done

---

## 1. Core Architecture & Tech Foundation

- [ ] Define scene management framework (Map / Battleground / City) with shared world-state singleton
- [x] Build input system: LMB click, hold, drag, release-velocity capture for flick mechanic
- [ ] Implement world-state model: single source of truth for population, resources, buildings, time
- [ ] Set up event/messaging bus between scenes (city changes affect battleground Wealth, etc.)
- [ ] Define data schemas: building defs, monster defs, ally defs, resource defs (data-driven via JSON)
- [ ] Performance budget per scene (target frame rate, draw call ceiling, monster count caps)

---

## 2. Camera & Scene Transitions

- [x] Implement orthogonal 2.5D camera for Battleground (Octopath-style reference)
- [x] Implement tilted bird's-eye camera for Map scene
- [ ] Implement 2.5D isometric camera for City scene
- [x] Build pan-with-LMB-drag in Battleground and City
- [x] Build zoom-out → Map / zoom-in → Battleground or City scene transition flow
- [ ] Design and implement "deity descending/ascending" transition: camera dolly, post-FX, audio crossfade
- [x] Define edge-of-screen behavior in Battleground (bounce-damage boundary)

---

## 3. Battleground Scene — Combat Loop

- [ ] Beacon entity: interactable, has HP, triggers wave start, destruction state, three repair paths (instant pearls / villagers + wood&stone / 5-min auto)
- [x] Flick mechanic: cursor-grab on monster, hold visualization, release velocity → projectile
- [x] Airborne physics (2D): screen-border bounces, escalating damage multiplier per bounce, mid-air monster-vs-monster collisions
- [x] Ground physics (3D for logic, 2D for visuals): walking pathfinding, collision with falling monsters, knockback
- [x] Damage resolution: ground-impact damage = f(multiplier × velocity)
- [ ] Pearl drops: spawn on monster death, magnetize-to-cursor pickup radius
- [ ] Wall/castle construction system: wall segments, placement, HP, destruction
- [ ] Special buildings on the wall: melee barracks, archer tower, mage tower, repair shop — each accepts 1–3 villagers
- [ ] Villager-to-warrior conversion logic (assign villager → produces guard of building's type)
- [ ] Wall mounting: place ranged allies on walls, fall-down behavior on segment destruction
- [ ] Friendly-fire flick: allow flicking allies, deal damage without multiplier
- [ ] End-of-wave cleanup: surviving monsters retreat, beacon resets

---

## 4. City Scene — Building & Economy

- [ ] Procedural island generation: terrain, resource node placement, water/beach boundaries (one-time per world)
- [ ] Resource model: pearls, wood, stone, iron, food, faith
- [ ] Building catalog: housing, farm, woodcutter, stonemason, ironsmith, altar, supply specialty buildings
- [ ] Building placement system: free placement + proximity check to resource nodes for gatherers
- [ ] Villager population: spawn-on-demand (costs pearls), capped by food, starvation deaths when food < pop
- [ ] Villager assignment UI/logic: assign to buildings (city work) or carry over to Battleground (defense)
- [ ] Beacon-buff: brighter sun increases vegetation/food growth rate during active battle
- [ ] Sunlight model: dim default growth, multiplied while beacon active
- [ ] Altar → faith generation rate based on assigned villagers
- [ ] Specialty supply buildings: produce throwables (tar barrels for oil puddles, others TBD)
- [ ] Cursor-reactive villager behaviors: speech clouds, prayer poses, pointing

---

## 5. Map Scene

- [?] Render island top-down with all entities at small scale (no detailed animations)
- [ ] Show live simulation: villagers moving, monsters moving during active battle
- [x] Implement zoom-target picking (which scene to dive into based on cursor position/click)
- [ ] Background-of-main-menu version of this scene (last-played world)

---

## 6. Enemy Design & AI

- [x] State machine: Aware (eyes-track-cursor), Suspicious, Scared (timed → Angry on expire), Angry (timed → Aware)
- [ ] State-transition triggers: witnessing nearby grab, surviving a flick, ally HP-low witness, etc.
- [ ] Pathfinding from sea spawn → beacon
- [ ] Sea-spawn system with staggered emergence
- [x] Variants: default, fast/thin, slow/sturdy
- [ ] Specials: transport monster (creates underground tunnel pathing for allies), shaman (heals other monsters)
- [ ] Tunnel mechanic: AI decision to use tunnel, tunnel lifetime, anti-cheese rules
- [x] Retreat behavior: Scared monster runs home, leaves smaller pearl drop
- [ ] Wave assembler: input Wealth value → assembles preset groups summing ≈ Danger value
- [ ] Wave preset library (designer-authored)

---

## 7. Ally Design & AI

- [ ] State machine: Aware, Scared (retreat-and-heal, invulnerable while retreating), Angry (short, after witnessing ally Scared)
- [ ] Per-class behavior: melee, archer, mage, repairman
- [ ] Range-based attack logic; melee on wall = no-op (UI should warn)
- [ ] Repair shop workers: target nearest damaged wall segment, non-combatant
- [ ] Heal-up at home building → return to post

---

## 8. Time, Save, & Offline Progression

- [ ] Save system: one save per world, multiple worlds, world-switcher in main menu
- [ ] Auto-save every 10 min + manual save from pause menu
- [ ] On-quit-during-battle = beacon destroyed (loss state on next launch)
- [x] Day/night cycle: bright during active beacon, dim dawn/dusk default, deeper night when long-absent
- [ ] Offline simulation: tick resource generation by working buildings, food consumption, starvation deaths
- [ ] Wealth recalculation on login

---

## 9. UI/UX & Menus

- [ ] Main menu: Continue / New Game / Options / Exit, with Map scene of last world as live background
- [ ] World creation & switching flow
- [ ] Pause menu: slow-mo world tick, Continue / Options / Main Menu / Save / Quit
- [ ] HUD: resources, faith bar, Wealth indicator (debug or visible — design call), wave progress
- [ ] Building placement UI (city)
- [ ] Villager assignment UI (drag-drop or click-assign)
- [ ] Wall construction UI (battleground prep mode)
- [ ] Faith ability hotbar (lightning, etc.)
- [ ] Throwable supply selector
- [ ] Speech-cloud rendering system
- [ ] Options menu: audio, video, controls, language

---

## 10. Audio & Music

- [ ] Three score beds: Map (lobby-serene), City (medieval calm + ambient bustle layer), Battleground (calm pre-battle, active during)
- [ ] Dynamic music system: parameter-driven layer mixing (danger level, distance-to-beacon)
- [ ] City ambient layer scaling with building count/type
- [ ] SFX library: monster sounds, ally combat, flick whoosh, screen-border bounce, ground impact, pearl pickup, beacon hum/destruction
- [ ] Villager prayer/speech vocalizations (gibberish)
- [ ] UI SFX

---

## 11. Art, Animation & VFX

- [ ] Character art: villager variants, all monster variants, ally classes
- [ ] Building art: city set, wall set (with upgrade tiers)
- [ ] Environment art: island terrain, water/beach, vegetation states
- [ ] Animation: idle / walk / attack / hurt / scared / angry / death for all units
- [ ] Cursor-tracking eye animation system (monsters and villagers in Aware)
- [ ] VFX: beacon activation, lightning ability, oil puddle, flick trail, bounce flash, ground impact dust, pearl shimmer
- [ ] Scene-transition VFX
- [ ] Day/night lighting passes per scene

---

## 12. Game Design — Balancing & Tuning

- [ ] Wealth formula (sum buildings + upgrades + population + faith reserves, weights TBD)
- [ ] Danger formula and wave preset values
- [ ] Economy curves: pearl drop rates, building costs, villager cost, upgrade costs
- [ ] Offline progression rates (resource gain, food decay)
- [ ] Faith generation rate and ability costs
- [x] Multiplier curve for bounce damage (linear vs exponential)
- [ ] Monster HP/damage/speed per variant
- [ ] Ally HP/damage/range per class

---

## 13. QA, Production & Live Ops

- [ ] Test plan per scene + integration tests for scene transitions
- [ ] Save/load corruption testing
- [ ] Offline-progression edge cases (1 day, 1 week, 1 month away)
- [ ] Performance testing on min-spec
- [ ] Localization prep (string externalization)
- [ ] Onboarding/tutorial design (currently undefined in spec — open question)
- [ ] Telemetry hooks for balance tuning
