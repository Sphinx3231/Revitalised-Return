# Step 9 (Unity port): World Generation, Terrain Greyboxing & Level Pacing — 2026-08-01

## Task Brief (Director)
- **Goal:** implement charter Step 9's full spec — the `RegionGraph`/`RegionNode` data
  structures, the shrine-spacing/rest-shrine convention, and a waypoint spine that doubles as
  Step 7's AI patrol source (already anticipated in Step 9's own charter text) — and greybox
  **one representative region** as proof of the pipeline, not all 5 acts. This mirrors every
  prior step's own scoping discipline (Step 5 proved combat resolution against one dummy, not
  a full bestiary; Step 7 proved AI against one enemy, not five). **Explicit scope boundary,
  logged up front:** only the Prologue ("Ashes of Sekigahara") gets built out this task — Act
  I-V's actual level content is real level-design work for future tasks, not attempted here.
- **Affected systems:** `Assets/Scripts/Systems/` (or a new `Assets/Scripts/World/` — decide
  in Approach) for `RegionGraph`/`RegionNode`/`RegionEdge` data types. `Assets/Scenes/Levels/`
  (currently empty — the Prologue scene lands here, the first real content in that folder).
  `Assets/ScriptableObjects/Regions/` (new, matching the `Stances/` precedent from Phase 2).
  ProBuilder-authored greybox geometry (this project already has `com.unity.probuilder`
  resolved and MCP `probuilder-*` tools available, unused until now). `Assets/Tests/EditMode/
  Editor/` (new tests for the data-structure/spacing logic, 80% gate — greybox geometry itself
  isn't unit-testable, same category as art/scene-content in every prior step).
  `docs/Worklog.md`.
- **Constraints:**
  - **S.O.L.I.D. mandatory** for the systemic pieces — `RegionGraph`'s data model, a shrine-
    placement/spacing validator, and the waypoint-spine-to-AI-patrol bridge are separate
    concerns. Greybox geometry itself is content, not logic — no S.O.L.I.D. concern applies to
    ProBuilder meshes themselves.
  - **Data structure locked** (charter 9): `RegionGraph` (`regionId`, `skylineAnchor`,
    `RegionNode[]`, `RegionEdge[]`, `criticalPath: string[]`) — Research to confirm whether
    this should be a `ScriptableObject` (asset-authorable, consistent with `StanceData`'s
    precedent) or a plain C# class (if it's meant to be constructed/populated at runtime
    rather than hand-authored) — charter says "plain C# class or `ScriptableObject`" itself,
    doesn't lock this choice.
  - **`RegionNode.Kind` enum locked**: `Entrance, Shrine, Encounter, Arena, Vista, Loot, Npc,
    Gate, Boss, SideDomain`.
  - **Shrine spacing locked** (charter 9): shrines at every region entrance (map-reveal on
    first rest — no map system exists yet, Step 11's territory, so this task only needs the
    shrine *placement* convention, not a functioning map reveal) and within 15-25s walk of
    every boss trigger `Collider` (non-negotiable, avoids bad runbacks into Step 8's
    arena-locked fights). Spacing target 60-120s travel between consecutive shrines. A
    `GraveMarker` auto-placed at every boss arena entrance (and, per the locked death-penalty
    ruling, at every death location — **no death-penalty/EXP system exists yet**, Step 14's
    territory, so this task only builds the boss-arena-entrance `GraveMarker` placement, not
    the death-location variant, logged as deferred).
  - **No real shrine *interaction* exists yet** (rest, save, map-reveal — that's Step 10's
    `Interactable`/`Shrine` subclass and Step 11's save policy). This task places shrine
    *markers* in the world per the spacing convention, wired as inert placeholders — a future
    step gives them real interaction behavior. Logged explicitly, not silently half-built.
  - **Waypoint spine doubles as Step 7's AI patrol source** — charter 9 says this explicitly.
    Step 7's `EnemyBrain.waypoints` is currently a flat `Transform[]` wired per-scene. Research
    to confirm whether this task should introduce a proper spine data structure (Splines
    package, already resolved per Step 7's own research) that both region navigation *and*
    enemy patrol pull from, or whether the existing flat-array approach is still sufficient
    for one region's worth of content and a spine is premature machinery for a single
    greyboxed area (mirroring Step 7's own reasoning for rejecting Splines there).
  - **Occlusion culling correctness** (charter's own Stage D caveat, already flagged in this
    project's charter text): greybox ProBuilder geometry must be real `MeshFilter`/
    `MeshRenderer` objects, not editor-only helper geometry, or it's invisible to the
    occlusion baker. Verify this explicitly for whatever's built this task.
  - **80% test coverage gate applies** to the systemic pieces (`RegionGraph` data model,
    shrine-spacing validation logic if any exists as code rather than pure level-design
    judgment). Scene/geometry content itself isn't coverage-bearing, same as every prior
    step's art/prefab work.
  - Use live Unity-MCP tools (including, for the first time this session, the `probuilder-*`
    tool family) for geometry/scene construction, established safety checks. **Mandatory human
    Play Mode pass required before sign-off**, though this one is primarily a visual/pacing
    check (does the greybox read clearly, are shrine distances reasonable) rather than a
    mechanical-correctness check like Steps 5-8.
- **Definition of done:**
  - `RegionGraph`/`RegionNode`/`RegionEdge` data types exist and compile clean, with the
    locked `RegionNode.Kind` enum.
  - A Prologue `RegionGraph` asset (or equivalent) exists describing at least: one `Entrance`
    node, one `Shrine` node, one `Boss` node (tying into Step 8's boss), a `criticalPath`
    connecting them.
  - A real greyboxed Prologue scene exists at `Assets/Scenes/Levels/` with ProBuilder-authored
    blockout geometry (real `MeshFilter`/`MeshRenderer`, verified occlusion-bakeable), a
    placed shrine marker, and the Step 8 boss placed in a real (if simple) arena rather than
    the open sandbox plane.
  - A `GraveMarker` placed at the boss arena's entrance.
  - Project compiles clean; ≥80% measured coverage on the systemic (non-geometry) new code.
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Research Agent)
Verified live: `com.unity.probuilder` 6.1.2, `com.unity.splines` 2.9.0 both already resolved.
1. **`RegionGraph` → `ScriptableObject`**, matching `StanceData`'s `[CreateAssetMenu]`
   precedent — a Prologue `.asset` is the natural DoD artifact. Use `string` node IDs for
   `RegionEdge.fromId`/`toId`/`criticalPath` (the charter already specifies this), not
   self-referencing node objects or `Dictionary` fields — both serialize poorly on a
   `ScriptableObject`. Runtime save state (`discoveredShrines`) belongs on the future
   `PlayerData`, never mutated onto the authored SO.
2. **No Splines, keep flat `Transform[]`** — same reasoning Step 7 already used to reject it.
   One greyboxed region doesn't need curve evaluation, and `EnemyBrain.waypoints` is already
   `Transform[]`; introducing `SplineContainer` now would force either an `EnemyBrain` change
   (out of scope) or a spline→Transform baking bridge that's pure overhead for this task's
   scope. Author the spine as an empty `WaypointSpine` GameObject with ordered child
   Transforms, referenced by both `RegionGraph` nodes and `EnemyBrain.waypoints` directly.
3. **ProBuilder recipe verified live** (concrete, not theoretical): `probuilder-create-shape`
   Cube for floor/walls (verified real `MeshFilter`/`MeshRenderer`/`ProBuilderMesh` output,
   occlusion-bakeable as-is) — **no Collider is added automatically**, must add
   `MeshCollider`/`BoxCollider` per piece or the player falls through. `probuilder-extrude` is
   for lengthening a corridor/raising a ledge from an existing slab, not for turning a floor
   into a room (extruding a cube's Up face just makes a taller solid block, verified). Simple
   separate wall Cubes are the right approach for a room/arena, not extrude-from-floor.
   `probuilder-create-poly-shape` is NOT actually exposed by this MCP server despite being
   listed — use `create-shape` instead.
4. **Occlusion culling verification via `script-execute`** (no MCP tool exists) — asserting
   each greybox root has `MeshFilter.sharedMesh != null`, `MeshRenderer` enabled, and
   `GetStaticEditorFlags` containing `OccluderStatic|OccludeeStatic|BatchingStatic` is the
   DoD-relevant check; a full `StaticOcclusionCulling.Compute()` bake is optional/out of scope
   for this task's proof-of-mechanism goal.
5. **Boss arena: build fresh in `Prologue.unity`, leave `MovementTest.unity` untouched.**
   `MovementTest.unity` is the isolated Sandbox test rig per the project's own folder
   convention (holds Steps 5-8's entire regression surface: `ArenaBarrier_*`,
   `BossEncounterTargetGroup`, `Waypoint_A/B`, HUD, etc.) — gutting or repurposing it would
   destroy that regression surface. Instantiate a fresh `Boss.prefab` into the new Prologue
   scene with its own `BossPhaseController.arenaBarriers` pointing at new real geometry.
   **Flagged gotcha for Approach:** `BossPhaseController.OnEnable()` seals arena barriers
   immediately (no aggro-trigger mechanism exists yet from Step 8) — in a real level this means
   the arena is walled from scene load. Must be resolved explicitly (accept-and-log, or place
   the shrine/GraveMarker outside the sealed ring) rather than silently producing an
   unreachable boss.

## Approach & Tradeoffs (Director sign-off)
(pending — Research complete, Approach not yet written; picking up here next session)

## Implementation Summary (Implementation Agent)
(pending)

## QA Iterations (QA/Test Agent)
(pending)

## Director Final Review
(pending)
