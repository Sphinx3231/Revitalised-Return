# Worklog — Project Return

Running index of every development cycle. One line per step/task, linking to its detail file in `Tasks/`. Update when a task file is opened and again when it's closed.

| Date | Step | Status | Task File |
|------|------|--------|-----------|
| 2026-07-29 | Step 1: Project Initialization & Directory Architecture | Done | [2026-07-29-step-1-project-init.md](Tasks/2026-07-29-step-1-project-init.md) |
| 2026-07-29 | Step 2: Core Game State Machine & Global Event Bus | Done | [2026-07-29-step-2-fsm-eventbus.md](Tasks/2026-07-29-step-2-fsm-eventbus.md) |
| 2026-07-29 | Design Research: Steps 9-14 (Genshin/Elden Ring reference pass) | Locked — Director resolved all Open Questions, condensed spec promoted into CLAUDE.md | [DesignDoc.md](DesignDoc.md) |
| 2026-07-30 | Step 3: Abstracted Input System & Rolling Action Buffer | Done | [2026-07-30-step-3-input-buffer.md](Tasks/2026-07-30-step-3-input-buffer.md) |
| 2026-07-30 | Step 4: 3D Kinematics, Movement Physics & Dodge Roll | Done | [2026-07-30-step-4-kinematics-dodge.md](Tasks/2026-07-30-step-4-kinematics-dodge.md) |
| 2026-07-30 | Step 5: Stance Engine, Hitbox Registration & Parry Logic | Done | [2026-07-30-step-5-stances-hitboxes.md](Tasks/2026-07-30-step-5-stances-hitboxes.md) |
| 2026-07-30 | Step 6: 3D "Juice" Engine & Impact Feedback | Implementation complete, QA passed — never reached Director sign-off before the engine pivot below; archived as-is under `legacy-godot/` | [2026-07-30-step-6-juice-engine.md](Tasks/2026-07-30-step-6-juice-engine.md) |
| 2026-07-31 | Engine Pivot: Godot 4/GDScript → Unity 6000.5.5f1/C# | Done — Godot work archived to `legacy-godot/`, charter rewritten, Unity skeleton created & QA'd | [2026-07-31-godot-to-unity-pivot.md](Tasks/2026-07-31-godot-to-unity-pivot.md) |
| 2026-07-31 | Step 2 (Unity port): EventBus + GameState — full implementation | Done — event raise-helpers, real SetState() transition table, Bootstrap scene at build index 0, QA passed | [2026-07-31-step-2-unity-eventbus-gamestate.md](Tasks/2026-07-31-step-2-unity-eventbus-gamestate.md) |
| 2026-07-31 | Player Base Character (functional placeholder, Steps 3+4 compressed) | Done — Input Actions asset, S.O.L.I.D.-split movement/dodge/camera scripts, Player.prefab, MovementTest sandbox scene, QA passed after 1 fix loop. User-confirmed working in Play Mode (after a post-signoff GameState-not-Playing bugfix). | [2026-07-31-player-base-character.md](Tasks/2026-07-31-player-base-character.md) |
| 2026-07-31 | UI Systems (Phase 2: reactive HUD + MainMenu, Step 11 spec) | Done — S.O.L.I.D.-split HUD (vitals/stance diamond/notices), MainMenu+Settings stub, 4 placeholder StanceData assets, QA passed. Supersedes the paused ui-systems-skeleton task's HUD/menu deliverables. **Play Mode not yet manually verified.** | [2026-07-31-ui-systems-phase2.md](Tasks/2026-07-31-ui-systems-phase2.md) |

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

## Step 5: Stance Engine, Hitbox Registration & Parry Logic - 2026-07-30

### 🧪 Tests
Objective: prove stance-data-driven combat math, hitbox/hurtbox collision resolution (parry/block/hit priority), and the two engine bugs Research found (sub-tick active windows, self-hit without layer separation) are all handled correctly against real Godot physics, not mocked. Method: `--import`/`--quit` sentinel check plus a dedicated `scripts/tests/combat_test.tscn` using two real `combatant.tscn` actors and `AnimationPlayer` method-call tracks. QA independently re-verified everything from code (not the implementer's summary), re-confirmed the implementer's own newly-discovered `monitoring`-toggle timing subtlety via a from-scratch probe, and went further: geometric self-hit attempts with layers deliberately defeated, real double-window dedup (not just a synthetic repro), simultaneous parry+block priority, and posture-floor/edge-trigger behavior. Also re-ran `kinematics_test.tscn` and `input_buffer_test.tscn` as regression checks since Step 5 touched `player.tscn`/`player.gd`/`project.godot`. Outcome: **PASS**, clean on Attempt 1, no fix-loop cycle. Two non-blocking gaps logged as debt, not fixed this cycle (see Bugs/Cause). See task file for the full QA report.

### 🔄 Changes
- Extended `resources/stance_data.gd` with the 5 real `@export` properties (deliberately invalid/neutral defaults, to avoid `ResourceSaver`'s default-omission gotcha Research found). Added 4 hand-authored `.tres` stances under `resources/stances/` (Stone/Water/Flame/Wind), each with explicit values reflecting their design-text identity (Stone: heavy posture damage/slow/wide parry window; Water: rapid/low-cost; Flame: wide-arc cleave; Wind: highest deflection/most generous parry window) — exact numbers are Director-approved judgment calls, charter names no numbers.
- Updated `project.godot`: new `[layer_names]` section naming 3D physics layers 6/7/8 as `player_hurtbox`/`enemy_hurtbox`/`hitbox`.
- Added `scripts/combat/combatant.gd` + `scenes/combat/combatant.tscn`: `Combatant` class (health/max_health/posture/max_posture/armor stubs, `# TODO(Step 11/14)` marked) with a hurtbox `Area3D` (`monitoring=false`/`monitorable=true`) and hitbox `Area3D` (`monitorable=false`, mask targeting only the opposing hurtbox layer — eliminates self-hit and hitbox-vs-hitbox structurally), `enable_hitbox()`/`disable_hitbox()` driven by real `AnimationPlayer` method-call tracks (works headlessly with zero animated properties, verified by Research), a `_hit_this_window` dedup set cleared on each `enable_hitbox()`, and `area_entered` resolution implementing parry-check → block-check → hit-check in that exact priority order with CLAUDE.md 5.2's formulas (`ArmorMitigation = Armor/(Armor+100)`, block = 80% health mitigation + full posture damage, parry = attacker's posture -= 40% of attacker's max). Emits `EventBus.entity_damaged`/`posture_broken`/`parry_executed` — never a fake-max `player_*_changed` signal. `is_blocking` is pure exposed state (public setter, no input wiring — see Bugs/Cause #1 below); `is_parrying` uses Step 4's tick-counter pattern, consuming `InputBuffer.consume_action(&"parry")`.
- Updated `scenes/player/player.tscn`/`scripts/player/player.gd`: player gained a composed `Hurtbox` (layer `player_hurtbox`, not a full `Combatant` — sign-off was explicit that retrofitting `Player` into `Combatant` is out of scope). `is_invulnerable` toggling now also disables/enables the hurtbox's `CollisionShape3D` via a new `_set_invulnerable()` helper, replacing all three prior direct-assignment call sites from Step 4.
- Added `scripts/tests/combat_test.gd` + `.tscn`: headless test covering normal hit, parry, block, the sub-tick-window authoring constraint, self-hit prevention, and double-window dedup.
- Updated this Worklog and `docs/Tasks/2026-07-30-step-5-stances-hitboxes.md`.

### 🔴 Bugs/Cause
Two real engine-behavior bugs caught by Research before implementation (never shipped in buggy form): (1) an `AnimationPlayer`-driven hitbox active window shorter than ~1 physics tick registers zero hits — mitigated via a documented >=3-tick (0.05s) authoring floor, not runtime-enforced; (2) hitbox/hurtbox sharing one collision layer lets an attacker hit their own hurtbox — fixed via layer separation. One real charter gap: CLAUDE.md 5.2 references a "blocking" state but Step 3's InputMap has no `block` action — ruled to implement `is_blocking` as pure exposed state, unwired to input, logged as a standing charter gap for a future step/amendment to resolve.

Two non-blocking gaps found by QA, logged as carried-forward debt rather than fixed this cycle: (1) `posture_broken` is not edge-triggered — re-fires on every hit once posture is already <=0, not just the crossing; future consumers (Step 6/8) must debounce themselves. (2) Every `Combatant` independently consumes the shared global `InputBuffer`'s `"parry"` action — harmless today (no enemy exists, `Player` isn't a `Combatant`), but a real trap once Step 7's enemy AI extends `combatant.gd`, since every enemy would react to the player's own parry press. Deliberately not fixed blind this cycle — the correct fix depends on Step 7's actual enemy-AI structure, which doesn't exist yet; flagged as a hard requirement for Step 7's task intake instead.

### 🛠️ Fix/Prevention
N/A this step for the two Research-caught bugs (designed around before implementation). The two QA-found gaps are intentionally deferred with an explicit owner (Step 7's intake) rather than guessed at now — see Bugs/Cause.

### 💪 Game Feel Wins
First real combat resolution in the project: stance-driven damage/posture math, a working parry (with a real attacker-posture punish) and block (partial mitigation, full posture bleed-through), and hitbox timing tied to genuine `AnimationPlayer` tracks rather than a placeholder timer — meaning Step 13's real animations can drive this exact mechanism with zero rework.

## Engine Pivot: Godot 4/GDScript → Unity 6000.5.5f1/C# - 2026-07-31

### 🧪 Tests
Objective: prove the new Unity project skeleton actually runs headlessly, not just compiles, and matches the rewritten charter's folder/stub spec exactly. Method: `Unity.exe -batchmode -nographics -projectPath <repo root> -executeMethod Ping.Run -quit -logFile <log>`, independently re-run by QA (not trusting the implementer's self-report), plus an independent `find`-based walk of all 27 `Assets/` directories and a manual read-through of every stub script against CLAUDE.md 2.1/2.2. Outcome: **PASS** — exit 0, `PING_OK` present, zero `error CS`, all directories present, all stub scripts match spec. See task file for full QA report.

### 🔄 Changes
- Archived the entire Godot 4 implementation (`project.godot`, `icon.svg(.import)`, `autoload/`, `assets/`, `resources/`, `scenes/`, `scripts/`, including Step 6's never-committed juice-engine files) into `legacy-godot/` via `git mv`, with a new `legacy-godot/README.md` explaining the archive. Nothing deleted.
- Rewrote `.gitignore`: added standard Unity ignores (`Library/`, `Temp/`, `Obj/`, IDE files, etc.), narrowed the old bare `.godot/` rule to `legacy-godot/.godot/`.
- Rewrote `CLAUDE.md` in full: converted every Godot-specific API/architecture reference (autoloads, `Area3D`, `AnimationPlayer` tracks, `Resource`/`.tres`, signals, `Engine.time_scale`, folder structure, Director/subagent workflow's tooling references) to Unity/C# equivalents. Section 1 (World & Acts) and the Steps 9-14 locked design decisions are unchanged — only their data-structure notation was translated, not the decisions themselves.
- Created a real Unity 6000.5.5f1 project at the repo root (`-createProject` targeting the existing non-empty repo root worked directly, no temp-and-move workaround needed) with the charter's full 27-directory `Assets/` structure, `Assets/Editor/Ping.cs` (NinjaGame's exact smoke-check convention), and Step-1-plus-stub-Step-2 skeleton scripts: `Assets/Scripts/Systems/EventBus.cs` (12 C# events per charter 2.1, declarations only), `Assets/Scripts/Systems/GameState.cs` (7-value `State` enum + stub `SetState()`/`IsPlayerInputLocked()`, `TODO(Step 2)` marked), `Assets/Scripts/Combat/StanceData.cs` (empty `ScriptableObject` stub, `TODO(Step 5)` marked).
- Updated this Worklog and `docs/Tasks/2026-07-31-godot-to-unity-pivot.md`.

### 🔴 Bugs/Cause
None. Clean pass on Attempt 1, no fix-loop cycle.

### 🛠️ Fix/Prevention
N/A this step.

### 💪 Game Feel Wins
N/A — this is an engine/tooling pivot, not a gameplay-system step. The real Step 1-6 game-feel numbers (dodge i-frame ticks, parry windows, hit-stop duration, camera trauma decay) are preserved as a porting reference at `legacy-godot/` and named explicitly in `CLAUDE.md`'s "Current status" as what future Unity ports should carry forward rather than re-derive.
