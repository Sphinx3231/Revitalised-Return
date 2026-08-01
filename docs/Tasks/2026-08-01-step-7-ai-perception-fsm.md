# Step 7 (Unity port): AI Architecture, Perception & Behavior — 2026-08-01

## Task Brief (Director)
- **Goal:** implement charter Step 7 in full per CLAUDE.md's "STEP DETAIL SPECIFICATIONS"
  7.1/7.2 — multi-sensory perception (vision cone + acoustic detection), and the enemy FSM
  (Idle/Patrol → Investigate → Telegraph → Attack → Recovery). This is the payoff for Step 5's
  `EnemyHitbox`/`EnemyHurtbox` layers, which have existed unused since Step 5. Replaces the
  passive `TrainingDummy` with a real enemy that can see the player, investigate noise, and
  attack — closing the "parry can't be manually verified" gap explicitly logged at Step 5
  sign-off, since a human can finally test parry/block against a live attacker.
- **Affected systems:** `Assets/Scripts/AI/` (new — matches the charter's own pre-declared
  folder, previously empty): perception, FSM/state components, enemy attack orchestration.
  `Assets/Prefabs/Enemies/` (new `Enemy.prefab`, or evolve `TrainingDummy.prefab` — decide in
  Approach). `Assets/Scenes/Sandbox/MovementTest.unity` (enemy placement + patrol waypoints).
  `Assets/Tests/EditMode/Editor/` (new tests, 80% gate). `docs/Worklog.md`.
- **Constraints:**
  - **S.O.L.I.D. mandatory**, same discipline as every prior phase: perception, FSM state,
    movement, and attack are separate concerns/components, not one `EnemyController` god-class.
    Reuse the exact `IDamageable`/`WeaponHitbox`/layer pattern Step 5 already built for the
    player, on the enemy side (`EnemyHitbox`/`EnemyHurtbox` layers already exist and are
    already correctly configured in the Physics Layer Collision Matrix — no new layer work
    needed this task).
  - **Perception numbers locked** (charter 7.1): vision cone half-angle `45°` (90° total),
    range `18.0m`, tick every `0.1s`, detection meter increment `30.0/d × deltaTime` while
    the raycast has unobstructed line of sight; acoustic detection sphere `8.0m`, triggers
    instant `Investigate` if the player is sprinting/rolling/swinging a weapon within range.
    **No sprint system exists yet** (player only has Move/Dodge/Attack) — Research to confirm
    how to detect "sprinting" given this gap (charter's own Step 14 also references a stagger
    framing note for this — likely: treat "moving" as the closest available proxy, or defer
    the sprint-specific trigger and keep dodge/attack as the acoustic triggers, logging the
    sprint gap explicitly rather than inventing a sprint mechanic that doesn't exist).
  - **FSM states and transitions locked** (charter 7.2): `Idle/Patrol` (waypoint following at
    50% speed + perception sweeps) → `Investigate` (full speed to last-known position, look
    around `3.0s`) → `Telegraph` (locked movement, smooth turn via `Quaternion.Slerp`/
    `Mathf.LerpAngle`, wind-up `0.3-0.6s`) → `Attack` (weapon hitbox active during swing,
    reusing Step 5's `AttackController`/`WeaponHitbox` pattern) → `Recovery` (cooldown
    `0.8-1.5s`, circle/backstep) → back to `Investigate`/`Idle`.
  - **No real animations exist yet** (Step 13) — Telegraph's "wind-up animation"/"eye-glint
    particle indicator" and Attack's "active swing frames" reuse the same timed-window
    placeholder convention already established in Step 5/6 (`TODO(Step 13)`).
  - **Movement approach:** charter 7.2 itself flags this as unresolved ("a simple `Transform[]`
    array, or Unity's Splines package... Research to confirm at Step 7 intake; Godot's
    `Path3D` has no single 1:1 Unity equivalent"). Research to give a concrete recommendation
    — simple waypoint array + a reused/adapted `CharacterController`-based motor (matching
    `PlayerMotor`'s established pattern) is the likely fit given this project has no NavMesh
    setup yet and Splines would be new machinery for a single-enemy proof-of-concept.
  - **80% test coverage gate applies.** Perception/FSM logic should follow the same explicit
    `Tick(deltaTime)` pattern used everywhere else in this project for EditMode testability —
    `Physics.Raycast`/`Physics.OverlapSphere` calls are real-physics-dependent and may need
    the same PlayMode-exclusion treatment as `PlayerMotor`'s CharacterController calls (already
    included in EditMode coverage per Research's Step-5-era finding that `PlayerMotor` tests
    fine in EditMode with a real `CharacterController` — Research to confirm the same holds
    for `Physics.Raycast`/`OverlapSphere` against a manually-constructed EditMode test scene).
  - Use live Unity-MCP tools, established safety checks (Edit-mode-only mutation, wire both
    prefab AND scene instance, verify by read-back). **Mandatory human Play Mode pass required
    before sign-off** — and this is the step that finally makes the Step-5-flagged parry gap
    closeable, so the Play Mode pass should specifically include trying to parry the enemy's
    attack, not just observing perception/patrol behavior.
- **Definition of done:**
  - An `Enemy` prefab exists with vision-cone + acoustic perception, patrols between at least
    2 waypoints in `MovementTest.unity`, transitions to `Investigate` on spotting the player or
    hearing a qualifying acoustic event, transitions through `Telegraph`→`Attack`→`Recovery`
    when close enough, and its `Attack` state actually damages the player via the existing
    `IDamageable`/`WeaponHitbox`/layer machinery (on the correct `EnemyHitbox`/`PlayerHurtbox`
    layers).
  - A human can Play-Mode-verify: the enemy notices the player, investigates, attacks, and —
    critically — that parrying the enemy's attack produces the Step 5-implemented parry
    resolution (interrupt + posture damage to the enemy, `ParryExecuted` fires) for the first
    time with a live opponent.
  - Project compiles clean; ≥80% measured coverage on newly-added logic-bearing code, any
    genuine PlayMode-only classes excluded per the established, logged pattern.
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Research Agent)
1. **Movement:** `com.unity.splines`/`com.unity.ai.navigation` already resolve (transitively),
   but neither adds real value for one enemy patrolling 2 waypoints on an open flat plane —
   recommend plain `Transform[]` + a `CharacterController`-based `EnemyMotor` mirroring
   `PlayerMotor` exactly (`SetDesiredDirection`/`TickMotor`, same lerp constants, `SpeedScale`
   field for Patrol's 50%). NavMesh especially rejected: `NavMeshAgent` owns its own movement,
   incompatible with the explicit-`Tick` testability pattern the coverage gate depends on.
2. **Acoustic detection:** no sprint mechanic exists. `DodgeAbility.IsDodging` and
   `AttackController.IsAttacking` already exist as public flags — wire dodge+attack as the
   acoustic triggers, explicitly log the sprint gap rather than inventing a speed-threshold
   proxy (which Research confirmed would fire on ordinary walking at the player's only
   available speed, effectively making the sphere an always-on aggro bubble — worse than
   omitting it).
3. **EditMode physics — critical finding, not assumed:** `Physics.autoSyncTransforms` is
   `false` in EditMode — a raycast/`OverlapSphere` against a just-moved transform **silently
   returns false/empty** until `Physics.SyncTransforms()` is explicitly called. Verified
   live against the actual Editor, not from documentation alone. This must be called both in
   EditMode tests AND in the real runtime perception tick (whenever the enemy or player moves
   and a query happens the same frame). Also: `Physics.queriesHitTriggers` defaults `true` —
   the line-of-sight raycast must pass `QueryTriggerInteraction.Ignore` or the player's/
   enemy's own trigger hurtboxes register as false obstructions. With these two fixes, no
   PlayMode exclusion is needed — perception is genuinely EditMode-testable.
4. **FSM pattern:** single enum + `switch`-based explicit `Tick`, matching `GameState.cs`'s
   own house style — a full per-state-class State pattern was considered and explicitly
   rejected as over-engineering for a closed, fully-specified 5-state graph with exactly one
   enemy type (consistent with this project's own anti-premature-abstraction convention).
5. **Attack reuse:** `AttackController`/`WeaponHitbox` are already stance-agnostic
   (null-safe `StanceController` dependency, `WeaponHitbox` treats null `CurrentStance` as
   1.0× multipliers) — reuse unmodified on the enemy, no bespoke enemy-attack class needed.
   **This is load-bearing, not just convenient:** `WeaponHitbox`'s parry branch specifically
   calls `GetComponentInParent<AttackController>().TryInterrupt()` and applies posture damage
   via `GetComponentInParent<IDamageable>()` — a bespoke enemy attack class would silently
   break the exact parry payoff this task exists to deliver. `TrainingDummy.prefab` already
   has the correct hurtbox-side wiring (layer 11, `DummyHealth`, `HitFlash`) — recommend
   evolving it into `Enemy.prefab` rather than starting fresh.

## Approach & Tradeoffs (Director sign-off)
- **Adopt all 5 Research recommendations as-is** — no open design questions left unresolved.
- **Component split (S.O.L.I.D.), `Assets/Scripts/AI/`:** `EnemyPerception` (vision cone +
  acoustic sphere, `Tick(deltaTime)` internally staggered via charter 14's
  `_perceptionOffset = Random.value * 0.1f` seeded in `Awake` for deterministic tests, exposes
  `bool CanSeePlayer`/`bool HeardNoise` read-only state — does not itself decide FSM
  transitions), `EnemyMotor` (mirrors `PlayerMotor`, adds a `SpeedScale` field), `EnemyBrain`
  (the orchestrator — owns the `State` enum, `TransitionTo`, `Tick(deltaTime)` calling
  perception/motor/attack in explicit order, matching `PlayerRoot`'s established role — reads
  `EnemyPerception`'s exposed state to decide transitions, calls `AttackController.TryLightAttack()`/
  `TickAttack()` reused directly from Step 5). Waypoints as a plain `Transform[]` serialized
  field on `EnemyBrain` (or a small `PatrolRoute` holder if that reads cleaner — Implementation's
  call, not a design fork worth blocking on).
- **Perception correctness fixes, non-negotiable per Research's live-verified finding:** every
  `Physics.Raycast`/`OverlapSphere` call in `EnemyPerception` must be preceded by
  `Physics.SyncTransforms()` when a transform moved this frame, and the LOS raycast must pass
  `QueryTriggerInteraction.Ignore`. Tests must do the same in `[SetUp]`/before assertions.
- **`Enemy.prefab` evolves from `TrainingDummy.prefab`** (Research's recommendation) — adds
  `CharacterController`, `EnemyMotor`, `EnemyPerception`, `EnemyBrain`, a `WeaponHitbox` child
  on layer `EnemyHitbox` (10), reuses the existing `DummyHealth`(`IDamageable`)/`HitFlash` on
  layer `EnemyHurtbox` (11) and `AttackController` (added fresh, reused unmodified per
  Research). Whether to rename the prefab/asset or keep the `TrainingDummy` name with expanded
  behavior is Implementation's call — functionally it becomes the real enemy either way. This
  finally closes Step 5's explicitly-logged "parry can't be manually verified without a live
  attacker" scope boundary.
- **Verification:** live MCP tools per established convention; mandatory human Play Mode pass,
  explicitly including a parry attempt against the live enemy; ≥80% measured coverage via the
  batchmode CLI, no new PlayMode exclusions expected per Research's finding.

## Implementation Summary (Implementation Agent)
### Attempt 1 — CUT OFF MID-TASK, NOT COMPLETE
The Implementation Agent was terminated mid-execution by a session/API limit (not a code or
design failure) partway through wiring the enemy prefab, immediately after stating "Now let's
add CharacterController, EnemyMotor, EnemyPerception, EnemyBrain, and AttackController to the
root." **This attempt is explicitly NOT signed off and Step 7 is NOT done.** Recorded here
factually so the next session can resume from an accurate checkpoint rather than re-deriving
what happened.

**Confirmed complete (verified independently by the Director, not just claimed):**
- `Assets/Scripts/AI/EnemyMotor.cs`, `EnemyPerception.cs`, `EnemyBrain.cs` exist on disk.
- Compiles clean — `assets-refresh` + `console-get-logs` (Error, 15 min) returned zero entries.
- `TrainingDummy.prefab` has all 6 intended `MonoBehaviour` components attached, confirmed by
  cross-referencing each component's serialized GUID against each script's `.meta` file:
  `EnemyMotor`, `EnemyPerception`, `EnemyBrain`, `AttackController` (newly added), plus the
  pre-existing `DummyHealth`/`HitFlash` from Steps 5/6.

**Confirmed NOT done (verified, not assumed):**
- `MovementTest.unity` shows **zero uncommitted changes** (`git status` clean on that file,
  scene `IsDirty: false`, and a direct `scene-get-data` root-object listing shows exactly the
  10 objects already known from Steps 1-6 — no waypoint GameObjects, no stray/partial objects
  left behind). **No scene-level wiring happened at all**: `EnemyBrain.playerTransform`/
  `waypoints`, `EnemyPerception.playerTransform`/noise-source references, and the
  `WeaponHitbox` child GameObject on the enemy (layer `EnemyHitbox`) — none of this exists yet.
- No `CharacterController` dimensions were tuned on the prefab (unverified whether the
  component itself was even added — the agent's cut-off message named it as about to happen).
- **Zero tests were written.** The 80% coverage gate has not been attempted, let alone met.
- No human Play Mode pass has occurred (expected, given the above — there's nothing
  functional to test yet).

**What IS safe:** the three new script files compile clean and don't break anything else in
the project (independently confirmed via a live compile check, not assumed from the cut-off
agent's own claims). This is being committed as an explicit, labeled WIP checkpoint — matching
this project's own established precedent (`9341ed2`: "WIP: pause UI/systems skeleton task —
blocked on Input System package") — not as a completed step.

## QA Iterations (QA/Test Agent)
Not started — nothing to QA yet given Implementation didn't reach a testable state.

## Director Final Review
**Not signed off. Step 7 remains open.** Next session should resume Implementation from where
it left off: finish wiring `AttackController`'s `WeaponHitbox` child on the enemy (layer
`EnemyHitbox`), tune `CharacterController` dimensions, wire the scene-level cross-references
(`playerTransform`, `waypoints`, noise-source components) on the `MovementTest.unity` instance,
write the EditMode tests (especially `EnemyPerception`'s — the Research-flagged
`Physics.SyncTransforms()`/`QueryTriggerInteraction.Ignore` correctness requirements are the
highest-risk untested logic right now), measure real coverage, then route through QA. The
Research Findings and Approach & Tradeoffs sections above remain valid and fully approved —
only Implementation needs to resume, not re-litigation of the design.
