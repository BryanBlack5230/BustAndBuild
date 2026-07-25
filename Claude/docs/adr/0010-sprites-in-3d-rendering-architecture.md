# Sprites-in-3D rendering: Visual Companions, one 3D renderer, cardboard kingdom

## Context

The game fakes 2D in a 3D world (visual targets: Kingdom Two Crowns, Octopath, AoE2; pillar:
*the lighting is the look*). Gameplay is already 3D (Unity Physics, pathing, AABB placement)
and simulated in DOTS — but the chosen art path is painted 2D (solo dev, AI-generated art,
skeletal 2D animation), and **Entities Graphics cannot render SpriteRenderer/SpriteSkin**.
Grilled 2026-07-10; full decision detail in `Claude/DesignDoc.md` § Visuals & Tech Art.

## Decision

- **Hybrid sim/presentation split.** ECS entities own the sim; each Unit's visuals live on a
  pooled **Visual Companion** GameObject (skeletal `SpriteSkin`), transform-synced from
  `LocalToWorld` in LateUpdate. The companion owns *all* unit presentation — flip/lean,
  damage juice, emote telegraphs, throw tumble; the sim never reads it. This deliberately
  un-defers the previously parked "Mono visual companion" layer. Past a map-zoom threshold
  (with hysteresis) companions return to the pool and units render as static ECS quads.
- **One URP Forward+ renderer for everything.** The URP 2D Renderer / `Light2D` path
  (leftover experiment in `DaylightHandler`) is removed. Sprites use a custom lit shadergraph:
  flat diffuse + sun-direction rim + emissive mask, **no normal maps** — painted art carries
  baked shading, and dynamic normal response double-shades it. Alpha-clip + MSAA
  alpha-to-coverage keeps sprites in the opaque queue (real depth writes → sorting, shadows
  and the planar-reflection water RT all just work).
- **Cardboard kingdom.** Only terrain/ground/sea are 3D meshes. ALL Structures — city
  buildings, WallSections, towers, Beacon — are painted sprite cards over the existing
  invisible gameplay colliders (damage states = texture swaps). Units shadow via crossed
  shadow-only silhouette quads under a strict caster budget (sun + ≤2 hero lights per scene).

## Considered Options

- *All-ECS visuals (quads + flipbook shaders), no companions.* Rejected — the user chose
  skeletal 2D animation, which cannot run inside Entities Graphics; tiered animation (grunts
  flipbook, elites skeletal) was also rejected to keep one animation pipeline and one look.
- *URP 2D Renderer lighting (`Light2D`).* Rejected — cannot light the 3D environment, and
  splits the project into two lighting worlds.
- *Normal-mapped sprites (faithful HD-2D).* Rejected for now — double-shading against
  painted albedo + per-part authoring cost; revisit only if the look-dev spike says flat-lit
  looks dead.
- *3D meshes for Structures.* Rejected — solo dev with no modeling pipeline, AI art is
  strongest at exactly the AoE2-style building renders, and the KTC wall reference is 2D.

## Consequences

- The companion pool is load-bearing presentation infrastructure: entity spawn/despawn and
  LOD swaps must acquire/release companions, and a perf gate (50–100 units) sits in the HS
  exit criteria. Don't "simplify" unit visuals back into Entities Graphics — that path is
  blocked by skeletal animation, not oversight.
- Visuals are fully detached from physics: colliders/AABBs describe gameplay shapes that no
  longer match any rendered mesh. Debugging placement/collision needs gizmos, not eyes.
- Art authored per framing (beach side-on, city iso) — cards look wrong from the *other*
  framing's angle by design; cameras never show them that way.
- Renderer/quality settings assume MSAA 4x for A2C edges; disabling MSAA silently hardens
  every sprite edge (fallback = plain clip, accepted).
- `DaylightHandler` grows into the lighting driver (sun rotation + color + grading weight)
  and drops its `Light2D` field.
