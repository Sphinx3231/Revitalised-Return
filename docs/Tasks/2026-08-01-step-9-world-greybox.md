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
- **Adopt all 5 Research recommendations as-is** — no open design questions left unresolved.
- **Data structures, `Assets/Scripts/World/`** (new folder — region/level data is its own
  concern, not a `Systems/`-level engine primitive nor `Combat/` content): `RegionNode.cs`
  (`[System.Serializable]` plain class: `id: string`, `Kind` enum (`Entrance, Shrine,
  Encounter, Arena, Vista, Loot, Npc, Gate, Boss, SideDomain` — locked), `worldPosition:
  Vector3`, `displayName: string`), `RegionEdge.cs` (`[System.Serializable]`: `fromId: string`,
  `toId: string`), `RegionGraph.cs` (`ScriptableObject`, `[CreateAssetMenu(menuName =
  "Return/Region Graph")]`: `regionId: string`, `skylineAnchor: string`, `nodes: RegionNode[]`,
  `edges: RegionEdge[]`, `criticalPath: string[]`). A small `RegionGraphValidator` (pure
  static class, testable) checks the shrine-spacing convention against a `RegionGraph` asset
  — not enforced at authoring time (no in-Editor blocking validation this task, that's
  tooling polish out of scope), just a queryable pass/fail the tests exercise.
- **Prologue `RegionGraph.asset`** at `Assets/ScriptableObjects/Regions/Prologue.asset`: an
  `Entrance` node, a `Shrine` node, a `Boss` node (`Captain Renzo`, tying to Step 8's `Boss`
  prefab), `criticalPath` connecting them in order.
- **Waypoint spine: flat `Transform[]` via a `WaypointSpine` marker GameObject** (empty
  GameObject with ordered child Transforms) — per Research, explicitly not Splines. Both the
  `RegionGraph`'s node `worldPosition`s and `EnemyBrain.waypoints` reference this same spine's
  children directly — no bridging code needed since both already consume `Transform`/`Vector3`.
- **Greybox scene: `Assets/Scenes/Levels/Prologue.unity`**, built fresh, per Research's
  explicit recommendation to leave `MovementTest.unity` untouched as the isolated Sandbox
  regression rig. ProBuilder recipe per Research's verified findings: separate wall/floor
  Cubes (not extrude-from-floor, which was verified to produce a solid block, not a room),
  each with an added `MeshCollider` (ProBuilder shapes don't get one automatically) and
  `StaticEditorFlags` set to `OccluderStatic | OccludeeStatic | BatchingStatic` (verified via
  `script-execute`, per Research's confirmed approach — no MCP tool exists for this).
- **Ruling on Research's flagged gotcha (`BossPhaseController.OnEnable()` seals barriers
  immediately, no aggro-trigger exists):** accept-and-log for this task, per Research's
  offered fallback — place the shrine and `GraveMarker` **outside** the arena's sealed ring
  entirely (at the region `Entrance`, not adjacent to the `Boss` node), so the encounter is
  walled from scene load but the rest-shrine/runback loop the charter cares about is still
  genuinely walkable and testable. A real aggro-trigger mechanism (only sealing on the player
  actually approaching) is deferred to whichever future task adds real encounter-entry
  triggers — not invented here as a scope-creep fix for a Step 8 gap.
- **Occlusion culling:** flags-only verification per Research (no full bake required for this
  task's proof-of-mechanism scope) — a `script-execute` assertion checking every greybox root
  has a populated `MeshFilter.sharedMesh`, enabled `MeshRenderer`, and the correct
  `StaticEditorFlags` combination.
- **80% coverage gate** applies to `RegionNode`/`RegionEdge`/`RegionGraph`/
  `RegionGraphValidator` (pure data/logic, fully EditMode-testable) — the greybox geometry and
  scene content itself is not coverage-bearing, consistent with every prior step's art/prefab
  work.
- **Verification:** live MCP tools (first use of the `probuilder-*` family this session) per
  established convention; mandatory human Play Mode pass — primarily a visual/pacing check
  this time (does the greybox read clearly, is the shrine reachable, does the boss arena feel
  appropriately scaled) rather than the mechanical-correctness scrutiny Steps 5-8 needed.

## Implementation Summary (Implementation Agent)
- `Assets/Scripts/World/RegionNode.cs`/`RegionEdge.cs`/`RegionGraph.cs`/
  `RegionGraphValidator.cs` created per the approved design — `RegionGraph.FindNode` and
  `RegionGraphValidator`'s two spacing checks (`HasShrineNearEveryBoss`,
  `HasEntranceShrine`), both correctly vacuously-true when no matching node kind exists.
- `Assets/ScriptableObjects/Regions/Prologue.asset`: Entrance/Shrine/Boss ("Captain Renzo")
  nodes, `criticalPath` connecting them, matches the built scene's actual geometry positions.
- `Assets/Scenes/Levels/Prologue.unity`: 10 ProBuilder-authored greybox pieces (entrance
  floor, 2 corridor segments, arena floor, 4 arena walls, 1 gap-filling arena barrier), each
  with an explicitly-added `MeshCollider` (ProBuilder doesn't add one automatically, confirmed)
  and correct `OccluderStatic|OccludeeStatic|BatchingStatic` flags. `Player`/`Boss` prefab
  instances placed; `Boss.BossPhaseController.arenaBarriers` wired to the real gap barrier.
  Inert `ShrineMarker`/`GraveMarker` placeholders positioned outside the boss's
  immediately-sealed ring (per the Approach's explicit ruling on the known Step 8
  `OnEnable()`-seals-immediately gotcha), a `WaypointSpine` with 3 ordered waypoints along the
  entrance→arena path.
- **Self-reported, correctly-scoped gap:** `BossPhaseController.playerTransform`/
  `playerKnockback` left unwired on the Prologue `Boss` instance — the Phase-2 AoE knockback
  will silently no-op there. Flagged proactively by Implementation, not discovered later.
- 335/335 tests passing, 96.6% reported coverage.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** independently re-read all 4 new data-type files, traced
  `RegionGraphValidator`'s distance-comparison logic directly for operator/off-by-one bugs,
  read `Prologue.asset`'s raw YAML to confirm `criticalPath` references real node ids (no
  typos — a genuine risk with string-keyed data), read `Prologue.unity`'s raw scene file to
  cross-reference `arenaBarriers`' wired fileID against the real `ArenaBarrier_Gap` object
  (not just "non-zero"), verified `MeshCollider` presence explicitly on the entrance and arena
  floors (a missing collider would let the player fall through), verified `ShrineMarker`/
  `GraveMarker` are spatially outside the sealed arena ring by comparing actual coordinates,
  independently re-ran the occlusion-flag check via `script-execute` rather than trusting the
  "10/10 passed" self-report, and formed an independent judgment on the self-reported
  `playerTransform`/`playerKnockback` gap rather than rubber-stamping Implementation's own
  framing of it.
- **Result: PASS, no defects found.** `RegionNode.Kind` enum matches the charter-locked list
  exactly. Validator logic correct (`<=` comparison, genuine vacuous-true behavior, not just
  documented). `criticalPath` ids all resolve to real nodes. All 10 greybox pieces confirmed
  with real colliders and correct occlusion flags (independently reproduced, not trusted).
  `arenaBarriers` wiring confirmed correct via fileID cross-reference. Markers confirmed
  spatially outside the sealed ring. **On the `playerTransform`/`playerKnockback` gap: QA's
  independent judgment concurred with Implementation's own framing** — the task's DoD asks
  whether the boss is "placed in a real (if simple) arena," not whether a full Phase-2
  knockback loop is playable in this specific greybox scene (that's already proven in the
  `MovementTest.unity` regression rig from Step 8) — acceptable to sign off with the gap
  logged as a deferred item, not a defect requiring a fix loop.
- **Director closed the coverage-verification gap directly:** closed the interactive Editor,
  independently re-ran the verified batchmode CLI, and reproduced **96.6% line coverage
  (745/771), 335/335 tests passing** — exact match to both Implementation's and QA's numbers.

## Director Final Review
- This task's scope discipline held throughout: one region greyboxed as pipeline proof, not
  all 5 acts attempted; the shrine/GraveMarker are genuinely inert placeholders as declared,
  not silently half-built with hidden behavior; the `playerTransform`/`playerKnockback` gap
  was surfaced proactively by Implementation and independently re-assessed (not just accepted)
  by QA before being judged acceptable — this is the right pattern for a "proof of mechanism"
  task, distinct from how Steps 5-8's mechanical-correctness gaps (parry math, boss-defeat
  unseal) were correctly treated as blocking fix-loop items instead. The Prologue arena is
  built from real, occlusion-bakeable, collider-bearing geometry rather than the invisible
  placeholder cubes Step 8 used out of necessity (no real level existed yet at that point) —
  this is a genuine step forward in fidelity, not just more of the same pattern.
- S.O.L.I.D. holds: `RegionGraph`/`RegionNode`/`RegionEdge` are pure data, `RegionGraphValidator`
  is a pure stateless function set — no god-class, no logic bleeding into the data types.
- **Sign-off: Step 9 (Unity port) complete**, pending the mandatory human Play Mode pass — this
  one is primarily a visual/pacing check (does the greybox read clearly, is the shrine
  reachable, does the arena feel appropriately scaled) rather than the mechanical-correctness
  scrutiny Steps 5-8 needed. 96.6% measured coverage (target 80%), 335/335 tests passing,
  independently double-confirmed. Next in strict 14-step order: Step 10 (Interactive Objects,
  Inventory Data & Gathering Economy) — which is also what finally gives this task's inert
  Shrine/GraveMarker placeholders real interaction behavior.
