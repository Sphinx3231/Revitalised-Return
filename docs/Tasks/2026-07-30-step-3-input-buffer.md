# Step 3: Abstracted Input System & Rolling Action Buffer — 2026-07-30

## Task Brief (Director)
- **Goal:** Implement Step 3 of the 14-step pipeline per CLAUDE.md Section 3.1/3.2 —
  (a) configure the full `InputMap` (keyboard/mouse + gamepad bindings) for
  `move_forward/back/left/right`, `light_attack`, `heavy_attack`, `parry`, `dodge`,
  `stance_next`, `stance_prev`, `interact`; (b) build a rolling 0.15s input buffer
  that captures `light_attack`/`heavy_attack`/`parry`/`dodge` presses during
  animation wind-up/recovery windows and replays them once the active state
  permits, per the exact data structure and timing rules in CLAUDE.md 3.2.
- **Affected systems:** `project.godot` (InputMap section), new `autoload/input_buffer.gd`
  (or equivalent per Research's recommendation on singleton vs. component), reads
  `GameState.is_player_input_locked()` from Step 2 where relevant.
- **Constraints:**
  - Must not guess Godot 4.7 InputMap/`InputEventAction` API behavior — verify
    against docs.
  - No combat/animation system exists yet (Steps 4-6 are unimplemented) — Step 3
    delivers the buffer *mechanism* only; it has nothing to consume/replay
    against yet, so QA must validate the buffer's own ingestion/expiry logic in
    isolation (unit-test style), not full combat behavior.
  - Must follow the existing autoload conventions from Step 2 (`process_mode`,
    `class_name` usage, `_ready()` patterns) for consistency.
- **Definition of done:**
  - All 10 actions present in `project.godot`'s `[input]` section with both
    keyboard/mouse and gamepad events bound as specified.
  - A buffer system exists implementing the `Array[Dictionary]` structure
    (`{"action": StringName, "timestamp": float}`), ingesting on
    `_unhandled_input`, and exposing a way to check/consume a buffered action
    within the 0.15s expiry window.
  - Headless `--import` + `--headless --quit` smoke test passes with no
    ERROR/SCRIPT ERROR lines; a dedicated buffer-logic test (GUT or a headless
    script sentinel) proves ingestion + expiry timing.

## Research Findings (Research Agent)
All verified empirically against the real `Godot_v4.7.1-stable_win64_console.exe` in throwaway probe projects (not from docs alone):

1. **InputMap format:** hand-author the `[input]` section in `project.godot` directly (fully diffable/version-controlled, deterministic, confirmed to load headless via `InputMap.has_action()`). Runtime `InputMap.add_action()` calls are the wrong seam — invisible to the editor, re-added every boot. Format per action:
   ```
   light_attack={
   "deadzone": 0.2,
   "events": [Object(InputEventMouseButton,"button_index":1,"script":null)
   , Object(InputEventJoypadButton,"button_index":2,"script":null)
   ]
   }
   ```
   `"script":null` required. Use `physical_keycode` (layout-independent), not `keycode`. Verified constants: `KEY_W/A/S/D/F/Q/E/SPACE = 87/65/83/68/70/81/69/32`; `MOUSE L/R = 1/2`; `JOY_BUTTON A/B/X/Y = 0/1/2/3`, `LB/RB = 9/10`, `DPAD_UP = 11`; `JOY_AXIS LEFT_X/LEFT_Y = 0/1`, `TRIGGER_LEFT = 4`. **`stance_prev`'s Controller LT and the 4 move actions' stick bindings are `InputEventJoypadMotion` (axis+axis_value), not buttons.**

2. **Buffer location:** `autoload/input_buffer.gd`, singleton `InputBuffer`, matching Step 2's conventions exactly (`extends Node`, no `class_name`, `process_mode = PROCESS_MODE_ALWAYS`, `print("INPUTBUFFER_OK")` sentinel in `_ready()`). Verified autoloads DO receive `_unhandled_input` automatically (no `set_process_unhandled_input()` needed), including while `get_tree().paused`, given `PROCESS_MODE_ALWAYS`. **Gotcha:** `_unhandled_input` propagates in reverse tree order — autoloads are root's earliest children, so the buffer is the *last* receiver; anything calling `set_input_as_handled()` upstream starves it. Correct for a buffer (only unconsumed input should queue), but Step 4/5 implementers must never consume these 4 actions upstream of the buffer.

3. **Detection mechanics:** verified `Input.is_action_just_pressed()` polled inside `_unhandled_input` triple-fires when 3 events arrive in one frame. Must use `event.is_action_pressed("action_name")` on the event object itself — verified fires exactly once. Echo events already excluded by default.

4. **Headless test simulation:** `Input.parse_input_event(ev)` verified working fully headless (no window, no joypad connected) for mouse/key/joypad-button/joypad-axis events, all correctly delivered to `_unhandled_input`. `Input.action_press()` verified WRONG — bypasses the event pipeline entirely, never reaches `_unhandled_input`. Events flush next frame, not synchronously — tests must `await get_tree().process_frame` before asserting. No GUT addon present in repo; recommend a plain sentinel test scene, run via `godot --headless --path . res://tests/x.tscn` honoring `get_tree().quit(0)`.

5. **Flagged issues for Director ruling:**
   - **Real charter conflict:** CLAUDE.md 3.1 binds keyboard `E` to both `stance_prev` and `interact` — both would fire on one keypress. Gamepad has no such conflict (LT vs. D-Pad Up).
   - `stance_prev`'s joypad axis (LT) repeat-triggers `event.is_action_pressed()` on every `InputEventJoypadMotion` above deadzone (confirmed at 0.8 and 0.9) — needs a latch. This is a Step 5 (stance engine) concern, not a Step 3 blocker, since Step 5 owns consuming this buffered action.
   - Re-confirmed Step 2's finding: a parse error in an autoload silently nulls it and the run hangs indefinitely rather than erroring — QA must keep grepping `ERROR:`/`SCRIPT ERROR:` and wrap headless runs in a timeout.

## Approach & Tradeoffs (Director sign-off)
- **InputMap:** hand-authored `[input]` section in `project.godot`, per Research's verified format. Keyboard uses `physical_keycode`. Rejected runtime `InputMap.add_action()` — not version-controlled, not diffable, re-added every boot.
- **E-key conflict ruling (charter says both `stance_prev` and `interact` bind to keyboard `E`):** remapping `stance_prev`'s **keyboard** binding to `Tab` (gamepad LT is untouched — no conflict there). Rationale: `interact` (E) is the far higher-frequency, more universally-expected binding (shrines/chests/NPCs/doors, used in nearly every step from 10 onward); `stance_next`/`stance_prev` already reads naturally as a Q/Tab pair, and Tab is unused elsewhere in the action list. This is a deliberate deviation from the charter's literal text, logged here rather than silently implemented — flag for the user/future Director review, not hidden in a diff.
- **Buffer location:** new `autoload/input_buffer.gd`, singleton name `InputBuffer`, registered in `project.godot`'s existing `[autoload]` section alongside `EventBus`/`GameState`. Matches Step 2 conventions: `extends Node`, no `class_name` (Step 2 set precedent of none), `process_mode = PROCESS_MODE_ALWAYS`, `print("INPUTBUFFER_OK")` sentinel in `_ready()`.
- **API surface (Step 3 delivers the mechanism only — Steps 4/5 are the consumers and don't exist yet):**
  - `buffer_action(action: StringName) -> void` — appends `{"action": action, "timestamp": Time.get_ticks_msec() / 1000.0}`, but only if `not GameState.is_player_input_locked()` (drop silently when locked, per Step 2's existing design intent — do not queue-and-replay-later).
  - `consume_action(action: StringName) -> bool` — scans the buffer for the newest matching entry, returns true and removes it if `Time.get_ticks_msec()/1000.0 - entry.timestamp <= 0.15`, discarding (not just skipping) any expired entries encountered along the way. Expiry computed at consume time, not via a `_process` sweep (Research's recommendation — avoids a redundant per-frame timer).
  - `clear() -> void` — empties the buffer (for state transitions, e.g. entering DIALOGUE/CUTSCENE).
- **Ingestion:** `_unhandled_input(event)` on `InputBuffer` itself, using `event.is_action_pressed("light_attack")` (etc.) for each of the 4 buffered actions — never `Input.is_action_just_pressed()` polling (verified to triple-fire).
- **Rejected:** adding the GUT testing addon for this one step — no GUT present in repo yet, and Research's plain sentinel-scene approach (headless, `Input.parse_input_event()`, `await get_tree().process_frame`, `get_tree().quit(0)`) is sufficient and consistent with Steps 1/2's existing headless-sentinel QA convention. Revisit GUT adoption if a later step (e.g. Step 14 QA) needs real assertion/reporting infrastructure.
- **Explicitly out of scope for Step 3** (deferred, not silently dropped): the `stance_prev` joypad-axis repeat-trigger/latch issue belongs to Step 5 (stance engine), which owns actually consuming `stance_prev`. `move_forward/back/left/right` are pure `InputMap` bindings with no buffering — buffering only applies to the 4 actions CLAUDE.md 3.2 names.

## Implementation Summary (Implementation Agent)
- `project.godot`: added a hand-authored `[input]` section with all 10 actions, each carrying a keyboard/mouse `InputEventKey`/`InputEventMouseButton` binding plus a gamepad `InputEventJoypadButton`/`InputEventJoypadMotion` binding, per the Research Agent's verified constants. `stance_prev`'s keyboard binding is `physical_keycode=4194306` (`KEY_TAB`, empirically confirmed against the real Godot 4.7.1 executable via a throwaway `--script` probe printing `KEY_TAB`) per the Director's E-key conflict ruling — `interact` keeps keyboard `E` (`physical_keycode=69`), gamepad D-Pad Up (`JOY_BUTTON` index 11). `stance_prev`'s gamepad LT is unaffected: `InputEventJoypadMotion` axis 4 (`TRIGGER_LEFT`), `axis_value=1.0`. The 4 move actions use `InputEventJoypadMotion` (axis 0/1, ±1.0) for the left stick, not buttons, per the sign-off.
- `project.godot`: registered `InputBuffer="*res://autoload/input_buffer.gd"` in the existing `[autoload]` section, alongside `EventBus`/`GameState`.
- `autoload/input_buffer.gd` (new): `InputBuffer` singleton, `extends Node`, no `class_name`, matching Step 2 conventions exactly. `process_mode = Node.PROCESS_MODE_ALWAYS` and `print("INPUTBUFFER_OK")` sentinel in `_ready()`. Implements:
  - `buffer_action(action: StringName) -> void` — appends `{"action": action, "timestamp": Time.get_ticks_msec() / 1000.0}` to an internal `Array[Dictionary]`, gated on `not GameState.is_player_input_locked()` (silently drops when locked, per Step 2's design intent).
  - `consume_action(action: StringName) -> bool` — scans the buffer newest-to-oldest; on the first entry matching `action`, removes it and returns whether it was within the 0.15s window; along the way, discards (removes) any non-matching entries it encounters that have already expired, per the sign-off's "discards any expired entries encountered" wording.
  - `clear() -> void` — empties the buffer.
  - `_unhandled_input(event)` — for each of `light_attack`/`heavy_attack`/`parry`/`dodge`, calls `event.is_action_pressed(action_name)` (never `Input.is_action_just_pressed()`, per Research's verified triple-fire finding) and buffers on a match.
- `scripts/tests/input_buffer_test.gd` + `scripts/tests/input_buffer_test.tscn` (new, new `scripts/tests/` convention — none existed yet): headless sentinel test. Forces `GameState.set_state(GameState.State.PLAYING)` first (default `INITIALIZING` state otherwise makes `is_player_input_locked()` true and silently drops every buffered input — caught this via an initial failing run, see below), simulates a `dodge` joypad-button press via `Input.parse_input_event()`, awaits two `process_frame`s, then asserts: (1) `consume_action("dodge")` returns `true` immediately, (2) consuming again returns `false` (already removed), (3) a second buffered press followed by a 0.2s wait (past the 0.15s expiry) makes `consume_action("dodge")` return `false`. Prints `INPUT_BUFFER_TEST_PASS`/`INPUT_BUFFER_TEST_FAIL: <reason>` and calls `get_tree().quit(0/1)`.
- `docs/Worklog.md`: Step 3 row added.

**Self-verification performed** (found `Godot_v4.7.1-stable_win64_console.exe` under `C:\Users\El Samaka\Downloads\Godot_v4.7.1-stable_win64.exe\`):
1. `--headless --path . --import` — exit 0, log shows `StanceData` registered as a global class (cache built), no errors.
2. `--headless --path . --quit` — exit 0, no `ERROR:`/`SCRIPT ERROR:` lines, output contains both `GAMESTATE_OK INITIALIZING` and `INPUTBUFFER_OK`.
3. `--headless --path . res://scripts/tests/input_buffer_test.tscn` — first run failed with `INPUT_BUFFER_TEST_FAIL: expected consume_action('dodge') to return true immediately after buffering` (root cause: test didn't set `GameState` to `PLAYING`, so the default `INITIALIZING` state made `is_player_input_locked()` true and every buffered input was silently dropped — this is correct `InputBuffer` behavior, not a bug in it; fixed by adding `GameState.set_state(GameState.State.PLAYING)` at the top of the test). Re-run: exit 0, prints `INPUT_BUFFER_TEST_PASS`, no error lines.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** Independent verification from scratch (did not trust the Implementation Agent's self-report):
  1. Read `project.godot`'s `[input]` section directly. Confirmed all 10 actions present (`move_forward/back/left/right`, `light_attack`, `heavy_attack`, `parry`, `dodge`, `stance_next`, `stance_prev`, `interact`), each with both a keyboard/mouse `InputEventKey`/`InputEventMouseButton` and a gamepad event. Confirmed `stance_prev` keyboard = `physical_keycode=4194306` (= `KEY_TAB`, verified: Godot 4's special-key range starts at `0x400000`=4194304, and `KEY_TAB` is the 3rd entry after `KEY_ESCAPE`(4194305), giving 4194306 — matches, NOT `E`/69). Confirmed `interact` keyboard = `physical_keycode=69` (`E`) + gamepad `InputEventJoypadButton button_index=11` (D-Pad Up). Confirmed the 4 move actions and `stance_prev`'s gamepad binding use `InputEventJoypadMotion` (axis 0/1 for movement, axis 4 = `TRIGGER_LEFT` for `stance_prev`), not `InputEventJoypadButton`.
  2. Read `autoload/input_buffer.gd` in full. Confirmed `process_mode = Node.PROCESS_MODE_ALWAYS` set in `_ready()`, `print("INPUTBUFFER_OK")` sentinel present, `buffer_action`/`consume_action`/`clear` all present with the specified semantics: `buffer_action` gates on `GameState.is_player_input_locked()` before appending; `consume_action` scans newest-to-oldest, checks the 0.15s expiry window, discards (removes) expired non-matching entries encountered along the way, and removes the matched entry regardless of outcome (so a second call can't double-fire); `_unhandled_input` uses `event.is_action_pressed(action_name)` per the 4 buffered actions (light_attack/heavy_attack/parry/dodge), not `Input.is_action_just_pressed()` polling.
  3. Confirmed `InputBuffer="*res://autoload/input_buffer.gd"` registered in `project.godot`'s `[autoload]` section alongside `EventBus`/`GameState`.
  4. Ran headless tests independently using `Godot_v4.7.1-stable_win64_console.exe` (found at `C:\Users\El Samaka\Downloads\Godot_v4.7.1-stable_win64.exe\`), each wrapped in a 60-90s `timeout`:
     - `--headless --path . --import` → exit 0, no errors.
     - `--headless --path . --quit` → exit 0, log contains `GAMESTATE_OK INITIALIZING` and `INPUTBUFFER_OK`, no `ERROR:`/`SCRIPT ERROR:` lines.
     - `--headless --path . res://scripts/tests/input_buffer_test.tscn` → exit 0, log contains `INPUT_BUFFER_TEST_PASS`, no error lines.
  5. Went further than the implementer: wrote a temporary standalone probe script (`scripts/tests/_qa_probe_lock.gd`, extending `SceneTree`, run via `--script`) to test scenarios the existing test scene didn't cover:
     - `buffer_action()` called while `GameState.current_state` is `INITIALIZING` (the real default), `PAUSED`, and `DIALOGUE` — asserted the buffered action is NOT retrievable via `consume_action` afterward (i.e. silently dropped, per spec).
     - `buffer_action()` called while `PLAYING` — asserted it IS retrievable, then asserted a second immediate `consume_action` call for the same action returns `false` (proving the entry was actually removed on first consume, not just read).
     - Ran via `--headless --path . --script res://scripts/tests/_qa_probe_lock.gd` → exit 0, printed `QA_PROBE_PASS`, no error lines. Deleted the probe file (`_qa_probe_lock.gd` and its `.gd.uid`) after running — confirmed via `ls scripts/tests/` that only the implementer's original 3 files (`input_buffer_test.gd`, `.gd.uid`, `.tscn`) remain, no test pollution left behind.
  6. Read the actual diff (`git diff project.godot`, full read of `autoload/input_buffer.gd` and `scripts/tests/input_buffer_test.gd`) rather than trusting the implementer's file list — no dead code, no obviously wrong constants, all content consistent with the Director's sign-off wording.
- **Result:** **PASS.** All Definition of Done items verified independently:
  - All 10 InputMap actions present with correct keyboard+gamepad bindings; `stance_prev` keyboard is confirmed `Tab` (not `E`) per the Director's ruling; `interact` keeps `E`+D-Pad Up; movement axes and `stance_prev` gamepad use `InputEventJoypadMotion` as required.
  - `InputBuffer` autoload registered correctly; buffer semantics (ingestion via `is_action_pressed` on the event, gated ingestion via `is_player_input_locked()`, expiry-aware consumption that actually removes entries) all verified both by reading the code and by independent headless probes, including the two "break it" cases explicitly called for (double-consume returns false; buffering while not `PLAYING` — `INITIALIZING`/`PAUSED`/`DIALOGUE` — is a no-op).
  - Headless `--import`/`--quit` smoke test and the dedicated `input_buffer_test.tscn` both pass cleanly (exit 0, both sentinels present, no ERROR/SCRIPT ERROR lines).
  - One non-blocking documentation note for the Director: `docs/Worklog.md` has a table row for Step 3 but (unlike Steps 1 and 2) no detailed `## Step 3: ...` section with Tests/Changes/Bugs/Fix/Game Feel Wins headers per CLAUDE.md's Worklog Logging Template — worth requiring before final sign-off closes the task, but does not affect the functional correctness of the InputMap or InputBuffer implementation.

## Director Final Review
- **Findings:** Reviewed `project.godot`'s `[input]` diff and `autoload/input_buffer.gd` directly (not just the implementer's/QA's summaries). Confirmed: all 10 actions present with correct keyboard/mouse + gamepad encodings; `stance_prev` keyboard is genuinely `Tab` (physical_keycode 4194306) with gamepad LT untouched, matching the sign-off's ruling exactly; movement + `stance_prev`'s gamepad binding correctly use `InputEventJoypadMotion` (axis), not buttons; `consume_action`'s newest-to-oldest scan correctly removes the matched entry (preventing double-fire) and discards expired entries it passes over first. No dead code, no naming inconsistencies, no unaddressed edge cases beyond what's already explicitly deferred to Step 5 (the joypad-axis latch for `stance_prev`, appropriately out of scope here since Step 3 has no consumer yet). One gap found: `docs/Worklog.md` had a table row but no detailed per-step section (unlike Steps 1/2) — closed this myself by adding the Step 3 Worklog section rather than sending back for a fix-loop cycle, since it's pure documentation with no code/behavior implications.
- **Sign-off:** **Step 3 — Abstracted Input System & Rolling Action Buffer — COMPLETE.** Clean QA pass (including QA's own independent double-consume and input-lock probes beyond what the implementer checked), Research's approach faithfully implemented including the deliberate E/Tab conflict resolution, and Worklog now fully consistent with Steps 1/2's documentation depth. No fix loop was required. Ready to proceed to **Step 4: 3D Kinematics, Movement Physics & Dodge Roll**.
