# Step 1: Project Initialization & Directory Architecture — 2026-07-29

## Task Brief (Director)
- **Goal:** Turn the current bare folder scaffold (CLAUDE.md + empty autoload/scenes/scripts/resources/assets/docs dirs) into an actual openable Godot 4 project, in 3D, matching the folder architecture and conventions in CLAUDE.md Section 4 Step 1 and the STAGE A description.
- **Affected systems:** Repo root (new `project.godot`, `icon.svg` or similar), `autoload/`, `scenes/`, `scripts/`, `resources/`, `assets/` (subfolder conventions if any), `docs/Worklog.md`.
- **Constraints:**
  - Must be Godot 4.x, 3D (not 2D) — per CLAUDE.md's explicit 3D note.
  - No autoload scripts should be registered yet — `GameState.gd`/`EventBus.gd` are Step 2 scope, not Step 1. Step 1 is directory + project bootstrap only.
  - A portable Godot 4.7.1 executable exists locally at `C:\Users\El Samaka\Downloads\Godot_v4.7.1-stable_win64.exe\Godot_v4.7.1-stable_win64.exe` — confirm this is usable for headless project creation/verification, or find another install.
  - Don't invent scope beyond what CLAUDE.md's Step 1 describes (directory architecture + initializing the worklog) — resist the urge to scaffold Step 2+ content early.
- **Definition of done:**
  - `project.godot` exists, configured for 3D rendering, opens without error.
  - Folder structure present and matches CLAUDE.md's documented layout.
  - Project can be validated headlessly (loads/quits cleanly, exit code 0, no error-level output) — establishes the QA convention for all future steps.
  - `docs/Worklog.md` has its first real entry for Step 1.
  - This task file fully filled in through Director sign-off.

## Research Findings (Research Agent)
- Godot 4 has no CLI project-creation flag; a project is just a hand-authored `project.godot` (`--path` requires the file to already exist). Verified against the 4.7 command-line docs and `4.7-stable` source (`project_settings.h`, `project_dialog.cpp`, `editor_node.cpp`).
- `config_version=5` for all Godot 4.0–4.7 projects (`core/config/project_settings.h`).
- Godot has no "2D vs 3D" project toggle — "3D" means the Forward+ renderer (`rendering/renderer/rendering_method="forward_plus"`, the desktop default and correct choice for an isometric/third-person 3D action-RPG) plus 3D root nodes. 4.7's new-project defaults also set `physics/3d/physics_engine="Jolt Physics"` and `rendering_device/driver.windows="d3d12"` on Windows — confirmed against real 4.7 demo projects (`godot-demo-projects/3d/platformer`, `3d/physics_tests`).
- `icon.svg` is optional (default `application/config/icon` is `""`); if referenced it must exist, otherwise omit both the file and the settings key. No Godot-3-style `default_env.tres`/default-environment mechanism exists in Godot 4 — removed in 4.0; 3D scenes get sky/ambient from an in-scene `WorldEnvironment` node (not needed for Step 1's empty scaffold).
- Two distinct, non-interchangeable headless checks: `--headless --path <dir> --import` (works with no main scene — validates the asset pipeline/import) and `--headless --path <dir> --quit` (requires `application/run/main_scene` set, or hard-fails with exit code != 0 and "no main scene defined"). Both are cheap and both matter — `--import` alone can't prove the project actually boots.
- Use `Godot_v4.7.1-stable_win64_console.exe` (not the plain `.exe`) for all CLI/QA work — the non-console exe detaches from the console on Windows and won't reliably surface stdout/stderr to a capturing process. Verified installed build via `--version` → `4.7.1.stable.official.a13da4feb`.
- Godot style guide mandates snake_case for all file/folder names (case-sensitive exported filesystem) — PascalCase is for node names only.
- Full findings with sources logged in the Research Agent's report (session history); key sources: docs.godotengine.org command-line tutorial, renderers overview, ProjectSettings class ref, project-organization style guide; godotengine/godot `4.7-stable` source tree; godotengine/godot-demo-projects.

## Approach & Tradeoffs (Director sign-off)
Approved approach, matching the Research Agent's recommendation:
1. Hand-author `project.godot` at repo root mirroring a real 4.7.1 Forward+/Jolt Windows project (config_version=5, `rendering_method=forward_plus`, `3d/physics_engine=Jolt Physics`, `d3d12` driver, `import_s3tc_bptc=true`, `window/stretch` block). No `[autoload]` section — that's Step 2's scope, not this one.
2. Ship the stock `icon.svg` (Godot's default project icon) and reference it via `config/icon` — keeping both together per the research (never reference without shipping, or ship neither).
3. Add a minimal `scenes/main.tscn` — a single `Node3D` root named `Main`, no script — wired via `run/main_scene`. Judgment call flagged by Research: this is arguably a hair beyond "folder structure only," but without it we can only ever run the weaker `--import` check, never prove the project actually boots. Worth the three lines. Accepted.
4. Round out `assets/` with the charter's documented subfolders (`meshes/materials/textures/audio/shaders`), each `.gitkeep`-ed, snake_case throughout.
5. Establish the two-command headless smoke-test convention (`--import` then `--quit` on the `_console.exe` binary, pass = exit 0 + no `ERROR:`/`SCRIPT ERROR:` lines) as the standing QA gate for this and every future step — this project's equivalent of the sibling project's `Ping.cs`/`PING_OK` convention, but expressed as a CLI check rather than a custom script since Godot's own flags already cover it.
6. Do not set Step 4's gravity value (`24.5`) or anything else belonging to later steps — leave physics/gravity at engine defaults for now.

Tradeoff accepted: hand-authoring `project.godot` instead of using the GUI project manager means it must be kept faithful to what the real Project Manager would generate (done above) rather than drifting into a hand-rolled config; this is the only way to do it non-interactively/headlessly at all, per Research.

## Implementation Summary (Implementation Agent)
- Created `project.godot` at repo root exactly per the approved approach: `config_version=5`, Forward+ renderer, Jolt Physics, d3d12 driver on Windows, `import_s3tc_bptc=true`, `run/main_scene="res://scenes/main.tscn"`, no `[autoload]` section.
- Fetched Godot's stock `DefaultProjectIcon.svg` (4.7-stable) from the godotengine/godot repo and saved as `icon.svg` at repo root, matching `config/icon` in project.godot.
- Created `scenes/main.tscn`: single `Node3D` root named `Main`, no script.
- Added snake_case `assets/` subfolders (`meshes/materials/textures/audio/shaders`), each `.gitkeep`-ed; kept parent `assets/.gitkeep`.
- Updated `docs/Worklog.md` with a Step 1 table row and full log entry.
- Did not touch autoloads, CLAUDE.md, `.gitignore`, or Step 4+ settings (gravity etc.) — correctly out of scope.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** `--headless --path <repo> --import` (asset pipeline / project load) then `--headless --path <repo> --quit` (runtime boot into `main.tscn`), both via `Godot_v4.7.1-stable_win64_console.exe`. Sanity-checked `.godot/` cache creation and file contents against spec.
- **Result:** Clean pass. Both commands exit code 0, no `ERROR:`/`SCRIPT ERROR:` lines. `.godot/` cache created with imported icon. All file contents matched spec exactly. QA also confirmed the `--import` + `--quit` two-command convention is friction-free and reusable as the standing smoke-test gate for future steps.

## Director Final Review
- Reviewed the actual diff (`project.godot`, `scenes/main.tscn`, `assets/*` subfolders, `docs/Worklog.md`) directly — matches the approved approach with no drift and no scope creep into Step 2 (no autoload registration, no gravity/physics tuning beyond the engine/new-project defaults).
- `.godot/` correctly stays untracked (already covered by `.gitignore`); `icon.svg.import` is a normal tracked Godot import artifact, correctly included.
- No edge cases apply yet at this scope (no gameplay, no save state, no pause behavior to check) — those checks become relevant starting Step 2 (GameState FSM) and Step 4 (dodge i-frames / pause).
- QA passed clean on first attempt — no fix loop needed.
- **Sign-off:** Step 1 is complete. Directory architecture and a real, openable, headlessly-verifiable Godot 4.7 3D project now exist. Ready to proceed to Step 2 (Core Game State Machine & Global Event Bus) in a future cycle.
