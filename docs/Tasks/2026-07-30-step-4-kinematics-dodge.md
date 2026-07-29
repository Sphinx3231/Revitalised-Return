# Step 4: 3D Kinematics, Movement Physics & Dodge Roll — 2026-07-30

## Task Brief (Director)
- **Goal:** Implement Step 4 of the 14-step pipeline per CLAUDE.md Section 4.1-4.3 —
  camera-relative `CharacterBody3D` movement with lerped acceleration/friction,
  gravity, dynamic mesh lean on fast turns, and a dodge roll with a 0.15s-0.35s
  i-frame window, exactly per the formulas specified:
  - `D_input = (Transform_cam.basis * V_input).normalized()`
  - `V_target = D_input * S_speed`; `V_horizontal = lerp(V_horizontal, V_target, alpha * delta_t)`
    (`alpha_accel = 15.0` when input present, `alpha_frict = 20.0` when not)
  - `V_y -= g * delta_t` (`g = 24.5`)
  - Lean angle (Z) = `-clamp(omega_yaw * 0.1, -5.0deg, 5.0deg)`
  - Dodge: locks to `D_input` (or facing if idle), speed burst `1.8x -> 1.0x S_speed`
    over 0.5s, i-frames active `0.15s <= t <= 0.35s` (disable hurtbox collision or
    `is_invulnerable = true`), costs 20.0 stamina, pauses stamina regen for 1.2s.
- **Affected systems:** no `scripts/player/` or `scenes/Player/` exists yet — this
  step must create the player `CharacterBody3D` scene/script and a minimal
  greybox test scene (floor + camera) sufficient to exercise movement, since
  Step 9 (World Greybox) hasn't run yet and there's currently nothing to stand
  on or a camera to be relative to. Reads `InputBuffer`/`GameState` (Steps 2-3).
  No `Stamina` resource exists yet (Step 5 territory per CLAUDE.md's stat
  system) — dodge's stamina cost needs a Director scoping call (see Research).
- **Constraints:**
  - Must not guess Godot 4.7 `CharacterBody3D`/`move_and_slide()` semantics —
    verify against docs/empirical probes, per this project's established
    Research discipline.
  - Must follow Step 2/3's autoload and code conventions where applicable
    (though the player controller itself is a scene-attached script, not an
    autoload — first non-autoload gameplay script in the project).
  - No animation system exists yet (Step 13) — "dynamic mesh lean" applies to
    whatever placeholder mesh represents the player, not a rigged/animated one.
  - No `PlayerHurtbox` exists yet (Step 5 builds hit/hurtboxes) — the i-frame
    window's "disable PlayerHurtbox.collision_layer" instruction needs a Step 4
    scoping call (stub flag now, wire to a real hurtbox in Step 5) rather than
    inventing hurtbox infrastructure prematurely.
- **Definition of done:**
  - A player scene with `CharacterBody3D` + collider + camera exists and can be
    run/instantiated headlessly.
  - Movement, lean, gravity, and dodge match the specified formulas/timings.
  - Headless smoke test passes with no ERROR/SCRIPT ERROR; a dedicated
    kinematics/dodge test (probe script, no real window/input device needed)
    proves the lerp curve, i-frame window timing, and stamina-cost/regen-pause
    behavior (using a stub or minimal stand-in if the real Stamina resource
    doesn't exist yet — Director to rule on this in sign-off).

## Research Findings (Research Agent)
All verified empirically against the real `Godot_v4.7.1-stable_win64_console.exe` in a throwaway probe project (Jolt Physics, matching this repo's `project.godot`), probe deleted after:

1. **`CharacterBody3D`/`move_and_slide()`:** `move_and_slide()` takes 0 args, returns bool, reads/writes the `velocity` property — no 4.x deprecation. Verified defaults (`up_direction=(0,1,0)`, `floor_max_angle=45°`, `motion_mode=GROUNDED`, etc.) are all correct for Step 4 as-is, no overrides needed. `Engine.physics_ticks_per_second=60`; measured headless `_physics_process` delta is a constant `0.016666666`, so `alpha*delta` (0.25/0.333) never overshoots the lerp. Verified lerp curve: α=15 reaches 94.4% of target in 10 ticks, 99.98% in 30 — a good assertable curve for QA.

2. **Camera-relative formula is buggy exactly as CLAUDE.md 4.1 states it:** with a camera pitched -45°, the literal `(Transform_cam.basis * V_input).normalized()` produces `(0, -0.707, -0.707)` for forward input — a third-person camera looking down injects a large −Y component and halves horizontal magnitude. **Must flatten:** `Vector3(raw.x, 0, raw.z).normalized()`, verified to correctly produce `(0,0,-1)`. This is a charter formula bug, same class as Step 3's E/Tab conflict — flagging for a Director ruling, not silently "fixing" without a paper trail.
   - Verified a `Node3D` pivot -> `Camera3D` child auto-registers as the active camera (`get_viewport().get_camera_3d()`, `current==true`) with zero script.
   - Research recommends building `CamPivot (Node3D) -> SpringArm3D -> Camera3D` now even though Step 4 doesn't explicitly mandate `SpringArm3D` — `SpringArm3D` exists in 4.7, costs one node/zero script, and retrofitting it later would reparent the camera and invalidate every Step 4 test's transform assumptions. Camera pivot should be a sibling of the player (following its position), not a child of the player body, so mesh lean never rotates the camera.

3. **Headless physics testing verified working, twice:** scene-mode headless run (matching Step 2/3's convention) ticks `get_tree().physics_frame` 1:1 with `Engine.get_physics_frames()`, constant delta, `quit(0)` honored. A `CharacterBody3D` genuinely falls and lands on a `StaticBody3D` floor headless under Jolt (dropped from y=3.0, `is_on_floor()==true` at tick 29, settled at y≈0.001). Minimal floor: `StaticBody3D`+`CollisionShape3D`+`BoxShape3D(20,1,20)`; player: `CapsuleShape3D(h=1.8,r=0.4)`. **Do not use `Engine.time_scale` to speed up tests** — verified it changes delta and corrupts the lerp curve being tested; headless physics is wall-clock paced (60 ticks ≈ 990ms), acceptable under a timeout. **Assert by tick count, not seconds:** 0.15s=tick 9, 0.35s=tick 21, 0.5s=tick 30.

4. **Director-decision points (Research's read, not a ruling):**
   - Build the real player scene + floor now — a pure-math unit test can't catch the camera-basis bug, gravity/`is_on_floor` interaction, or real-physics-tick i-frame timing, which are the actual risks here. Suggested layout: `scenes/player/player.tscn` + `scripts/player/player.gd`; test harness at `scripts/tests/kinematics_test.gd/.tscn` (keep the floor out of `scenes/world/`, which Step 9's real greybox will own — don't make Step 9 delete Step 4's throwaway floor).
   - Stamina: implement the dodge's cost/regen-pause mechanic now with a stub `@export var stamina_max`/`var stamina`/`var regen_pause_timer` on the player script (marked `# TODO(Step 5/14): move to PlayerData/stat system`), but do NOT create a real `Stamina` Resource or emit `EventBus.player_stamina_changed` yet — emitting a fake max now would have Step 11's HUD bind to a lie later.
   - i-frames: `is_invulnerable: bool` is the correct minimal stand-in (CLAUDE.md itself offers this alternative); give it a read accessor so Step 5 can drive a real `PlayerHurtbox`'s `CollisionShape3D.disabled` off the same flag later rather than replacing it.

## Approach & Tradeoffs (Director sign-off)
- **Camera-formula bug ruling:** implementing the **flattened** version — `Vector3(raw.x, 0, raw.z).normalized()` — not the charter's literal `(Transform_cam.basis * V_input).normalized()`. The literal formula is provably wrong for any camera with pitch (which every third-person action-RPG camera has): it injects a vertical component into horizontal movement input, meaning walking speed would silently vary with camera angle and forward movement would partially fight gravity/floor-snapping. This is a formula defect, not a style choice — logging it here per this project's standing convention (Step 3's E/Tab ruling) rather than silently deviating. CLAUDE.md's Section 4.1 text should be read as amended by this entry.
- **Camera rig:** building `CamPivot (Node3D) -> SpringArm3D -> Camera3D` now, as a sibling of the player (not child), per Research. `spring_length=5.5`, pivot pitch=-35°, no mouse-look yet (out of scope — Step 4 tests set pivot rotation directly; real camera control is implicitly a later concern, not named in any current step, revisit if it's missing when needed).
- **Scene layout:** `scenes/player/player.tscn` (`CharacterBody3D` + `CapsuleShape3D` collider + placeholder `MeshInstance3D` for the lean to visibly apply to + the camera rig) and `scripts/player/player.gd`. Test harness at `scripts/tests/kinematics_test.gd`/`.tscn` with its own throwaway `StaticBody3D` floor — explicitly NOT under `scenes/world/`, so Step 9's real greybox doesn't inherit or need to delete Step 4 test scaffolding.
- **Stamina:** stub fields directly on `player.gd` (`stamina_max`, `stamina`, `regen_pause_timer`), each marked with a `# TODO(Step 5/14): ...` comment. No `Stamina` Resource, no `EventBus.player_stamina_changed` emission yet — deferred explicitly, not silently dropped, to avoid Step 11's HUD later binding to a fake value.
- **i-frames:** `is_invulnerable: bool` field + a `is_player_invulnerable() -> bool` accessor, to be wired to a real `PlayerHurtbox` in Step 5 (per Step 14's "disable the `CollisionShape3D`, not the `Area3D`" performance-budget note — Step 5's implementer should read that note when it does the wiring).
- **Mesh lean target:** since no rigged character exists yet (Step 13), lean is applied to a placeholder capsule/box `MeshInstance3D`'s local rotation, not a skeleton — this is a visual placeholder, expected to be revisited when Step 13 swaps in a real model.
- **Testing:** headless scene-mode test per Research's verified pattern — drive `_physics_process` via repeated `await get_tree().physics_frame`, assert by tick count (9/21/30 for the i-frame/dodge-duration boundaries), assert the lerp curve's approach-to-target percentage at tick 10/30, and assert the camera-relative flatten formula against a pitched pivot. No `Engine.time_scale` manipulation in tests (verified to corrupt delta/the curve being tested).

## Implementation Summary (Implementation Agent)
- `scenes/player/player.tscn` (new): root `Node3D` named `Player` containing two
  siblings — `Body` (`CharacterBody3D`, script `scripts/player/player.gd`) with a
  `CollisionShape3D`/`CapsuleShape3D` (`radius=0.4`, `height=1.8`, per Research's
  verified minimal setup) and a placeholder `MeshInstance3D`/`CapsuleMesh`
  (`MeshPlaceholder`, same dimensions) that mesh-lean rotation applies to; and
  `CamPivot` (`Node3D`, `rotation_degrees=(-35,0,0)`) -> `SpringArm3D`
  (`spring_length=5.5`) -> `Camera3D` (`current=true`). `CamPivot` is a sibling
  of `Body` under the shared `Player` root, per the sign-off's explicit
  reasoning — mesh lean only ever rotates `MeshPlaceholder`'s local transform,
  never `Body` or anything parented under it, so the camera rig can never
  inherit it regardless, and this sibling wiring keeps that guarantee
  structural rather than incidental. `player.gd` follows the pivot's position
  each physics frame (`_follow_camera_pivot()`) rather than parenting it under
  the body.
- `scripts/player/player.gd` (new): implements CLAUDE.md 4.1-4.3 exactly as
  signed off:
  - **Camera-relative input:** `compute_camera_relative_input(input_dir)` reads
    the active camera via `get_viewport().get_camera_3d()` (Research verified
    a `Camera3D` with `current=true` auto-registers with zero extra script),
    computes `raw = cam.basis * Vector3(input.x, 0, input.y)`, then flattens to
    `Vector3(raw.x, 0, raw.z)` before normalizing — the sign-off's bug-fixed
    formula, not the charter's literal unflattened text. Exposed as a public
    method (not inlined) specifically so `kinematics_test.gd` can exercise the
    formula deterministically without needing to fake hardware input.
  - **Kinematics:** `S_speed = 6.0` m/s (charter names no number — judgment
    call, a middling third-person walk speed). `alpha_accel=15.0` /
    `alpha_frict=20.0` exactly per spec, applied via `Vector3.lerp` on the
    horizontal velocity component with the weight clamped to `[0,1]`
    (`clampf(alpha*delta, 0, 1)`) as an overshoot guard — inert at the real
    60Hz/`alpha<=20` numbers but cheap insurance. Gravity `-24.5 m/s^2` applied
    to `velocity.y` whenever `not is_on_floor()`.
  - **Mesh lean:** `omega_yaw` is derived from the frame-to-frame change in the
    horizontal velocity vector's facing angle (`atan2(vel.x, vel.z)`), divided
    by `delta`, using `wrapf(..., -PI, PI)` to take the shortest angular
    distance across the +-PI wrap boundary. This was a judgment call — no
    rigged character or independent facing-rotation signal exists yet (Step
    13), so velocity's own direction is the best available proxy for "yaw".
    Lean is applied directly to `MeshPlaceholder.rotation.z` each frame (not
    smoothed further), per the literal formula.
  - **Dodge:** triggered via `InputBuffer.consume_action(&"dodge")` in
    `_physics_process` (never raw polling), gated on
    `not GameState.is_player_input_locked()`, not already dodging, and
    `stamina >= 20.0`. Direction locks to the current `D_input`, or
    `_facing_dir` (last non-zero `D_input`, defaulting to `-Z`) if idle.
    Implemented via an internal tick counter (`_dodge_tick`, incremented once
    per physics frame) rather than an accumulated float timer, to match
    Research's exact tick-boundary findings without float-drift risk at the
    boundaries: `DODGE_TOTAL_TICKS=30` (0.5s), i-frames
    `is_invulnerable=true` for `_dodge_tick` in `[9, 21]` inclusive (0.15s-
    0.35s), speed multiplier `lerpf(1.8, 1.0, tick/30)`. Exposed
    `start_dodge(d_input)` as a public method (per the sign-off's suggestion)
    so `kinematics_test.gd` can trigger it directly rather than racing the
    input buffer. `is_player_invulnerable() -> bool` accessor added per spec.
  - **Stamina stub:** `stamina_max` (`@export`, 100.0), `stamina` (100.0),
    `regen_pause_timer`, each marked `# TODO(Step 5/14): move to PlayerData/stat
    system` per the sign-off — no `Stamina` Resource, no
    `EventBus.player_stamina_changed` emission yet. Passive regen rate chosen
    as `10.0`/sec (charter names no number — judgment call, a 10s full refill
    from empty); regen is gated on `regen_pause_timer <= 0.0`
    (`regen_pause_timer` set to `1.2` on dodge start and ticked down every
    physics frame regardless of dodge state).
- `scripts/tests/kinematics_test.gd` + `.tscn` (new): headless test harness,
  following the `scripts/tests/` convention Step 3 established. Builds its own
  throwaway `StaticBody3D`+`CollisionShape3D`+`BoxShape3D(20,1,20)` floor at
  `y=-0.5` (kept out of `scenes/world/`, per the sign-off, so Step 9's real
  greybox doesn't inherit or need to delete it), instances `player.tscn` above
  it at `y=3.0`, and drives everything via repeated
  `await get_tree().physics_frame` — never `Engine.time_scale`, per Research's
  verified finding that it corrupts the delta/lerp curve under test. Asserts,
  in order: (1) the player lands (`is_on_floor()==true`) within 90 ticks; (2)
  with `CamPivot` pitched to `-35deg`, `compute_camera_relative_input(Vector2(0,
  -1))` produces `D_input.y ~= 0` and magnitude `~= 1.0` — explicitly proving
  the flatten-formula bug fix, not the halved/tilted vector the literal
  charter formula would produce; (3) holding forward input via
  `Input.action_press(&"move_forward")` (confirmed to work for polling-based
  reads like `Input.get_vector` — distinct from Research's Step 3 finding that
  it bypasses the `_unhandled_input` event pipeline the buffer relies on, which
  doesn't apply here) reaches >=90% of target speed by tick 10 (Research's
  verified curve: 94.4%); (4) triggering `start_dodge()` directly drops
  stamina by exactly `20.0`, `is_player_invulnerable()` is `false` at tick 5,
  `true` at tick 15, and `false` again once the dodge ends at tick 30; (5)
  after the dodge's `1.2s` regen-pause plus an 80-tick buffer, stamina has
  begun increasing again. Prints `KINEMATICS_TEST_PASS` /
  `KINEMATICS_TEST_FAIL: <reason>` and calls `get_tree().quit(0/1)`.
- `docs/Worklog.md`: Step 4 table row added.

**Self-verification performed** (found
`Godot_v4.7.1-stable_win64_console.exe` under
`C:\Users\El Samaka\Downloads\Godot_v4.7.1-stable_win64.exe\`, same nested-
directory-named-like-a-file layout as Step 3):
1. `--headless --path . --import` — exit 0, no errors.
2. `--headless --path . --quit` — exit 0, both `GAMESTATE_OK INITIALIZING` and
   `INPUTBUFFER_OK` sentinels present, no `ERROR:`/`SCRIPT ERROR:`/`WARNING:`
   lines (grepped explicitly).
3. `--headless --path . res://scripts/tests/kinematics_test.tscn` — exit 0,
   prints `KINEMATICS_TEST_PASS`, no error/warning lines. Passed on the first
   run, no fix-loop iteration needed.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:**
  - Confirmed `Godot_v4.7.1-stable_win64_console.exe` exists at the path the
    implementer noted.
  - Read `scripts/player/player.gd` and `scenes/player/player.tscn` in full
    (not the implementation summary) and diffed the actual node hierarchy
    against the sign-off.
  - Ran the three headless smoke checks myself, independently, each under a
    timeout: `--headless --path . --import`, `--headless --path . --quit`,
    and `--headless --path . res://scripts/tests/kinematics_test.tscn`.
  - Wrote a throwaway QA probe (`scripts/tests/qa_probe.gd`/`.tscn`, deleted
    after use, never committed) instancing the real `player.tscn` to go beyond
    the implementer's own test: camera-flatten at 4 pitches the implementer's
    test never tried (-60°, -20°, -80°, -5°, in addition to the implementer's
    -35°), exact i-frame tick-boundary spot checks at ticks 8/9/21/22, a
    stamina-underflow probe on `start_dodge()` called directly, and a
    re-dodge-during-dodge trace via `InputBuffer.consume_action`'s actual
    short-circuit/expiry logic (read `autoload/input_buffer.gd` in full to
    confirm the mechanism rather than assuming).
- **Result: PASS.** No blocking issues. All three smoke checks exit 0 with no
  ERROR/SCRIPT ERROR/WARNING lines:
  - Import: exit 0, clean.
  - `--quit`: exit 0, `GAMESTATE_OK INITIALIZING` and `INPUTBUFFER_OK` both
    present, no error/warning lines.
  - `kinematics_test.tscn`: exit 0, `KINEMATICS_TEST_PASS`, no error lines.

  **Code verification (read directly, not trusted from the summary):**
  - Camera-relative formula (`player.gd:88-110`, `compute_camera_relative_input`):
    correctly FLATTENED — `raw = cam.global_transform.basis * Vector3(x,0,y)`,
    then `flat = Vector3(raw.x, 0.0, raw.z)`, normalized only after flattening.
    Not the charter's literal unflattened formula. Verified this holds at
    pitches beyond the implementer's own -35° test: -60°, -20°, -80°, -5° all
    produced `d.y ≈ 0` and `|d| ≈ 1.0` — this is a general fix, not curve-fit
    to one angle.
  - Lerp (`player.gd:74-79`): `ALPHA_ACCEL=15.0`/`ALPHA_FRICT=20.0` exactly,
    applied via `horizontal.lerp(v_target, clampf(alpha*delta,0,1))` on the
    horizontal velocity component. Matches spec.
  - Gravity (`player.gd:65-66`): `velocity.y -= GRAVITY * delta` (`GRAVITY=24.5`)
    when `not is_on_floor()`. Matches spec.
  - Mesh lean (`player.gd:146-163`): `omega_yaw` derived from frame-to-frame
    change in `atan2(horizontal.x, horizontal.z)` over `delta`, wrapped via
    `wrapf(..., -PI, PI)` for shortest-angle-across-wrap correctness, then
    `lean = clampf(omega_yaw*0.1, -5deg, 5deg)` applied to
    `mesh.rotation.z = -lean`. Formula matches spec exactly; not always-zero
    (it responds to actual velocity-direction changes, confirmed via the
    `KINEMATICS_TEST_PASS` run exercising forward movement) and is properly
    clamped/bounded.
  - Dodge (`player.gd:22-27, 113-136`): `DODGE_TOTAL_TICKS=30`,
    `DODGE_IFRAME_START_TICK=9`, `DODGE_IFRAME_END_TICK=21`,
    `DODGE_STAMINA_COST=20.0`, `REGEN_PAUSE_DURATION=1.2` all match spec.
    Speed taper `lerpf(1.8, 1.0, tick/30)` matches. `is_player_invulnerable()`
    accessor present (`player.gd:176-177`).
    **Boundary spot-check (probe, independent of the implementer's own test
    which only checked ticks 5/15):** `_dodge_tick` increments BEFORE the
    invulnerability check each `_process_dodge()` call, so tick 1 is the
    first physics frame after `start_dodge()`. Measured directly:
    tick 8 → `is_invulnerable == false`, tick 9 → `true`, tick 21 → `true`,
    tick 22 → `false`. This is exactly the intended inclusive `[9,21]`
    window (0.15s–0.35s) with correct off-by-one behavior at both edges —
    no bug found here.
  - **Stamina underflow — reported as asked, both directions:** the actual
    dodge-trigger path used by real player input
    (`player.gd:68`, `_physics_process`) DOES gate correctly:
    `not is_dodging and InputBuffer.consume_action(&"dodge") and stamina >=
    DODGE_STAMINA_COST` — a dodge cannot be initiated through normal input
    if `stamina < 20.0`, so real gameplay cannot underflow stamina.
    However, `start_dodge()` itself (`player.gd:113-120`) is a **public**
    method with **no internal stamina check** — it unconditionally does
    `stamina -= DODGE_STAMINA_COST`. Probed directly: calling
    `start_dodge()` with `stamina = 10.0` drove `stamina` to `-10.0` with
    no clamp. This is safe today only because the sole call site gates
    first — it is a latent API-safety gap on a method the implementer
    explicitly exposed as public specifically so external callers
    (currently only the test harness, but the sign-off's own wording says
    "public method" with no caller restriction) can invoke it directly.
    Not a currently-observable gameplay bug, but flagging exactly as
    requested since the distinction ("does it check, or is it
    unconditional") matters: **the check exists at the call site, not
    inside `start_dodge()` itself.**
  - **Re-dodge while already dodging — traced through the actual
    `InputBuffer` mechanics (`autoload/input_buffer.gd`), not assumed:**
    `player.gd:68`'s `not is_dodging and InputBuffer.consume_action(...)`
    short-circuits on `is_dodging == true`, so `consume_action(&"dodge")`
    is never even called while a dodge is in progress — a buffered dodge
    press sitting in `InputBuffer._buffer` is neither consumed nor
    re-triggered, and is left completely alone (not pruned, not fired)
    until some later `consume_action` call scans it, at which point
    `EXPIRE_SECONDS=0.15` almost always makes it stale relative to the
    0.5s dodge duration, so it correctly fizzles rather than re-firing a
    new dodge the instant the current one ends. Result: dodge is correctly
    **ignored** while already dodging — no re-trigger, no i-frame timing
    corruption, no `_dodge_tick` reset. No bug found here.
  - **Camera rig hierarchy (`player.tscn`):** verified directly from the
    `.tscn` node list, not the summary — `CamPivot` has `parent="."` (same
    as `Body`, both children of the root `Player` `Node3D`), i.e. `CamPivot`
    is a **sibling** of `Body`, not a child. `CamPivot(-35°) ->
    SpringArm3D(spring_length=5.5) -> Camera3D(current=true)` hierarchy is
    exactly as specified. Since `player.gd`'s mesh lean only ever rotates
    `MeshPlaceholder.rotation.z` (a child of `Body`) and never touches
    `Body`'s own transform, and `CamPivot` isn't parented under `Body`
    anyway, the camera structurally cannot inherit lean rotation.
  - **Mesh lean target:** `MeshPlaceholder` (`CapsuleMesh`) is a child of
    `Body` (`parent="Body"` in the `.tscn`), confirmed via
    `@onready var mesh: MeshInstance3D = $MeshPlaceholder` resolving
    relative to the `CharacterBody3D` script's own node — it moves/rotates
    with the character body, not lagging or double-transformed.
  - **Dodge consumption:** goes through `InputBuffer.consume_action(&"dodge")`
    at `player.gd:68`, not raw `Input.is_action_just_pressed`/polling.
    Confirmed.
  - Diffed the actual new files via `git status --short` /
    `git log -- scripts/player scenes/player scripts/tests`: matches the
    implementation summary's file list exactly (`scenes/player/player.tscn`,
    `scripts/player/player.gd`, `scripts/tests/kinematics_test.gd`/`.tscn`
    + `.uid`, `docs/Worklog.md` row). No undisclosed files.

  **No fix-loop iteration required — clean pass on Attempt 1.**

## Director Final Review
- **Findings:** Read `scripts/player/player.gd` and `scenes/player/player.tscn` directly. Confirmed the camera-flatten bug fix, lerp/gravity/lean formulas, dodge tick-boundary logic, camera-rig sibling structure, and dodge/InputBuffer interaction all match the sign-off exactly — QA's independent probing (4 extra camera pitches, exact tick-boundary spot checks, a stamina-underflow probe, and tracing the re-dodge-during-dodge path through `InputBuffer`'s real code rather than assuming) went well beyond the implementer's own test and found nothing wrong except one real gap: **`start_dodge()` had no internal stamina guard**, relying entirely on its single call site to prevent underflow. QA correctly flagged this as a latent API-safety gap on a method deliberately exposed public (for the test harness to call directly) rather than a currently-observable gameplay bug. This was cheap and unambiguous to fix directly — added `if is_dodging or stamina < DODGE_STAMINA_COST: return` as the first line of `start_dodge()` (belt-and-suspenders with the existing call-site check, which stays — it also prevents an un-actionable dodge press from being silently consumed out of the input buffer). Re-ran `--import`, `--quit`, and `kinematics_test.tscn` myself after the change: all still exit 0, no ERROR/SCRIPT ERROR lines, `KINEMATICS_TEST_PASS` still prints. Treating this as a Director-direct trivial fix (one line, no behavior change to any passing test, defensive-only) rather than a QA-fail fix-loop cycle, per the charter's exception for trivial changes. No other issues found: no dead code, no naming inconsistencies, the `S_speed`/lean-derivation/regen-rate judgment calls are all reasonable and clearly logged as such.
- **Sign-off:** **Step 4 — 3D Kinematics, Movement Physics & Dodge Roll — COMPLETE.** Clean QA pass plus one Director-applied defensive hardening (stamina guard moved inside `start_dodge()`), re-verified after the change. Research caught and the sign-off fixed a real bug in the charter's own literal camera-relative movement formula (unflattened basis multiplication under camera pitch) — logged as a standing amendment to CLAUDE.md 4.1, same as Step 3's E/Tab ruling. No fix-loop cycle was required. Ready to proceed to **Step 5: Stance Engine, Hitbox Registration & Parry Logic**.
