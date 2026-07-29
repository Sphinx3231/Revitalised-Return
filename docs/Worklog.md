# Worklog — Project Return

Running index of every development cycle. One line per step/task, linking to its detail file in `Tasks/`. Update when a task file is opened and again when it's closed.

| Date | Step | Status | Task File |
|------|------|--------|-----------|
| 2026-07-29 | Step 1: Project Initialization & Directory Architecture | Done | [2026-07-29-step-1-project-init.md](Tasks/2026-07-29-step-1-project-init.md) |
| 2026-07-29 | Step 2: Core Game State Machine & Global Event Bus | Done | [2026-07-29-step-2-fsm-eventbus.md](Tasks/2026-07-29-step-2-fsm-eventbus.md) |
| 2026-07-29 | Design Research: Steps 9-14 (Genshin/Elden Ring reference pass) | Staged — pending Director sign-off on Open Questions | [DesignDoc.md](DesignDoc.md) |

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
