# Bust and Build — Design Document

Living design doc, built scope-by-scope through grilling sessions. Vocabulary follows
`CONTEXT.md`. Sources it consolidates: `Claude/Design.md` (vision), `Claude/Emotions.md`
(emotion spec, absorbed here), grilling decisions of 2026-07-09.

## Pillars & constraints

- Physics throw-em-up: Grab/Throw is THE verb; player attention is the real resource.
- Deity fantasy: an unknown lesser deity protecting a small island people.
- Reactive, emotional enemies — emergent stories over scripted encounters.
- Platform: PC primary (mouse). Controller/Steam Deck aspiration — **open problem: making
  controller flick feel mouse-equivalent** (unsolved, not blocking until Full Game).
- Peak scale target: 50–100 Enemy Units on the Battleground at once.
- Team: solo dev, placeholder art until Horizontal Slice demands better.

## Scope ladder

| Scope | Question it answers | Release |
|---|---|---|
| **Prototype** | Is the battle core loop fun on its own? | free itch.io build |
| **Horizontal Slice** | Do all systems/scenes hold together as one game? | itch.io — de-facto Early Access channel |
| **Full Game** | Ship list (1.0) | Steam decision deferred until the core toy has its presentation pass |

All three scopes grilled & designed 2026-07-09.

---

# Prototype — "Battle depth"

**Hypothesis:** the Battleground loop — flick combat + emotional enemies + defense building —
is fun and generates emergent stories, with the City faked behind a UI.

## Exit criteria (story test)

~5 outside players, 2+ sessions each.
**PASS:** most retell a specific emergent moment unprompted AND voluntarily start another run.
**FAIL:** signals written down → iterate inside the test box; no PASS by its close → pivot/re-scope.

### Timebox (grilled 2026-07-12, wayfinder ticket 004)

- **Build box: 2026-07-09 → 2027-01-09** (6 months at ~5–12 h/wk solo). Anchor = design-grilling
  close. Preceded by ~8 months of v0.1.0 groundwork (from 2025-11-18) — recorded so the true
  elapsed cost stays readable, but kept outside the box it guards. **The clock never pauses** —
  slow weeks are priced into the estimate.
- **Expiry = ship-what-exists:** at close the build goes to the ~5 testers as-is; unbuilt
  "New for prototype" items are cut by default (they may re-earn a place in HS). **Shell outranks
  content** whenever the box forces a choice — without a distributable build + checkpoint the
  story test physically can't run.
- **Mid-build checkpoint 2026-10-09:** compare the "New for prototype" column against elapsed
  hours; if under ~half done, pre-cut from the bottom band then. Cuts only — the checkpoint
  cannot add scope.
- **Cut bands** (checkpoint & expiry cut Trim first, then Depth bottom-up; Core never — cutting
  Core invalidates the test):
  - **Core:** emotion writers · telegraphs (emote/tint) · wave assembler + Danger curve ·
    sockets + shop UI + staffing · Heal Rings · core-verb SFX.
  - **Depth:** Digger + tunnel · wall mounting · guard role behaviors (splash, block) ·
    PowerHit finish · water Dunk rule.
  - **Trim (cut first, in order):** repairman AI → Shaman → tar barrel.
- **Test & iterate box: ≤2 months from build-box close** (hard stop ~2027-03-09). PASS ends the
  phase immediately → HS planning, no extra polish round. FAIL → fix → retest, as many rounds as
  fit inside the box. No PASS by the hard stop → the phase ends in a pivot/re-scope session on
  the hypothesis itself. No extensions on either box.

## Cut line

| IN | OUT (deferred) |
|---|---|
| Full emotion system (both factions) | Faith abilities (lightning etc.) |
| PowerHit (charge & release shockwave) | — |
| Specials: Digger + Shaman | City scene, villagers, resources |
| Wall sockets + special buildings + Guards | Free building placement |
| Wall mounting (no upgrade gating) | Offline progression; save polish beyond the Day checkpoint |
| Shell: menus, slow-mo pause, Day checkpoint, core-verb SFX (see › Prototype shell) | Music/ambience beds, resolution list, manual save |
| Tar barrel throwable | Wealth formula (curve instead) |
| Wave assembler + authored Danger curve | Controller input |
| Pearls-only economy | Wood/Stone/Iron/Food/Faith |
| Water Dunk rule (Depth band) | Dunk visuals (HS); wash-ashore pickups (contingency) |

## Session structure

- **Endless run, soft-fail.** Beacon driven to 1 HP → Day force-ends, enemies retreat,
  repair = pearls or free timer (≤30s in proto). Matches existing `IsInvulnerable` →
  `ForceFinishDayCommand` clamp + both repair paths. **Two paths are final** — Design.md's
  third path (villagers + wood&stone) is **cut**, all scopes (grilled 2026-07-12, wayfinder
  ticket 005); timer lengthens at HS, see HS › Saves & offline.
- **Danger = authoredCurve(dayIndex).** Wave assembler is built now; Wealth input deferred.
- **10 authored Days, staged intros:** D1–2 default · D3 +fast · D4 +sturdy · D6 +Shaman ·
  D8 +Digger · D10 everything. Past D10: extrapolate (+15%/Day, all types).
- **Day rhythm:** Day ≈ 5–6 min; spawning spans the first 3–4 min as a **continuous trickle
  with intensity valleys** (breathing room, not discrete pulses). Enemies finished ≠ Day end —
  the sunlit tail stays (in Full Game that's the City's beacon-buff window).
- Between Days: shop, staffing, restock, free full heal of all structures + Beacon + all
  allies (emotions reset to Aware — see › Heal Rings).

## Emotion system

Default state for every Unit: **Aware** (eyes track cursor). Rename `Emotion.Normal` →
`Emotion.Aware` in code. All triggers are **event-edge** (evaluated on damage events,
witness events, timer expiry) — never continuous HP checks.

**Precedence: Angry > Scared > Suspicious > Aware.** While Angry, Scared triggers are ignored;
a new Angry trigger refreshes the timer.

**Anti-cycle latch:** the low-HP Scared trigger fires once; after Scared→Angry conversion it
stays consumed and re-arms only when HP climbs back above the threshold (heals = hysteresis).

### Enemy table

| State | Effects | Entry | Exit |
|---|---|---|---|
| Aware | default | spawn | — |
| Suspicious | slower move & attack | enemy within 20m became Grabbed | 30s → Aware; any Scared/Angry trigger |
| Scared | faster, flees to Base, avoids danger | −2/3 max HP within 5s, OR HP < 10% (any damage source) | reaches Base → escapes (smaller Pearl drop); timer ~12s expires → Angry |
| Angry | ignores danger, faster, hits harder | enemy died within 20m; Scared timer expiry | 5s → Aware |

Notes: Design.md's "survived a flick → Scared" is **superseded** — a gentle toss doesn't
scare; a hard throw scares via the burst rule. Tuning flag: death-witness Angry at 20m makes
dense crowds near-permanently Angry under heavy killing — intended risk/reward (kill away
from the pack), watch radius/duration in tests.

### Ally table

| State | Effects | Entry | Exit |
|---|---|---|---|
| Aware | default | spawn | — |
| Scared | invulnerable, flees to home building, heals to full (Heal Ring) | HP < 10%/2^n (n = non-Scared allies within 10m — **Courage**; floor 1%) | healed → returns to post, Aware; player release at HP ≥ exit knob (~50%) → Aware |
| Angry | hits harder | ally within 10m became Scared | 5s → Aware |

**Allies never die:** lethal damage clamps to 1 HP + force-Scared — overrides Angry
precedence and Courage. Punishment is tempo (guard out for the Day), not loss.

**Telegraphs (prototype bar):** floating emote icon + subtle body tint per state, one system
for both factions (Rimworld-style mood marks). The story test fails if testers can't see the
drama. Debug gizmo text stays dev-only. HS upgrades to eyes + posture animation.

## Defense build (UI shop, between Days)

- **~6 fixed Sockets** on the wall line; **geometry decides the structure** (grilled
  2026-07-13, ticket 010): straight Sockets take a WallSection, corner Sockets take a
  **tower** — one purchase per Socket, no either/or choice (supersedes ticket 002's
  "WallSection OR archer/mage tower"). A tower is a corner section of the wall, treated
  as a WallSection for the most part: destructible, breach-relevant, repairable, **no
  staffing, no Heal Ring** — its difference is a taller Mount (bigger range bonus).
- **Special buildings (tickets 002/010):** **Barracks (melee), Archery Range (archer),
  Mage Academy (mage), Repair Shop (repairman)** stand on **separate fixed spots inside
  the Castle** (count fixed for now) — **no HP, untargetable, unkillable**. Physically a
  normal collider: slight steering obstacle, thrown bodies bounce off, no other gameplay
  rule. They hold ALL staffing: **1–3 staffing slots** each, bought one at a time; 1 slot
  = 1 Guard of the building's class. Spot positions + purchase UX: open (map fog).
  (Supersedes staffable towers — towers are unstaffed Mount posts now.)
- **Mounts = Grab & place** (AssignablePlace seam, ADR-0007): 1 **Mount** per WallSection
  top and per tower top (proto); grab again to unmount. Mounted: invulnerable (enemies hit
  the structure), attack range × a mounted multiplier — **tower Mount > wall Mount**
  (two config knobs; height = sight), melee = allowed-but-no-op + shrug indicator.
- **Mounts persist as deployments (ticket 010):** a mounted Guard starts the next Day
  mounted; after a mid-Day collapse the fallen Guard re-mounts the rebuilt structure on
  its own between Days (spirit of ticket 008's re-man rule, now for Mounts).
- **Structure destroyed under a mounted Guard (ticket 010):** the Guard takes **50% of max
  HP as fall damage (floor 1 HP — never lethal)** and fights on the ground as normal
  (supersedes ticket 008's "no fall damage"). A hard fall usually trips Scared → it flees
  to its home Special building's Heal Ring. No "non-assigned" state exists: a Guard's home
  can't die, so ticket 008's fight-on/Castle-sit-out/re-man block is **fully superseded**.
- **Enemy targeting rule (grilled 2026-07-11):** enemies target WallSections, towers
  (perimeter structures — breaching them is the point), the Beacon, and Units — **never
  Special buildings**, which are unkillable outright (ticket 002 supersedes the earlier
  untargetable-but-killable rule; incidental damage does nothing to them).
- **Breach rule (grilled 2026-07-11, ticket 002):** breach = **the perimeter has a hole**,
  however it appeared. A destroyed tower breaches exactly like a destroyed WallSection
  (`hasBeenBreached` → global re-evaluation, wall scoring off). A Day starting with any
  empty Socket = **breached from tick one** — enemies path through the gap instead of
  battering walls; sealing all Sockets for the first time is a felt progression moment.
  Breach is **one-way per Day**: destroyed structures can't be rebuilt mid-Day, and buying
  a structure into an empty/destroyed Socket is a **hard** purchase (applies at Day end,
  per the Socket-tiers soft/hard split). Day end rebuilds everything free and resets the
  flag.
- **Repair:** repairmen fix damaged (HP > 0) **perimeter** structures only (walls + towers);
  destroyed is gone for the Day, and interior buildings have no HP. Non-combatant,
  flickable. Between Days everything heals free.

## Assignment model (grilled 2026-07-13, wayfinder ticket 010)

**Assignment = which brain drives a Unit.** Staffing a Special building's slot grants the
building's class kit + the **battle brain**; HS City work buildings grant the **civilian
brain** (gathering/special work). The assignment is the Guard's class, home, and flee
target in one. A Mount is a *position*, not an assignment — a mounted Guard still occupies
its home building's staffing slot.

- **No jobless Guards in the prototype.** A slot purchase creates a Guard bound to that
  building; since Special buildings can't die, no transient unassigned state exists either.
  Jobless-as-a-state (and the dismissal verb) arrives at HS with villagers.
- **Reassignment = atomic transfer:** grab a Guard, release it at another Special building
  with an open slot → old slot frees, Guard takes the new building's class (brain swap).
  Release at a full building or anywhere else = normal drop, assignment untouched.
- **HP converts by fraction** (round, min 1): new HP = old HP% × new max. No wound-erasure
  vs flat damage (a dying archer panic-swapped to melee still dies in the same hits), and
  ticket 009's ~50% release-exit knob reads the same fraction before and after.
- **Allowed anytime, incl. mid-Day** — same physical verb, costed by player attention and
  the walk. Grab-off-a-Mount → release at a building = unmount + transfer in one motion.
- **Auto-assignment is dead** (old vision's "or is automatically assigned to an empty
  slot"): jobless HS villagers idle/wander near the main building until grab-placed. It
  would make staffing decisions for the player and undo the HS UI-panel dismissal.
- **HS coexistence:** grab-release = assign/transfer; the building panel's dismiss button
  = explicit detach to jobless (villager walks out, idles). Two verbs, no overlap.

## Heal Rings (grilled 2026-07-12, wayfinder ticket 009)

Every Special building (Barracks, Archery Range, Mage Academy, Repair Shop — ticket 010
recast; towers project **nothing**) projects a **Heal Ring** — a visible ground-decal
radius that slowly heals **any** damaged Ally Unit standing inside it. Universal, not
class-gated: assignment decides where a Scared unit *flees* (its home building), never
where it may heal. The Castle projects nothing. Special buildings can't die, so **rings
are always on** (ticket 009's "ring off while destroyed" clause has no trigger left).

- **Unconditional tick** — heals while fighting too. The perimeter has NO healing: mounted
  Guards are invulnerable but unhealed; ground fighters must run home or be grab-triaged.
  Post-breach "last stand at the keep" beside the interior rings is owned drama. Heal rate
  is the tuning valve if ring-camping degenerates.
- **Scared arc becomes emergent:** flee home → stand in ring → heal to full → Aware,
  return to post (a mounted deployment re-mounts — Mounts persist, ticket 010).
- **Scared-at-home = anchored kiting:** avoid-danger steering anchored to the home
  building — the unit scurries from enemies while staying inside the ring, heal keeps
  ticking (it's invulnerable anyway; kiting is readable cowering, not survival logic).
  No new emotion state.
- **Grab = triage verb:** healing is purely positional — pauses outside any ring, resumes
  inside, never resets. On **release** a Scared ally re-evaluates: HP ≥ the release-exit
  knob (~50%) → Aware, drops invulnerability; below → stays Scared, resumes fleeing home.
  Pull a half-healed guard back to the line early, or ferry a fallen wall Guard straight
  into its home ring instead of letting it limp there.
- **Day end: all allies heal to full + reset to Aware** — tempo punishment stays strictly
  within-Day, and the Day checkpoint payload stays HP-free (ticket 003).
- Allies only — the Shaman is the enemy heal. **Band: Core** — the ally Scared exit is
  unshippable without it. Visual = simple ring decal (PowerHit-telegraph style).
- Playtest watch: the **Scared bowling ball** — a Scared ally is invulnerable, so flicking
  one costs nothing but its heal delay (exists independent of rings; release re-eval
  limits it).

## Guard class roles

| Class | Role |
|---|---|
| Archer | long range, fast, single-target — sustained DPS; Digger/Shaman killer |
| Mage | mid range, slow cast, small AoE splash — punishes clumps (tunnel exits, puddles) |
| Melee | short range, solid DPS, bodily **blocks** enemy movement — a living wall |
| Repairman | non-combat; repairs nearest damaged socket structure |

## Special enemies

### Digger (transport)
1. Stops at long range from target → **5s dig channel** — grabbable/flickable only during
   this window (flick = interrupt; survivor walks back, tries again).
2. Dig complete → Tunnel open; Digger **submerges at entrance**: ungrabbable, killable only
   by Ally Unit damage (later: special powers).
3. Tunnel exit: **always outside the wall perimeter**, just short of the nearest blocking
   Structure toward the Beacon (or short of the Beacon if no wall). Skips the killing field,
   never the wall.
4. **≤2 travelers underground at a time** (traverse ~3s, config); travelers untargetable.
5. Digger dies → collapse → travelers eject at entrance, **stunned ~2s** (combo window).

### Shaman
D2-goblin vibe: single-target heal **cast on cooldown**, heals a **random amount (min–max
config)**; target = lowest-HP Enemy Unit in range (default, config knob). Squishy backline.
Watch: Shaman healing a submerged Digger = sustain fortress combo.

## Throwable: tar barrel

Buy between Days (cheap, stock cap ~3–5); rack near the Beacon. Grab → throw → **shatters on
first hard contact** (no bouncing, light impact damage, no multiplier) → oil puddle ~10s,
big move-speed debuff to **all** ground Units crossing (symmetric — oiling your own gate is a
legitimate mistake). Puddle under a tunnel exit = welcome mat.

## PowerHit (second verb, grilled 2026-07-10)

RMB **hold-to-charge, release-to-detonate**: radius/force grow along `SizeCurve` up to
`Duration` cap (auto-detonate at cap); detonation applies a radial impulse (`Force` × charge%)
to all Grabbable bodies in radius — enemies, allies, barrels, airborne bodies (juggling
emerges). **Zero direct damage** — shoved bodies feed the existing bounce/collision damage
pipeline, keeping damage ownership in one system. Cost = charge time (cursor can't Grab
while charging) + short cooldown (~3–5s). Disabled while Grabbing. Symmetric physics: you
can scatter your own guards, even off walls. Submerged Digger immune (not Grabbable —
consistent). No new emotion triggers — collisions fire the damage-based rules naturally.
Charge telegraph in proto = simple ring decal. `PowerHitController` stub + config already
exist; ships in the **Prototype**.

## Water: the Dunk rule (grilled 2026-07-12, wayfinder ticket 007)

Water is a real gameplay surface (own physics layer — today it's a Ground-layer collider
bodies stand on). Any Unit that lands in the sea — thrown, PowerHit-shoved, or knocked — is
**Dunked** ("Dunk" avoids overloading *submerged* = Digger state, *splash* = guard AoE):

1. **Splash damage** = existing impact pipeline × `WaterImpactMultiplier` (config, guess ~0.4 —
   less than ground). Harder throw = harder splash. Lethal splash kills enemies normally —
   their loot drops at the splash point and **sinks**; allies never die (standard clamp).
2. **Goes under on contact** (no floating/fishing window): out of play — hidden, untargetable,
   ungrabbable, invulnerable, AI off — for `DunkDuration` (config, guess ~2–3s).
3. **Reappears at the enemy spawn point, both factions** — the spawn shore is the only place
   anything can climb out of the sea. HP and emotion persist; no resets, no new emotion
   triggers (event-edge design untouched).

Emergent rules, owned:
- **Dunk a Scared murloc = seal its escape**: it surfaces at its Base, existing escape path
  fires (2s dwell) → despawns with half loot by the **normal escape rule** at the land-side
  boundary (reappear must reset the drop anchor to land, or it drops mid-sea); scatter that
  still lands over water sinks.
- **Dunking your own guard = teleport behind enemy lines** (damage tax + walk-back
  self-limits; allies can't die). Watch in playtests.
- Repeated dunk-chipping is possible but self-limited (victim reappears at the far shore).

**Pickups sink — loot over water is lost** (Pearls return to the sea; "don't dunk the
carriers" tension is intended). If playtests flag the frustration → contingency: partial
wash-ashore over time. Trajectory predictor's impact circle renders on water — aiming a
Dunk is a deliberate verb.

Presentation: proto = body slips under + splash SFX (core-verb SFX are pulled forward);
HS = splash VFX + emergence-foam reappear (rides the HS water stack); 1.0 = seaweed flavor
on dunked murlocs. **Band: Depth** — cuttable at the checkpoint; fallback = today's
walk-on-water (non-broken, just screenshot bait).

## Economy

- **Pearls only.** Flat prices, fixed socket count — pressure comes from the Danger curve.
- Income: kills drop full Pearls; escapes drop reduced (exists).
- Sinks: sockets (walls/buildings), staffing slots, barrels, Beacon repair.

## Prototype shell (grilled 2026-07-12 — wayfinder ticket 003)

**Build:** Windows download on itch.io (DOTS rules out WebGL). Feedback = in-build button on
the main menu → opens an external form URL (`Application.OpenURL`).

**Boot & menus** (near-HS main menu, static blockout bg; replaces the GameManager debug HUD):
- Main menu: **Continue — Day N** · New Run · Options · Feedback · Quit.
- New Run over an existing checkpoint → confirm panel: *"This ends your current run — Day N
  reached."* The confirm **is** the run's end screen — runs only end by voluntary wipe
  (endless soft-fail has no other end). No best-day persistence.
- Pause (lean): Resume · Options · Main Menu. Feedback & quit-to-desktop live on the main menu.
- Options (reachable from both): master volume slider + windowed/fullscreen toggle.
  Resolution list stays at HS.

**Pause = slow-mo backdrop, pulled forward from HS:** world ticks at a tiny timescale under
the menu; gameplay input off (existing `OnPause` unregister pattern) — **not** bullet-time.
Pause mid-grab force-releases through the normal release path with zero throw velocity
(gentle drop, no multiplier); a charging PowerHit cancels. Replaces the interim hard
`GameLoopSystemGroup.Enabled` toggle.

**Between-Days checkpoint (the one save):** single slot behind `ISaveSystem` — the real-disk
slice pulled forward from HS; still no save *polish*. Written twice per cycle: at shop-open
(Day end) and at Day-start (locks shopping in; quit during shop re-does at most the shop).
Payload = day index + Pearls + socket layout + staffing + barrel stock (structures heal free
between Days). Quit or Main-Menu mid-Day = revert to the Day-start checkpoint; quit-scumming
accepted at proto scale. Design.md's "quit during battle = Beacon destroyed" is **explicitly
deferred to HS** (see HS › Saves & offline). No manual save button.

**Audio: core-verb SFX only, pulled forward from HS** — grab, whoosh, border bounce, ground
impact, pearl pickup, beacon (placeholder one-shots). No music/ambience beds, no dynamic
mixing. Rationale: the story test reads game feel; a silent build under-reads the fun.

## Config knobs (all tunable, initial guesses)

Suspicious: witness radius 20m, duration 30s · Scared: burst −2/3 in 5s, low-HP 10%, timer
12s · Angry: death radius 20m, duration 5s (both factions) · Ally Courage: 10%/2^n floor 1%,
radius 10m · Dig channel 5s · travelers ≤2 · traverse 3s · eject stun 2s · Shaman heal
min–max + cooldown + range · puddle 10s · Day 5–6 min, trickle 3–4 min · curve +15%/Day past
D10 · sockets 6 · staffing 1–3 · barrel cap 3–5 · repair free-timer ≤30s · pause slow-mo
timescale · Heal Ring radius ~5m · heal time-to-full ~20s · Scared release-exit ~50% ·
mounted range ×: wall ~1.2, tower ~1.4 (tower > wall) · Mount fall damage 50% max HP
(floor 1).

## Existing vs new (map to TaskBreakdown)

| Exists | New for prototype |
|---|---|
| Emotion enum + Scared readers (flee/escape) | Emotion **writers** (all triggers, timers, latch, precedence) |
| Flick/bounce/land damage pipeline | Wave assembler + Group presets + Danger curve |
| Beacon Day loop + repair paths (2 of 3) | Sockets + shop UI + Special-building staffing/assignment |
| WallSections + breach detection | Mounts on WallSection/tower tops (AssignablePlace) |
| Ally/Enemy profiles, faction-agnostic AttackSystem | Guard classes' role behaviors (splash, block) |
| Variants: default/fast/sturdy | Digger + Tunnel, Shaman |
| Pearl drops + magnet pickup | Tar barrel + puddle |
| `AssignablePlace` settle-check seam | Repairman AI |
| `HealthAspect` damage drain (no heal path) | Heal Rings (positional heal tick + release re-eval) |
| GameManager debug HUD (Start/Pause/Resume) | Shell: menus, slow-mo pause, Day checkpoint, core-verb SFX |

## Code hygiene notes

- Rename `Emotion.Normal` → `Emotion.Aware` (ubiquitous language).
- Rename `TunnelTeleporter` → e.g. `OverlapEjector` — it's a throw-release overlap resolver;
  "Tunnel" is now a domain term owned by the Digger.

## Open questions (parked)

- Controller flick parity (Full Game blocker, not before).
- Shaman↔Digger sustain combo balance.
- Death-witness Angry radius under 50–100-enemy density.
- Scared bowling ball (invulnerable ally as free projectile) + ring-camping sustain —
  watch in playtests (see › Heal Rings).

---

# Horizontal Slice — "One game, all systems"

**Hypothesis:** the couplings hold — food→population→Wealth→Danger, villagers→Guards,
offline absence→recovery, cross-scene continuity. All systems present, fidelity minimal.

## City scene (all four systems in, simplest honest versions)

- **Villager sim:** population capped by food; starvation deaths when food < pop; buy new
  villagers with Pearls. The engine of the Wealth-crash-and-recover loop.
- **Villagers replace pearl slots:** staffing battle buildings assigns real villagers — the
  city-work vs defense trade-off becomes the economy. Prototype's pearl-bought slots retire.
  **Jobless villagers idle/wander near the main building** until grab-placed — auto-assignment
  to empty slots is dead (ticket 010); dismissal (UI panel) is the explicit detach.
- **Altar + Faith + one ability** (lightning): villagers on the altar → Faith income → one
  castable in battle. Third resource loop proven end-to-end.
- **Cursor-reactive villagers:** speech clouds / prayer poses near the cursor, gibberish only.
- **Building placement:** free placement + proximity-to-resource-node validity — the real
  system. Full resource set: Pearls, Food, Wood, Stone, Iron, Faith.
- **Island: handcrafted**, hand-placed nodes. Proc-gen deferred to Full Game (its value is
  replay variety across worlds, not coupling).

### City sim model (grilled 2026-07-10)

- **Income: rate-based for HS** — staffed building yields X/min into the stockpile;
  villager walk-loops are decorative (Visual Companions). Same formula drives the offline
  closed-form calc. **Physical hauling is COMMITTED at 1.0** (user call, retrofit accepted):
  when it lands, the offline formula gains a per-building *efficiency factor* approximating
  haul distance, and HS income rates get re-based. Plan for it; don't discover it.
- **Population, Timberborn-style:** each house = **+2 hireable villagers** (cap). Hire with
  Pearls; no breeding, no aging. Food is upkeep — food < pop → starvation deaths (offline
  floor 3–5 stands). Only starvation kills villagers (battle deaths impossible — allies
  never die).
- **Nodes:** natural resources (wood/stone/iron) = infinite workplace anchors. **Farm crops
  deplete on harvest and regrow**; regrowth rate scales with sunlight — dim default, ×N
  while the Beacon is active (this is Design.md's beacon-buff/offline-dwindle mechanism).
  Beacon-buff income multipliers: farms large · woodcutter slight · stone/iron none (config).

## Wealth → Danger (the real formula)

- **Durables only:** Wealth = Σ weighted(city buildings, battle sockets/staffing, population).
  Liquid currency invisible — savers aren't punished; offline losses lower Danger
  automatically (the recover-after-absence promise).
- Snapshot at Beacon Core insert + after offline calc. Danger = curve(Wealth) ± small
  variety band.

## Saves & offline

- **Single world**, real disk backend behind `ISaveSystem`; auto-save 10 min + manual.
- **Offline progression = one closed-form calculation on login:** elapsed time → resource
  ticks by staffed buildings, food consumption, starvation with the 3–5-villager survival
  floor. A formula, not a simulation.
- Quit during battle = Beacon destroyed (loss state on next launch). Explicitly deferred
  from proto, where quitting mid-Day only reverts to the Day-start checkpoint.
- **Beacon repair free timer lengthens to ~5 min at HS** (config; proto's ≤30s retires) —
  a deliberate breather pushing the player into the City; pearls skip it anytime, incl.
  mid-timer. Ticks in **real time incl. offline** (folds into the closed-form calc — a
  returning player usually finds it elapsed). Design.md's villager+wood&stone repair path
  stays cut (grilled 2026-07-12, wayfinder ticket 005).
- Multi-world creation/switching + menu live background → Full Game.

## Map scene & continuous simulation

- **Mid-Day scene-leaving is a core tension:** the battle runs full-sim, unattended, while
  the player farms the beacon-buff in the City — defenses must hold without the cursor.
  HS must prove this architecture (battle sim active while other scenes are in front) —
  see ADR-0009.
- Map = functional router + live small-scale view. Deity descend/ascend transition FX
  deferred; existing zoom transitions suffice.

## Presentation bar: functional-minimal

- Menus: shipped at proto (see Prototype › Prototype shell); HS adds the resolution list
  to options.
- Audio: three stock/placeholder scene beds (core-verb SFX shipped at proto; destruction
  SFX added here). No dynamic mixing.
- Art: consistent blockout, distinct silhouettes per unit/building.
- **One deliberate exception:** emotion presentation upgrades to cursor-tracking eyes +
  per-state posture — that's readability (a system), not polish.

## UI / interaction language (grilled 2026-07-10)

- **Grab & place is the universal assignment verb** (one hand, one language): villager →
  building = assign · villager → battle building = staff · guard → Mount (wall/tower top)
  = mount · guard → other Special building = transfer (ticket 010) · Core → Beacon = start
  Day · barrel → throw. TaskBreakdown's "throwable supply selector"
  UI is deleted — throwables are physical.
- **Dismissal is UI**: click building → panel with worker slots → dismiss (villager walks
  out, idles). The panel doubles as the building's info surface (rates, staffing).
- **Placement is UI**: build menu → ghost preview → proximity/validity ring → place.
- **HUD: diegetic-first, minimal overlay.** Overlay = resource counters (exist) + Faith bar
  (HS). World carries the rest: sun position = Day clock · Beacon damage states = its HP ·
  PowerHit cooldown = cursor ring · emote telegraphs. **Wealth is never shown** (internal
  director value). Danger has no meter — at most a subtle omen on heavy Days.
- **Contextual cursor**: cursor icon transcribes the available verb — grab (enemy),
  ally-colored grab, default/move, assign-to-building… (existing cursor system grows a
  state machine).
- **Damage-reactive HP bars**: anything damaged shows its bar temporarily, then fades.
- **Hover = detail layer**: wall/tower slots, tooltips, full readouts.

## Audio direction (grilled 2026-07-10)

- **Middleware: FMOD, adopted at the HS→EA boundary**, gated by a 2–3 day trial spike when
  dynamic-music work starts (one battle track + `threat` parameter + one stinger; fallback =
  Unity mixer, knowingly). Until then plain Unity audio + stock beds (HS bar). Do NOT
  hand-roll adaptive layering on Unity's mixer first and migrate later.
- **Dynamic music model** (from Design.md): parameters `dayPhase` (idle/battle/sundown) ·
  `threat` (active enemy count × Danger) · `beaconProximity` (nearest enemy). Stingers:
  Day start, wall breach, Day end. City ambient layer scales with building count.
- **Gibberish speech**: syllable-bank synthesis (Animal-Crossing style), pitched per
  villager; no real language (fiction: the deity can't understand them).

## Exit gates (all three, timeboxed)

1. **Coupling test** — testers play multi-session with real offline gaps and explain the
   loops back unprompted ("I need food to staff the tower", "I lost people so attacks eased").
2. **No dead ends** — no softlocks or unrecoverable death spirals across a week of mixed play.
3. **Perf checkpoint** — peak battle (50–100 enemies) + city sim + cross-scene continuity at
   target FPS; the perf budget (TaskBreakdown §1.6) finally gets written here.

HS build goes to itch.io — the public iteration channel.

---

# Visuals & Tech Art — fake 2D in a 3D world

References: Kingdom Two Crowns (aesthetics, day/night, water) · Octopath (2.5D battle feel) ·
AoE2 (city building look). Pillar: **the lighting is the look.**
Architecture core recorded as **ADR-0010** (Visual Companions · one 3D renderer · cardboard
kingdom).

## Decisions (grilled 2026-07-10)

- **Art style: painted/storybook 2D** (not pixel art) — AI-gen friendly, commissionable later.
- **Animation: skeletal 2D** (Spine or Unity 2D Animation). Consequence accepted: skeletal
  can't render in Entities Graphics → un-defers the Mono **visual companion** layer.
- **AI pipeline: cloud gen + manual cutout** (note: skeletal needs part-segmented art —
  extra cutting/painting per character).
- **Composition: 3D world + sprite units** (Octopath recipe), one recipe for all 3 scenes;
  camera angle differentiates them. Light2D/2D-Renderer path (leftover experiment) dies.
- **World topology (confirmed existing architecture):** ONE world space, composed additively
  from the 3 scene files (World = island + lighting · Battle · City), all loaded together;
  Map/Battle/City are camera **framings**, transitions = Cinemachine camera + post moves.
  Region art is authored for its intended framing (beach side-on, city iso).
- **Render tiers — "cardboard kingdom":** 3D mesh = terrain/ground/sea ONLY (+ invisible
  gameplay colliders). ALL Structures — city buildings, WallSections, towers, Beacon — are
  painted sprite **cards** (footprint pivot, damage states = texture swap; the FBX wall
  retires). Units = pooled **Visual Companion** GOs (SpriteSkin, LateUpdate transform sync
  from ECS; companion owns ALL presentation: flip/lean, juice, emote icons+tint, throw
  tumble). Flat static things (pearls, vegetation, decals) = ECS quads.
- **Lighting: single URP Forward+ renderer.** Custom lit sprite shadergraph: flat diffuse
  (NO normal maps initially — painted art double-shades) + rim/backlight from sun + emissive
  mask. DaylightHandler extends to drive sun **rotation** (long dawn/dusk shadows) + drops
  its Light2D reference.
- **Shadows: cross-quad silhouette casters** (2 perpendicular shadow-only quads, alpha-clipped
  base silhouette — works from any light direction). **Caster budget: sun + ≤2 hero lights**
  per scene (Beacon at night = radial monster shadows). Pearls/bio-plants/torches = shadowless
  glow. Airborne units get a landing blob shadow. Sprites receive shadows flatly.
- **Water: true planar reflection** — mirrored ortho camera → half-res RT, culled set (units,
  Structures, sky, hero lights); ripple-distorted, depth tint, shoreline + emergence foam
  (foam also serves Dunk-rule reappears; Dunk splash VFX lands here — see Prototype › Water).
- **Post (URP Volumes/scene): bloom + dayPercent-blended color grading + per-scene DoF**
  (battle subtle · Map strong tilt-shift "diorama" · city mild) **+ depth haze**. Explicitly
  out: grain, chromatic aberration, vignette.

- **Sprite edges: alpha-clip + MSAA 4x alpha-to-coverage (A2C)** + 1–2px feather baked into
  the art. Opaque queue, real depth writes — sorting, shadows and the water RT just work.
  Fallback toggle to plain clip if MSAA cost ever bites.
- **Map-height LOD:** past a zoom threshold (with hysteresis), Visual Companions return to
  their pool and units render as static ECS quad billboards (base sprite, tinted, no icons).
  Sim untouched (ADR-0009); Design.md itself says map-height units are too small for
  animations.
- **Sequencing:** prototype stays greybox + emote icons. A 1–2 week **look-dev spike** runs
  immediately after the prototype passes its story test — one in-engine shot: greybox
  terrain + a handful of AI sprites + the FULL stack (rotating sun, rim shader, cross-quad
  shadows, planar water, post) at all three framings. It gates companion-system build and
  all production art. Production art sprints after HS exit, toward the EA bar.

## Pipeline conventions (defaults, not grilled)

Author art at ~2× display size · SpriteAtlas per category (skeletal parts atlased per
character) · mips + trilinear (painted, not pixel) · outlines baked into art, never shader ·
skeletal segmentation happens at cutout time (AI art → parts is manual work; budget it).
VFX language: painted flipbook particles (puffs, splashes, impact stars) + shader FX
(ripples, glow, dissolve) — no realistic sims.

## Risks owned

Card-vs-card sorting in dense city overlap · roofline occlusion of units behind buildings
(footprint-pivot discipline) · per-character segmentation cost of the AI→skeletal pipeline ·
companion count at 50–100 units (HS perf gate covers it).

## Parked

- Normal maps on sprites — revisit only if the look-dev spike says flat-lit looks dead.

(Water as a gameplay surface — resolved 2026-07-12, see Prototype › Water: the Dunk rule.)

# Full Game — 1.0

**Release model: itch.io-first Early Access.** Prototype and HS ship on itch.io; the itch
audience is the de-facto EA. The Steam decision (EA vs full launch) is deferred until the
core toy has its presentation pass. Premium; no F2P/IAP — offline progression is cozy
pacing, never a monetization hook.

## Structural decisions

- **Pure endless.** No win state; difficulty and content plateau by design. Long-run variety
  must come from combinatorics (emotions × specials × physics × defense layouts) — this is
  why the roster stays compact but every entry must change battle *shape*.
- **Onboarding: minimal text prompts** — a few contextual one-liners on first encounters
  ("Hold and flick!"), plus diegetic affordances (Beacon glow, villager pointing/prayer).
  Tooltips live in menus/shop UI. No tutorial mode, no guided island.
- **Controller/Steam Deck: gated experiment.** Timeboxed (~2 weeks, late production):
  right-stick velocity flick vs Deck gyro-flick vs trackpad. Pass bar = testers call it
  as good as mouse. Fail = mouse-only ship, Deck unverified, controller revisited
  post-launch. The unsolved input problem does not hold the ship list hostage.

## Content volume (compact roster)

| Category | Target |
|---|---|
| Enemy types | 7–8 total: 3 base + Digger + Shaman + 2–3 new shape-changing specials |
| Group presets | ~40 authored |
| City buildings | ~10 (housing, farm, woodcutter, stonemason, ironsmith, altar, supply…) |
| Battle buildings | 4 Special (Barracks, Archery Range, Mage Academy, Repair Shop) + walls/towers × 3 tiers (module axis door-open) |
| Faith abilities | 3–5 |
| Throwables | 3–4 |

## Socket tiers (grilled 2026-07-11, ships 1.0 — wayfinder ticket 008)

Wood/Stone/Iron sinks for the socket line. Provisional against prototype/HS playtests.

- **3 base tiers** per structure — perimeter (walls, towers) AND interior buildings + a door-open
  **module axis**: a structure carries tier (1–3) + 0..n module slots. NO modules
  authored until post-prototype playtests — shape only, so config doesn't corner us.
- **Walls:** T1 palisade (proto baseline) → T2 reinforced (HP jump) → T3 **battlement** —
  walkable top, the ONLY mountable tier. Mount gating is diegetic: lower tiers simply
  aren't an `AssignablePlace` (no top to stand on — visible in the card, cursor never
  offers assign, no refusal UX). Supersedes proto's ungated mounting at 1.0; Design.md's
  "depends on wall upgrades" is law. Melee-on-wall shrug rule unchanged.
- **Towers (ticket 010 recast — unstaffed Mount posts):** mountable **from T1** (walls stay
  T3-gated); tier raises **HP + height** (taller perch = bigger mounted-range multiplier,
  per-tier knob) and **T3 adds a second Mount**. Supersedes "tier = staffing-slot cap + HP"
  — towers have no staffing. **Special buildings** (Barracks, Archery Range, Mage Academy,
  Repair Shop): tier = slot cap only — no HP, unkillable (tickets 002/010). No mounting on
  building roofs.
- **Costs:** T1 Wood → T2 Wood+Stone → T3 Wood+Stone+**Iron** (peak defense ties to the
  ironsmith chain). Values = tuning.
- **Purchase: anytime, incl. mid-Day**, via the structure's click-panel. **Soft** effects
  (HP, slot cap) apply instantly — mid-Day HP bump heals by the added max
  (`current += Δmax`; panic button by design). **Hard** upgrades (shape/capability:
  T2→T3 battlement, a tower's T3 second Mount, modules) queue and apply when the battle
  ends. Queued hard upgrade +
  destruction both resolve at Day end — rebuild lands already-upgraded.
- **Destruction: rebuilds free at same tier** between Days ("heals free" covers
  destruction); tier investment is permanent per socket. Finite-sink trade-off owned —
  modules are the future sink headroom.
- **Wealth:** tiers & slots count as durables → upgrading raises Danger. Feeds the director.

## Deferred-to-1.0 systems

Proc-gen island (once per new world) · multi-world + switcher + main-menu live background ·
deity descend/ascend transition FX · Beacon repair-crew flavor anim (assigned battle units
visibly fix it during the timer — ticket 005) · seaweed flavor on dunked murlocs (ticket 007) ·
dynamic music (danger/proximity layers) + city ambient
scaling + gibberish vocalizations · socket tiers (designed — see block above) · full
art/anim/VFX sets · localization · telemetry for balance · offline edge cases (1 day / 1 week /
1 month) · Steam page & marketing beats.

**1.0 =** compact roster complete + proc-gen + multi-world + controller verdict +
localization + the endless plateau tuned.
