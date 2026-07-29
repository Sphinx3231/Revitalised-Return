# Worklog — Project Return

Running index of every development cycle. One line per step/task, linking to its detail file in `Tasks/`. Update when a task file is opened and again when it's closed.

| Date | Step | Status | Task File |
|------|------|--------|-----------|
| 2026-07-29 | Step 1: Project Initialization & Directory Architecture | Done | [2026-07-29-step-1-project-init.md](Tasks/2026-07-29-step-1-project-init.md) |
| 2026-07-29 | Step 2: Core Game State Machine & Global Event Bus | Done | [2026-07-29-step-2-fsm-eventbus.md](Tasks/2026-07-29-step-2-fsm-eventbus.md) |
| 2026-07-29 | Design Research: Steps 9-14 (Genshin/Elden Ring reference pass) | Locked — Director resolved all Open Questions, condensed spec promoted into CLAUDE.md | [DesignDoc.md](DesignDoc.md) |
| 2026-07-30 | Step 3: Abstracted Input System & Rolling Action Buffer | Done | [2026-07-30-step-3-input-buffer.md](Tasks/2026-07-30-step-3-input-buffer.md) |

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
