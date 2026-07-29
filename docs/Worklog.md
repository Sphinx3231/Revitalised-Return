# Worklog — Project Return

Running index of every development cycle. One line per step/task, linking to its detail file in `Tasks/`. Update when a task file is opened and again when it's closed.

| Date | Step | Status | Task File |
|------|------|--------|-----------|
| 2026-07-29 | Step 1: Project Initialization & Directory Architecture | Done | [2026-07-29-step-1-project-init.md](Tasks/2026-07-29-step-1-project-init.md) |
| 2026-07-29 | Step 2: Core Game State Machine & Global Event Bus | Done | [2026-07-29-step-2-fsm-eventbus.md](Tasks/2026-07-29-step-2-fsm-eventbus.md) |
| 2026-07-29 | Design Research: Steps 9-14 (Genshin/Elden Ring reference pass) | Locked — Director resolved all Open Questions, condensed spec promoted into CLAUDE.md | [DesignDoc.md](DesignDoc.md) |
| 2026-07-30 | Step 3: Abstracted Input System & Rolling Action Buffer | Done | [2026-07-30-step-3-input-buffer.md](Tasks/2026-07-30-step-3-input-buffer.md) |
| 2026-07-30 | Step 4: 3D Kinematics, Movement Physics & Dodge Roll | Done | [2026-07-30-step-4-kinematics-dodge.md](Tasks/2026-07-30-step-4-kinematics-dodge.md) |

## Step 1: Project Initialization & Directory Architecture - 2026-07-29

### 🧪 Tests
Objective: confirm the project opens and boots cleanly as a real Godot 4.7 project (not just a folder scaffold). Method: headless `--import` then `--headless --quit` smoke-test via `Godot_v4.7.1-stable_win64_console.exe`. Outcome: **PASS** — both commands exit 0, no ERROR/SCRIPT ERROR lines, `.godot/` cache created correctly. See task file for full QA report.

### 🔄 Changes
- Added `project.godot` at repo root: `config_version=5`, app name/description, `run/main_scene="res://scenes/main.tscn"`, `config/features=PackedStringArray("4.7", "Forward Plus")`, `config/icon="res://icon.svg"`, canvas_items/expand stretch settings, `3d/physics_engine="Jolt Physics"`, `renderer/rendering_method="forward_plus"`, `rendering_device/driver.windows="d3d12"`, `textures/vram_compression/import_s3tc_bptc=true`. No `[autoload]` section (out of scope for Step 1).
- Fetched Godot's stock `DefaultProjectIcon.svg` (4.7-stable) from the godotengine/godot GitHub repo and saved as `icon.svg` at repo root, matching the `config/icon` reference above.
- Added `scenes/main.tscn`: a single `Node3D` root named `Main`, no script, wired via `run/main_scene`.
- Rounded out `assets/` with snake_case subfolders per the charter: `assets/meshes/`, `assets/materials/`, `assets/textures/`, `assets/audio/`, `assets/shaders/`, each containing a `.gitkeep`. Left the parent `assets/.gitkeep` in place.
- Updated this Worklog: replaced the empty table row with a real Step 1 entry and appended this log entry.

### 🔴 Bugs/Cause
None.

### 🛠️ Fix/Prevention
None.

### 💪 Game Feel Wins
N/A — no gameplay implemented in this step.

## Step 2: Core Game State Machine & Global Event Bus - 2026-07-29

### 🧪 Tests
Objective: verify EventBus/GameState autoloads register and boot without error, and that the StanceData forward-reference doesn't break parsing. Method: `--import` then `--headless --quit` via `Godot_v4.7.1-stable_win64_console.exe`, grepped for ERROR/SCRIPT ERROR, checked for `GAMESTATE_OK INITIALIZING` sentinel. Outcome: **PASS** — both exit 0, no errors, sentinel present, StanceData registered as global class. See task file for full QA report.

### 🔄 Changes
- Added `autoload/event_bus.gd`: `EventBus` singleton with the three signal groups (Player & Vital, Combat & Damage, World & UI) verbatim per CLAUDE.md 2.1, plus `process_mode = Node.PROCESS_MODE_ALWAYS` set in `_ready()`.
- Added `resources/stance_data.gd`: minimal 2-line `StanceData` stub (`class_name StanceData` / `extends Resource`, no properties) so `EventBus.gd`'s `stance_swapped(new_stance_resource: StanceData)` signal parses ahead of Step 5's full implementation.
- Added `autoload/game_state.gd`: `GameState` singleton with the `State` enum (`INITIALIZING, MAIN_MENU, PLAYING, PAUSED, DIALOGUE, CUTSCENE, GAME_OVER`), `current_state` var, `set_state(new_state)` transition logic (mouse-mode + pause rules per CLAUDE.md 2.2), `process_mode = Node.PROCESS_MODE_ALWAYS` in `_ready()`, the new `is_player_input_locked() -> bool` helper, and a `GAMESTATE_OK` print sentinel for QA.
- Updated `project.godot`: appended a new `[autoload]` section registering `EventBus="*res://autoload/event_bus.gd"` and `GameState="*res://autoload/game_state.gd"`, after the existing `[rendering]` section, with no other sections disturbed.
- Updated this Worklog: added the Step 2 table row and this log entry.

### 🔴 Bugs/Cause
None.

### 🛠️ Fix/Prevention
None.

### 💪 Game Feel Wins
N/A

## Step 3: Abstracted Input System & Rolling Action Buffer - 2026-07-30

### 🧪 Tests
Objective: prove the InputMap loads correctly and the rolling buffer's ingestion/expiry/single-consume logic actually works, not just parses. Method: `--import` then `--headless --quit` via `Godot_v4.7.1-stable_win64_console.exe` (grepped for ERROR/SCRIPT ERROR, checked `GAMESTATE_OK`/`INPUTBUFFER_OK` sentinels), plus a dedicated `scripts/tests/input_buffer_test.tscn` sentinel scene using `Input.parse_input_event()` to simulate a joypad press. QA independently re-ran all of this from scratch (not relying on the implementer's self-report) and additionally probed: a second `consume_action()` call right after a successful consume (must return `false` — proves removal, not just a read), and `buffer_action()` while `GameState` is `INITIALIZING`/`PAUSED`/`DIALOGUE` (must no-op). Outcome: **PASS** — all sentinels present, no errors, both extra QA probes behaved correctly. See task file for full QA report.

### 🔄 Changes
- Added `autoload/input_buffer.gd`: `InputBuffer` singleton (`extends Node`, no `class_name`, matching Step 2's convention), `process_mode = Node.PROCESS_MODE_ALWAYS` in `_ready()` + `INPUTBUFFER_OK` sentinel. Implements `buffer_action(action)` (no-ops while `GameState.is_player_input_locked()`), `consume_action(action) -> bool` (scans newest-to-oldest, removes and returns validity for the first match, discarding any expired entries it passes over first), and `clear()`. `_unhandled_input(event)` ingests `light_attack`/`heavy_attack`/`parry`/`dodge` via `event.is_action_pressed()` — deliberately not `Input.is_action_just_pressed()` polling, which Research verified triple-fires on multi-event frames.
- Updated `project.godot`: hand-authored the full `[input]` section (all 10 actions from CLAUDE.md 3.1, keyboard/mouse + gamepad each) and registered `InputBuffer="*res://autoload/input_buffer.gd"` in `[autoload]`. **Deliberate deviation from CLAUDE.md's literal text:** `stance_prev`'s keyboard binding is `Tab`, not `E` — the charter as written double-binds keyboard `E` to both `stance_prev` and `interact`, a real conflict caught by Research. Gamepad `stance_prev` (LT) is untouched. Logged as a Director ruling in the task file, not silently changed.
- Added `scripts/tests/input_buffer_test.gd` + `.tscn` (new `scripts/tests/` convention — didn't exist before this step): headless-runnable sentinel proving buffer ingestion + 0.15s expiry + single-consume, printing `INPUT_BUFFER_TEST_PASS`/`FAIL`.
- Updated this Worklog and `docs/Tasks/2026-07-30-step-3-input-buffer.md`.

### 🔴 Bugs/Cause
None in the shipped implementation. The implementer's *first* run of `input_buffer_test.tscn` failed, but the root cause was a test-authoring bug (forgot to set `GameState` to `PLAYING` before buffering, so `is_player_input_locked()` correctly dropped every input) — not an `InputBuffer` defect. Fixed by the implementer before handoff to QA.

### 🛠️ Fix/Prevention
N/A (no shipped-code bug this step).

### 💪 Game Feel Wins
N/A — no gameplay/juice systems yet; this step is pure input plumbing consumed by Steps 4/5.

## Step 4: 3D Kinematics, Movement Physics & Dodge Roll - 2026-07-30

### 🧪 Tests
Objective: prove camera-relative movement, lerped accel/friction, gravity, mesh lean, and dodge/i-frame timing all behave per the exact formulas/tick counts specified, against real Godot physics (not a pure-math mock). Method: `--import`/`--quit` smoke test (checked `GAMESTATE_OK`/`INPUTBUFFER_OK` sentinels), plus a dedicated `scripts/tests/kinematics_test.tscn` driven by real `await get_tree().physics_frame` ticks against a throwaway floor. QA independently re-verified all of it from scratch and went further: tested the camera-flatten fix at 4 additional pitches beyond the implementer's own -35° test, spot-checked the exact i-frame tick boundaries (8/9/21/22) for off-by-one errors, probed `start_dodge()` directly for stamina underflow, and traced the re-dodge-while-dodging path through `InputBuffer`'s real consume/expiry logic. Outcome: **PASS** — clean on Attempt 1, no fix-loop cycle. One latent (non-blocking) gap found and closed directly by the Director afterward (see Bugs/Cause). See task file for the full QA report.

### 🔄 Changes
- Added `scenes/player/player.tscn`: `CharacterBody3D` (`Body`) + `CapsuleShape3D` collider + placeholder `MeshInstance3D` (`CapsuleMesh`) that mesh-lean rotates, plus a `CamPivot(Node3D) -> SpringArm3D(length 5.5) -> Camera3D` rig wired as a **sibling** of `Body` (not a child), so the camera structurally cannot inherit mesh-lean rotation regardless of what `Body`'s script does.
- Added `scripts/player/player.gd`: camera-relative movement using a **flattened** basis-multiply formula (`Vector3(raw.x, 0, raw.z).normalized()`) — a deliberate fix to a real bug in CLAUDE.md 4.1's literal (unflattened) formula, which Research proved injects a vertical component and shortens horizontal magnitude under any camera pitch. Also implements the lerped accel/friction (`alpha_accel=15`/`alpha_frict=20`), gravity (`24.5`), mesh lean (`-clamp(omega_yaw*0.1, ±5deg)`, `omega_yaw` derived from velocity-facing-angle delta since no rigged character exists yet), and dodge (tick-counted i-frames `[9,21]` inclusive = 0.15s-0.35s at 60 physics ticks/sec, speed taper 1.8x→1.0x over 30 ticks, 20.0 stamina cost, 1.2s regen-pause) consumed via `InputBuffer.consume_action(&"dodge")`. Stamina is a stub (`stamina_max`/`stamina`/`regen_pause_timer`, `# TODO(Step 5/14)` marked) — no real `Stamina` Resource or `EventBus` emission yet, to avoid Step 11's HUD later binding to a fake value. `S_speed=6.0` m/s and regen rate `10.0`/sec are both judgment calls (charter names no numbers for either).
- Added `scripts/tests/kinematics_test.gd` + `.tscn`: headless harness with its own throwaway floor (kept out of `scenes/world/`, which Step 9's real greybox will own), asserting landing, the camera-flatten fix, the lerp curve, and dodge/stamina/regen timing via real physics ticks.
- Updated this Worklog and `docs/Tasks/2026-07-30-step-4-kinematics-dodge.md`.

### 🔴 Bugs/Cause
Two things worth recording, neither a shipped-code defect that reached players:
1. **Charter formula bug (caught pre-implementation, not shipped):** CLAUDE.md 4.1's literal camera-relative formula is wrong for any pitched camera — see Changes above. Fixed at the design stage via the sign-off, never actually implemented in its buggy form.
2. **Latent API-safety gap (caught by QA, closed by the Director post-QA):** `start_dodge()` was implemented as a public method with no internal stamina check — only its single call site in `_physics_process` gated on `stamina >= 20.0`. Not an observable gameplay bug today (the only caller already guards it), but any future direct caller could have driven `stamina` negative. Root cause: the guard was written at the call site (correct, for a different reason — it also prevents consuming a buffered dodge press that can't take effect) but not defensively duplicated inside the method itself.

### 🛠️ Fix/Prevention
For gap #2: added `if is_dodging or stamina < DODGE_STAMINA_COST: return` as the first line of `start_dodge()`, kept the call-site check as-is (still needed so an un-actionable dodge press isn't silently eaten from `InputBuffer`). Re-ran all three headless checks (`--import`, `--quit`, `kinematics_test.tscn`) after the change — all still pass clean. Prevention: any future method exposing a resource-consuming action publicly should guard its own preconditions internally, not rely solely on being well-behaved at its current call site(s).

### 💪 Game Feel Wins
First real physical movement in the project — camera-relative walk/run with inertia (not instant-stop/instant-start), a dodge with a genuine speed burst and i-frame window, and a subtle mesh lean on direction changes. All still placeholder-mesh/no-animation (Step 13 territory), but the underlying feel curves (lerp rates, dodge taper) are now real, tested numbers rather than guesses.
