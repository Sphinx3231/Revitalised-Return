# Player Base Character (functional placeholder) — 2026-07-31

## Task Brief (Director)
- **Goal:** Phase 1 of the user-directed "character → UI → combat" reprioritization (see
  CLAUDE.md "Current status"). Build a functional Player character: `CharacterController`-based
  3D kinematics driven by real input (Unity Input System, ported from the charter's Step 3
  spec), a placeholder primitive mesh (capsule stand-in for Jin — real art lands at Step 13),
  and a third-person/isometric camera rig. This compresses charter Steps 3 (Input System +
  0.15s action buffer) and 4 (kinematics, lean, dodge roll + i-frames) into one pass, using
  `legacy-godot/`'s validated formulas/timings as the porting reference rather than
  re-deriving them.
- **Affected systems:** `Assets/Settings/PlayerControls.inputactions` (new), `Assets/Scripts/Player/`
  (new: `PlayerInput` buffer, `PlayerMotor`/kinematics, `PlayerCamera` rig — split by
  responsibility per the S.O.L.I.D. standard, not one god-`MonoBehaviour`), `Assets/Prefabs/Player/`
  (new Player prefab), `Assets/Scenes/Sandbox/` (a movement test scene), `docs/Worklog.md`.
- **Constraints:**
  - **S.O.L.I.D. mandatory** (CLAUDE.md Section 2): split input-buffering, movement/kinematics,
    camera control, and (later) dodge i-frame/stamina logic into separate single-responsibility
    components communicating via small interfaces or `EventBus`, not one monolithic
    `PlayerController.cs`. No god-class.
  - Camera-relative input must be flattened before normalizing (charter 4.1's explicit
    amendment — literal camera-basis multiply injects a vertical component under pitch).
  - Kinematics constants are locked, not re-derived: `alpha_accel = 15.0`, `alpha_frict = 20.0`,
    gravity `g = 24.5 m/s^2`, dodge burst `1.8x` tapering to `1.0x` over `0.5s`, dodge i-frame
    window `0.15s–0.35s` within a `0.5s` total duration, dodge stamina cost `20.0` (Player has
    no stamina *system* yet — stub a placeholder float field, do not build the full stamina/
    posture bars now, that's Combat-phase/Step 11 HUD territory), input buffer expiry `0.15s`.
  - `com.unity.inputsystem` is already pinned to `1.20.0` and resolves clean per
    `docs/Tasks/2026-07-31-ui-systems-skeleton.md`'s blocker-resolution note — reuse that, do
    not re-litigate the package version.
  - Movement/camera code must respect `GameState.IsPlayerInputLocked()` (Step 2, already
    implemented) rather than assuming pause "just happens."
  - Placeholder mesh only — no real character art/animation rig this phase (Step 13 territory).
    A capsule (or ProBuilder blockout) is sufficient; do not block on art.
  - Use the live Unity-MCP tools for scene/prefab/GameObject work and compile verification, per
    established convention this session.
- **Definition of done:**
  - Input Actions asset exists with the charter's 3.1 binding list (move/light_attack/
    heavy_attack/parry/dodge/stance_next/stance_prev=Tab/interact), keyboard+gamepad.
  - A rolling 0.15s input buffer component exists and is unit-testable independent of
    `MonoBehaviour` lifecycle (per S.O.L.I.D./testability — pure C# class, not buried in
    `Update()`).
  - Player prefab moves via `CharacterController.Move()` with the lerped accel/friction
    kinematics, camera-relative flattened input, mesh lean on fast turns, and a working dodge
    roll with correct i-frame timing window.
  - A third-person camera rig follows the player (Cinemachine if Research confirms it's the
    right fit, per the charter's own open question at Step 8 intake — reuse that judgment here
    since it's the same camera-following problem).
  - A Sandbox test scene exists to exercise movement in Play mode.
  - Project compiles clean (verified via MCP `console-get-logs`), QA confirms actual movement
    behavior (not just compilation) via Play Mode testing through MCP where feasible.
  - Worklog updated, task file fully filled through Director sign-off.

## Research Findings (Research Agent)
File-verified (MCP connection was dead again this pass — reconnect before implementation):
1. `com.unity.inputsystem` pinned `1.20.0`, confirmed in manifest/lockfile. **Cinemachine 3.1.6
   is already resolved transitively** (pulled in by the MCP plugin's Cinemachine integration
   package) — usable today; should be promoted to an explicit direct dependency so it survives
   any future removal of the MCP package.
2. **Input pattern:** generated C# wrapper class from the `.inputactions` asset, not the
   `PlayerInput` MonoBehaviour (untestable, reflection-based). A pure-C#, zero-Unity-deps
   `InputBuffer` class (enum-keyed, caller-injected timestamps) is both testable and matches
   the charter's Step 3.2 spec directly.
3. **Camera:** Cinemachine 3.x confirmed available (`CinemachineCamera`, not the 2.x
   `CinemachineVirtualCamera`) — resolves the open question the charter flagged at Step 8.2 by
   reusing it here. `CinemachineFollow` (Body) + `CinemachineRotationComposer` (Aim) for a
   third-person rig; `CinemachineBasicMultiChannelPerlin` and `CinemachineTargetGroup` noted as
   direct future homes for Step 6/8.2's hand-rolled trauma-shake/midpoint-framing math.
4. **S.O.L.I.D. component split recommended and adopted** (see Approach below) — direct
   references (Call Down) + small interfaces for player-internal wiring; `EventBus` reserved
   for cross-system broadcast only (Signal Up), not per-frame internal state.
5. **`CharacterController` gotchas confirmed:** use `Move()` not `SimpleMove()` (the latter
   applies its own gravity, fighting the charter's explicit `g=24.5`); grounded velocity should
   clamp to `-2f` not `0f`; placeholder capsule `height=1.8/radius=0.4/center=(0,0.9,0)`
   matches the legacy Godot capsule exactly; `minMoveDistance` must be set to `0` (default
   `0.001` silently swallows slow movement). `Time.fixedDeltaTime` confirmed `0.02` (50Hz) —
   fine since dodge windows are time-based, not tick-count-based, in this port.
6. **Legacy Godot constants ported verbatim, no discrepancies found:** `S_SPEED=6.0` (documented
   only in legacy code, not the charter — carried forward as the judgment call it already was),
   `alpha_accel=15.0`, `alpha_frict=20.0`, `g=24.5`, lean scale `0.1` clamped ±5° using
   angle-delta wrapping (`Mathf.DeltaAngle` — the wrap is load-bearing, prevents a spurious
   spike at a ±180° facing crossing), dodge `1.8x→1.0x` linear over the full `0.5s` (not just
   the i-frame window), i-frames toggle `hurtboxCollider.enabled = false` (not
   `GameObject.SetActive`, per charter 14's own performance rule), stamina cost `20.0`/regen
   pause `1.2s`/regen rate `10.0`/s (also legacy-only judgment numbers, carried forward).
   Input buffer's exact `consume_action` semantics (newest-first scan, expired-but-matched
   entries still get removed, opportunistic pruning of unrelated expired entries) must be
   reproduced exactly in C# — already QA'd behavior in the legacy implementation.

## Approach & Tradeoffs (Director sign-off)
- **Component split (S.O.L.I.D., adopted from Research as-is):** `InputBuffer` (pure C#,
  no Unity deps, unit-testable) → `PlayerInputReader` (MonoBehaviour adapter, owns the
  generated Input Actions class, feeds the buffer) → `CameraRelativeInput` (pure C# helper,
  charter 4.1's flattened camera-basis transform) → `PlayerMotor` (MonoBehaviour, owns
  `CharacterController`, lerped kinematics) → `DodgeAbility` (MonoBehaviour, i-frame state
  machine, overrides motor's horizontal velocity while active) → `MeshLean` (MonoBehaviour,
  cosmetic only) → `PlayerRoot` (thin orchestrator MonoBehaviour, gates on
  `GameState.IsPlayerInputLocked()`, ticks the others in explicit order in `Update()` rather
  than relying on Unity's Script Execution Order settings). Interfaces (`IMovementInput`,
  `IInvulnerabilityProvider`) used where they earn their keep (Dependency Inversion); direct
  serialized references elsewhere. This is a deliberate structural departure from the legacy
  Godot `player.gd`, which Research flagged as a genuine god-class — the numbers port
  verbatim, the architecture does not.
- **Camera: Cinemachine 3.x**, resolving the charter's own open Step 8.2 question now since
  it's already resolved as a package. `CinemachineFollow` + `CinemachineRotationComposer`.
  **Game-feel reference (per the user's explicit steer to draw on the two locked design
  touchstones, Genshin Impact / Elden Ring, already used for Steps 9-14):** default to a
  Genshin-style medium-distance third-person follow (readable silhouette, generous framing
  for traversal) rather than Elden Ring's tighter over-the-shoulder lock — this project has no
  lock-on/target-switch system yet (that's Combat-phase/Step 7-8 territory), so a traversal-
  first framing is the right default for this phase. `followOffset`/damping values are a
  first-pass placeholder, expected to be re-tuned once the Combat phase adds lock-on framing.
- **Placeholder mesh:** a capsule primitive is sufficient (matches the legacy capsule
  dimensions exactly) — real silhouette/cloth-physics art is explicitly Step 13, not this
  phase. No stance-color-coding or stylization attempted yet (that's a Genshin-adjacent
  Step 13 concern per the charter's silhouette rules, not relevant to a grey capsule).
  Movement *feel* itself (the lerped accel/friction, not the visuals) is what should already
  read as responsive/weighty per the Elden-Ring-informed game-feel goals — that's what the
  locked kinematics constants are for, ported verbatim rather than re-tuned.
- **Verification:** live Unity-MCP tools once reconnected, same convention as Step 2.

## Implementation Summary (Implementation Agent)
### Attempt 1
- Built `Assets/Settings/PlayerControls.inputactions` (Player action map: `move` 2D-composite
  WASD/left-stick, `light_attack`/`heavy_attack`/`parry`/`dodge`/`stance_next`/`stance_prev`
  (Tab, per the standing charter amendment)/`interact`, keyboard+gamepad), C# class generation
  enabled (worked around an internal-API access error via `SerializedObject` reflection on the
  importer rather than the inaccessible `InputActionImporter` type directly).
- Built all 9 scripts under `Assets/Scripts/Player/` per the approved S.O.L.I.D. split:
  `InputBuffer`, `IMovementInput`, `IInvulnerabilityProvider`, `CameraRelativeInput`,
  `PlayerInputReader`, `PlayerMotor`, `DodgeAbility`, `MeshLean`, `PlayerRoot`.
- Built the Cinemachine 3.x camera rig (`CinemachineBrain` on Main Camera, `PlayerFollowCam`
  with `CinemachineFollow`+`CinemachineRotationComposer`, targets set to Player).
- Built `Assets/Prefabs/Player/Player.prefab` and `Assets/Scenes/Sandbox/MovementTest.unity`
  (Player + Ground plane + camera rig), all wiring verified via read-back against live
  instanceIDs, not assumed.

### Attempt 2 (fix loop, see QA Attempt 1 below)
- **Fix 1:** `PlayerMotor`/`MeshLean` converted from their own `Update()` to explicit
  `TickMotor(dt)`/`TickLean(dt)` methods; `DodgeAbility`'s coroutine-based timer replaced with
  a manual elapsed-time accumulator advanced via `TickDodge(dt)`. `PlayerRoot.Update()` is now
  the sole actual driver, calling input-read → buffer-consume/`TryDodge` → `TickDodge` →
  `TickMotor` → `TickLean` in that explicit order — matching what the class's own comments had
  claimed but hadn't actually implemented.
- **Fix 2:** `PlayerRoot` now assigns its concrete `PlayerInputReader` serialized field to a
  private `IMovementInput` field in `Awake()` and reads `MoveRaw` through that interface
  everywhere else, so Dependency Inversion is actually exercised, not just declared.
  `DodgeAbility : MonoBehaviour, IInvulnerabilityProvider` confirmed already correct.
- **Director-caught follow-up:** the fix added a new `PlayerRoot.meshLean` field that wasn't
  wired on either the live scene instance or the prefab asset — would have silently gone
  inert (no lean animation, no error). Wired both directly via
  `gameobject-component-modify` (scene instance) and `assets-prefab-open`/`-modify`/`-save`
  (prefab asset, confirmed on disk via `grep` after save: `meshLean: {fileID: ...}` present,
  no longer absent).

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** Independently re-read all 9 script files (not trusting the implementation
  report), cross-referenced every `PlayerRoot` serialized-reference instanceID against the
  real live components/camera transform, verified `CharacterController` values live, verified
  Cinemachine Follow/LookAt targets live, searched console logs for runtime exceptions.
- **Result:** Correctness/spec-match PASS on all locked constants and formulas (speed, lerp
  alphas, gravity, grounded-velocity clamp, dodge timing/multiplier curve, lean formula,
  flatten-before-normalize, `InputBuffer` legacy semantics). **Two design-quality deviations
  flagged** (not spec-breaking, but real): (1) `PlayerRoot`'s claimed explicit tick-ordering
  wasn't actually implemented — `PlayerMotor`/`DodgeAbility`/`MeshLean` each ran on their own
  independent `Update()`/coroutine schedule; (2) `IMovementInput`/`IInvulnerabilityProvider`
  were declared but never consumed as an actual field/parameter type, defeating their DIP
  purpose. One informational note (hurtbox collider unassigned — expected, Combat-phase
  territory, not a defect). **Play Mode itself could not be exercised** — no
  `editor-application-set-state`-equivalent tool was found in this MCP toolset session; static
  wiring/instanceID cross-referencing substituted as the best available check.
- **Director ruling:** given this task explicitly mandates S.O.L.I.D., both deviations were
  sent back for a real fix rather than accepted as "good enough" — see Implementation Attempt 2.

### Attempt 2 (post-fix)
- **Method:** Fix-loop implementation report + Director's own direct verification: re-read
  the four changed files, confirmed `assets-refresh`+`console-get-logs` showed zero `error CS`
  in a 10-minute window (large log required file-based `grep` due to output size), confirmed
  the `meshLean` wiring gap (which the fix itself introduced) via live `gameobject-find`/
  `gameobject-component-get` read-back on the scene instance and a `grep` of the saved
  `Player.prefab` YAML on disk.
- **Result: PASS.** Both deviations resolved; the wiring gap the fix introduced was caught and
  closed before sign-off rather than shipped. Zero compile errors.
- **Known gap, not a failure:** actual Play Mode runtime behavior (does the capsule really
  move/dodge/lean correctly when a human presses keys) remains unverified by an automated
  agent this cycle — no batchmode/Play-Mode-control MCP tool was available in this session.
  Recommend the user do a manual Play Mode pass in `MovementTest.unity` before treating this
  as gameplay-verified, not just compile-and-wiring-verified.

## Director Final Review
- Reviewed both implementation passes' diffs directly (not just the self-reports) and the
  QA report. The fix loop's own late-breaking wiring gap (`meshLean`) was itself caught before
  sign-off, on both the scene instance and the prefab asset independently (a prefab and its
  scene instances are separate serialized objects — confirmed both were updated, not just one).
- S.O.L.I.D. re-checked post-fix: `PlayerMotor` still never touches Input; `DodgeAbility` still
  never touches Input Actions; `MeshLean` still only reads velocity; `PlayerRoot` is now a real
  single explicit orchestrator (SRP for orchestration itself); `IMovementInput` is now actually
  exercised (DIP). No god-class, no dead abstractions remaining that were flagged.
- Genshin/Elden-Ring-informed camera framing (Approach section) is a first-pass placeholder by
  design — flagged again here so it isn't forgotten once the Combat phase adds lock-on framing.
- Real gap, explicitly not hidden: no agent in this pipeline actually pressed a key and watched
  the capsule move in a running Play session — everything verified is static (code, serialized
  wiring, compiled state). This is a tooling limitation (no Play-Mode-control MCP tool
  surfaced), not a shortcut taken silently. Logged here so it isn't forgotten before this is
  treated as "done" in a stronger sense than "compiles and is wired correctly."
- **Sign-off: Player base character (Phase 1) complete** with the above gap explicitly noted.
  Ready for Phase 2 (UI systems) next, per the user-directed reprioritization.
