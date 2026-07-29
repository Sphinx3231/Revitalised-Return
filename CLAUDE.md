# PROJECT RETURN — SYSTEM CHARTER (GODOT 4)

This project is independent — it has no relation to any other project in sibling directories (e.g. NinjaGame). It lives in its own repo at `https://github.com/Sphinx3231/Revitalised-Return.git`, built in **Godot 4 (GDScript), in 3D** (isometric/third-person camera over a 3D world — not a 2D top-down game). All node references throughout this charter are 3D nodes (`CharacterBody3D`, `Area3D`, `Camera3D`, `RayCast3D`, `CSGBox3D`, `MultiMeshInstance3D`) unless explicitly noted otherwise.

## 🗺️ 1. WORLD & ACTS
*   **Hero:** Jin Takakura (Samurai returning to Kaze-no-Tani to bury his lord's medallion).
*   **Prologue:** Ashes of Sekigahara | Boss: Captain Renzo.
*   **Act I:** Ashlands | Boss: Kuroda | Reward: Stone Stance (Heavy break).
*   **Act II:** Sunken Pines | Boss: Soren | Reward: Water Stance (Rapid strikes).
*   **Act III:** Mount Shindai | Boss: General Masato | Reward: Flame Stance (Crowd cleave).
*   **Act IV:** Outskirts | Boss: Madame Mei | Reward: Wind Stance (Deflects).
*   **Act V:** Ancestral Estate | Final Boss: Lord Osamu (Dynamic Stance Mirror).

## 💻 2. ARCHITECTURE & FORMULAS
*   **Autoloads:** `GameState.gd` (FSM), `EventBus.gd` (Signals), `SoundManager.gd`.
*   **Node Communication:** Call Down (direct calls), Signal Up (`EventBus`).
*   **Persistence:** Custom `Resource` scripts (`PlayerData.tres`) via `ResourceSaver`/`ResourceLoader` in `user://saves/`.
*   **Formulas:**
    *   EXP = 100 × (Level)^1.5
    *   Damage = (Base + Weapon) × (1 - Armor / (Armor + 100))
*   **Input Queue:** Rolling 0.15s GDScript `Array` buffer for combo buffering.

## 🛠️ 3. GAME FEEL & POLISH RULES
*   **Silhouettes:** Top-knot, haori cloth physics, waist scabbard facing direction.
*   **Kinematics:** Velocity `lerp()` dampening, sharp turn lean angle, drop shadow sprite.
*   **Juice Engine:**
    *   **Hit-Stop:** 0.03s–0.06s freeze frame via `SceneTreeTimer`.
    *   **Camera:** Trauma-decay shake using `FastNoiseLite`.
    *   **VFX:** White flash shader (`flash_strength`), `GPUParticles3D` sparks, mesh-trail slash arcs (e.g. `Trail3D`/ribbon mesh, not `Line2D`).

## 🚦 4. STRICT 14-STEP PIPELINE
1.  **Project Init:** Directory architecture (`res://autoload`, `res://scenes`, `res://resources`, `res://docs`).
2.  **Core FSM & EventBus:** State machine + dynamic global signals.
3.  **Input System:** `InputMap` setup + 0.15s buffer engine.
4.  **Kinematics & Dodge:** `CharacterBody3D` physics, `lerp()` velocity, 0.2s–0.35s dodge i-frames.
5.  **Stances & Hitboxes:** Custom `Resource` stances + `Area3D` hit/hurtbox sweeps & parry timing.
6.  **Juice Engine:** Hit-stop, camera trauma, hit flashes, particle sparks, slash arcs.
7.  **AI Architecture:** 90° `RayCast3D` FOV, sound radiuses, FSM behavior nodes.
8.  **Boss Mechanics:** Multi-phase HP/posture thresholds, arena locks, `Camera3D` tracking. See 8.1–8.2 below for detail.
9.  **World Greybox:** Block out 5 regions with `CSG`/mesh primitives & pacing checks; see Stage D below for dressing/culling passes.
10. **Interactions & Inventory:** Shrine/chest `Area` triggers, inventory `Resource` stack structures.
11. **HUD & Save/Load:** Reactive `Control` UI via `EventBus` + `ResourceSaver` serialization.
12. **Narrative & Quests:** Quest FSM + branching `Control` dialogue tree nodes.
13. **Art, Blend Trees & Audio:** Final models, `AnimationTree` masks, shaders, material footstep SFX.
14. **Balancing & Optimization:** Economic curves, object pooling, locked 60 FPS (<16.6ms).

Each of these 14 steps is executed through the **Director/subagent workflow** in Section 6, not implemented directly.

**Steps 9–14 design pass:** unlike Steps 1–8 above, Steps 9–14 are not yet fully specced here. See `docs/DesignDoc.md` for a Genshin Impact / Elden Ring-referenced research pass covering region topology, itemization, HUD/save design, quest/dialogue structure, animation/audio, and economy balancing for those steps. It is **staged, not locked** — pending Director sign-off on its "Open Questions" section — and gets folded into this charter as a spec addendum once approved.

## 🔧 STEP DETAIL SPECIFICATIONS (Steps 2–7)

### STEP 2: Core Game State Machine & Global Event Bus

**2.1 Global Signal Dispatcher (`res://autoload/EventBus.gd`)**
Pub/sub messenger that decouples systems (e.g. UI updates on damage without referencing the Player or Enemy nodes directly).

*Player & Vital Signals:*
```gdscript
signal player_health_changed(current: float, max_health: float)
signal player_stamina_changed(current: float, max_stamina: float)
signal player_posture_changed(current: float, max_posture: float)
signal stance_swapped(new_stance_resource: StanceData)
signal player_died()
```
*Combat & Damage Signals:*
```gdscript
signal entity_damaged(target_node: Node3D, amount: float, is_critical: bool)
signal posture_broken(target_node: Node3D)
signal parry_executed(attacker: Node3D, defender: Node3D)
signal enemy_killed(enemy_node: Node3D, exp_reward: int)
```
*World & UI Signals:*
```gdscript
signal quest_state_updated(quest_id: String, state: int)
signal interaction_triggered(interactable_node: Node3D)
signal show_notice(text: String, duration: float)
```

**2.2 Game State Manager Singleton (`res://autoload/GameState.gd`)**
Top-level control over global game loops, menu states, tree pausing, and cursor trapping.

FSM state enum:
```gdscript
enum State { INITIALIZING, MAIN_MENU, PLAYING, PAUSED, DIALOGUE, CUTSCENE, GAME_OVER }
var current_state: State = State.INITIALIZING
```
State transition logic (`set_state(new_state)`):
*   Updates `current_state`.
*   `PLAYING`: `Input.mouse_mode = Input.MOUSE_MODE_CAPTURED`, `get_tree().paused = false`.
*   `PAUSED` / `MAIN_MENU`: `Input.mouse_mode = Input.MOUSE_MODE_VISIBLE`, `get_tree().paused = true` (UI nodes must have `process_mode = PROCESS_MODE_ALWAYS`).
*   `DIALOGUE` / `CUTSCENE`: `Input.mouse_mode = Input.MOUSE_MODE_CAPTURED`, freezes player inputs while keeping environment/animations ticking (`get_tree().paused = false`).

### STEP 3: Abstracted Input System & Rolling Action Buffer

**3.1 Hardware Abstraction Layer (Input Map Setup)**
Configure actions in `Project Settings -> Input Map` with both keyboard/mouse and gamepad bindings:
*   `move_forward` (W / Left Stick Up), `move_back` (S / Left Stick Down)
*   `move_left` (A / Left Stick Left), `move_right` (D / Left Stick Right)
*   `light_attack` (LMB / Controller X)
*   `heavy_attack` (RMB / Controller Y)
*   `parry` (F / Controller LB)
*   `dodge` (Space / Controller A)
*   `stance_next` (Q / Controller RB), `stance_prev` (E / Controller LT)
*   `interact` (E / Controller D-Pad Up)

**3.2 Rolling Action Queue Buffer**
Inputs pressed during an animation's wind-up or recovery window are queued and executed as soon as the active state permits.

*   **Buffer data structure:** `Array[Dictionary]` of active input entries: `{ "action": StringName, "timestamp": float }`.
*   **Buffer ingestion:** in `_unhandled_input(event)`, whenever `light_attack`, `heavy_attack`, `parry`, or `dodge` is pressed, append `{ "action": action_name, "timestamp": Time.get_ticks_msec() / 1000.0 }`.
*   **Buffer clean-up & consumption:** expiration window `T_expire = 0.15s`. At the start of an animation recovery frame, check if `t_current - t_action <= 0.15`. If valid, trigger the buffered action and clear the buffer array.

### STEP 4: 3D Kinematics, Movement Physics & Dodge Roll

**4.1 Directional Movement & Inertia Interpolation**
Calculates character movement relative to the active `Camera3D` transform using lerped acceleration/deceleration vectors.

Camera-relative vector derivation:
```
D_input = (Transform_cam.basis * V_input).normalized()
```
Kinematic velocity equations:
```
V_target = D_input * S_speed
V_horizontal = lerp(V_horizontal, V_target, alpha * delta_t)
```
Where `alpha_accel = 15.0` when `D_input != 0`, and `alpha_frict = 20.0` when `D_input == 0`.

Gravity & air resistance:
```
V_y = V_y - g * delta_t   (g = 24.5 m/s^2)
```

**4.2 Dynamic Character Mesh Lean**
Applies subtle mesh tilting during fast direction changes to convey weight:
```
Lean Angle (Z-axis) = -clamp(omega_yaw * 0.1, -5.0deg, 5.0deg)
```

**4.3 Dodge Roll Mechanics & Invincibility Window (i-Frames)**
*   **Execution:** locks movement direction to `D_input` (or mesh facing direction if idle).
*   **Speed profile:** initial burst at `1.8x S_speed`, tapering linearly to `1.0x S_speed` over `0.5s`.
*   **i-Frame timing:** total duration `0.5s`; i-frame window `0.15s <= t <= 0.35s`. During the active window, disable `PlayerHurtbox.collision_layer` or set `is_invulnerable = true`.
*   **Resource cost:** consumes `20.0` Stamina. Triggers a `1.2s` pause on passive stamina regeneration.

### STEP 5: Stance Engine, Hitbox Registration & Parry Logic

**5.1 Custom Stance Resource Architecture (`StanceData.gd`)**
Stances are defined via custom `Resource` scripts exported into `.tres` files.

Resource properties:
*   `stance_name: String`
*   `base_damage_multiplier: float` (e.g. `1.2x`)
*   `posture_damage_multiplier: float` (e.g. `1.8x`)
*   `attack_speed_scalar: float` (e.g. `0.85x`)
*   `parry_window_duration: float` (e.g. `0.12s`)
*   `icon: Texture2D`

The 4 stances:
*   **Stone:** Heavy posture damage, slow recovery, high poise.
*   **Water:** Rapid fluid thrusts, low stamina cost per strike.
*   **Flame:** Wide horizontal arc cleaves for crowd control.
*   **Wind:** High deflection efficiency and specialized anti-spear counters.

**5.2 Frame-Accurate Area3D Sweep Validation & Parry Logic**
*   **Hitbox/hurtbox setup:** weapons carry an `Area3D` (hitbox) attached to the hand bone; entities carry an `Area3D` (hurtbox) encompassing the body mesh.
*   **Active window monitoring:** hitbox monitoring is enabled strictly during key animation frame tracks using `AnimationPlayer` method-call tracks (`enable_hitbox()` / `disable_hitbox()`).
*   **Collision resolution** (`area_entered` callback):
    *   **Parry check:** if target is in `PARRY` state AND `t_parry <= parry_window_duration`: attacker gets interrupted (plays stagger animation, posture depleted by `40%`), defender triggers parry counter animation, emit `EventBus.parry_executed(attacker, defender)`.
    *   **Block check:** if target is blocking, reduce incoming damage by block mitigation factor (`80%`), apply full posture damage to target's posture bar.
    *   **Hit check:** deduct `Health = BaseDamage * StanceMultiplier * (1 - ArmorMitigation)`; deduct posture; emit `EventBus.entity_damaged`.

### STEP 6: 3D "Juice" Engine & Impact Feedback

**6.1 Freeze-Frame Hit-Stop Engine**
On successful heavy hit or parry, momentarily freeze the game tree to give attacks weight:
*   Execute `Engine.time_scale = 0.0` for `0.03s - 0.06s` (2-4 frames at 60 FPS).
*   Resume `Engine.time_scale = 1.0` via a non-paused scene tree timer (`get_tree().create_timer(duration, true, false, true)`).

**6.2 Camera3D Trauma Shake System**
Non-repetitive shake based on 2D Simplex/FastNoise.

Trauma decay:
```
Trauma(t) = clamp(Trauma(t-1) - Decay * delta_t, 0.0, 1.0)   (Decay = 1.5)
```
Rotational offset calculation:
```
Pitch = MaxPitch * Trauma^2 * Noise.get_noise_2d(seed1, t)
Yaw   = MaxYaw   * Trauma^2 * Noise.get_noise_2d(seed2, t)
Roll  = MaxRoll  * Trauma^2 * Noise.get_noise_2d(seed3, t)
```

**6.3 Visual Impact VFX**
*   **Hit-flash material shader:** spatial shader with uniform `flash_intensity: float` (`0.0` to `1.0`). On damage hit, tween `flash_intensity` to `1.0` (pure white albedo) and decay to `0.0` over `0.08s`.
*   **Directional spark particles:** instantiate `GPUParticles3D` at the contact point, oriented along the contact surface normal vector.
*   **Weapon arc trails:** dynamic `MeshInstance3D` ribbon trail rendered along the sword tip and hilt points during active attack frames.

### STEP 7: 3D AI Architecture, Perception & Behavior Trees

**7.1 Multi-Sensory Perception Engine**
Enemies evaluate player detection every `0.1s` via physics sweeps.

Vision cone verification:
*   Vector to target: `D_target = (P_player - P_enemy).normalized()`.
*   Dot product with facing vector: `cos(theta) = F_enemy . D_target`.
*   Angle check: if `theta <= 45deg` (90 degree total cone) and distance `d <= 18.0m`: cast `RayCast3D` from enemy eye position to player center. If `RayCast3D.get_collider() == Player`, increment detection meter by `30.0 / d * delta_t`.

Acoustic detection sphere: if the player enters an enemy's sound `Area3D` (`d <= 8.0m`) while sprinting, rolling, or swinging a weapon, instantly force enemy state to `INVESTIGATE`.

**7.2 Enemy Finite State Machine (FSM) Execution**
Each enemy controller runs an isolated state machine:
```
[IDLE] ----(Sight/Sound)----> [INVESTIGATE] ----(In Range)----> [TELEGRAPH]
   ^                                                                 |
   |                                                                 v
[RECOVERY] <------------------(Animation End)------------------- [ATTACK]
```
State behaviors:
*   **IDLE / PATROL:** follows path nodes (`Path3D`) at `50%` movement speed while running visual sweeps.
*   **INVESTIGATE:** moves toward target noise/last-seen position at full speed, looking around for `3.0s`.
*   **TELEGRAPH:** locks movement, turns smoothly toward player (`lerp_angle`), plays wind-up animation, and activates eye-glint particle indicator (`0.3s - 0.6s`).
*   **ATTACK:** enables weapon hitbox `Area3D` collisions during active swing frames.
*   **RECOVERY:** pauses attack logic for a configurable cooldown window (`0.8s - 1.5s`), circling or backstepping relative to the player before re-engaging.

### 8.1 Boss Phase Logic (Step 8 detail)
*   **Phase 1 (100%–50% HP):** On crossing the 50% HP threshold, trigger invincibility (`invulnerable = true`), execute an area-of-effect knockback attack, activate arena wall hazards, and switch the active behavior tree.
*   **Phase 2 (50%–0% HP):** Enraged attack speeds, flaming blade particle trails, expanded multi-hit combo strings, and stance-swapping logic (e.g., Lord Osamu mirroring Jin's active stance).

### 8.2 Camera3D Arena Bounds Lock (Step 8 detail)
*   Seal arena entrances using dynamic `CSGBox3D` barrier meshes upon crossing the boss trigger `Area3D`.
*   Adjust `Camera3D` target position to track the midpoint between player and boss:
    `cam_target = (player_position + boss_position) / 2 + isometric_offset`

## 🌐 STAGE D: WORLD BUILD (Step 9 detail)
*   Render foliage, cherry blossom petals, and terrain debris via `MultiMeshInstance3D` to consolidate thousands of meshes into single draw calls.
*   **Occlusion Culling:** Bake static occlusion culling meshes for indoor and mountain terrain assets.
*   **Target Performance Metric:** Maintain a locked **60 FPS** with frame execution times strictly below **16.6ms**.

## 📁 FOLDER STRUCTURE
```
res://
├── autoload/          (EventBus.gd, GameState.gd, SoundManager.gd)
├── scenes/            (UI, World, Player, Enemies, Shrines)
├── scripts/           (FSMs, AI, Combat logic)
├── resources/         (StanceData.gd, ItemData.gd, SavedState.gd)
├── assets/            (Meshes, Materials, Textures, Audio, Shaders)
└── docs/              (Worklog.md, Tasks/)
```

## 👥 6. STUDIO HIERARCHY — DIRECTOR/SUBAGENT WORKFLOW

Work runs as a small studio with distinct roles, implemented as separate subagents (via the Agent tool) coordinated by the **Director** — the top-level agent in the conversation. The Director never does research, implementation, or testing itself; it assigns, routes, reviews, and is the only one allowed to mark a step done.

Every step in Section 4 goes through this pipeline:

1.  **Director — intake**
    *   Turns the roadmap step into a task brief: goal, affected systems (autoload/scenes/scripts/resources), constraints, definition of done.
    *   Opens `docs/Tasks/<date>-<slug>.md` from the template (see below) before assigning anything.

2.  **Research Agent** (`Explore` for quick lookups, `general-purpose` with WebSearch/WebFetch for anything needing Godot docs or external references)
    *   Investigates existing systems it touches, relevant Godot 4 APIs/docs (`CharacterBody3D`, `Area3D`, `RayCast3D`, `CSGBox3D`, `MultiMeshInstance3D`, `AnimationTree`, `ResourceSaver`, etc.), how similar mechanics are usually implemented in Godot 3D, and constraints (physics tick rate, signal ordering, node lifecycle, occlusion culling bake times).
    *   Never guesses at Godot API behavior — verifies against docs or existing project code.
    *   Reports findings back to the Director; does not write implementation code.
    *   Findings get logged in the task file before implementation starts.

3.  **Director — approach sign-off**
    *   For anything nontrivial, states the chosen approach and tradeoffs (informed by Research) before implementation starts — no silently picking one.

4.  **Implementation Agent** (`general-purpose`)
    *   Given the approved approach and research findings, writes the GDScript/scene changes. Does not re-do research or decide the approach.
    *   Reports back a summary of what changed and why.

5.  **QA/Test Agent** (`general-purpose`)
    *   Runs the project (Godot headless or editor Play mode) and exercises the feature — not just "it parses." Where unit tests make sense (damage formulas, EXP scaling, save/load round-trips), it runs or writes GUT (Godot Unit Test) or equivalent tests.
    *   Reports pass/fail with specifics: repro steps, exact error, affected file/line.
    *   Never fixes issues itself — only reports them back to the Director.

6.  **Fix loop**
    *   If QA reports a failure, the Director routes the QA report back to the Implementation Agent (with the research doc still available). Re-run Implementation → QA until QA reports a clean pass. Log every iteration (attempt number, what was tried, what QA found) in the task file — do not overwrite prior attempts.
    *   No step is marked done while QA reports unresolved issues.

### Model tiering for subagents
When spawning pipeline subagents via the Agent tool, use:
*   **Research Agent:** `opus` — research/design judgment calls get the strongest model.
*   **Implementation Agent:** `sonnet` (default, no override needed).
*   **QA/Test Agent:** `sonnet` (default, no override needed).
*   **Director:** whatever model fits the session — not spawned via the Agent tool, so no per-call override applies; left to judgment rather than fixed.

7.  **Director — final review**
    *   Re-reads the diff critically as if reviewing a coworker's PR: correctness, obvious bugs, dead code, inconsistent naming, missed edge cases (e.g. enemy losing perception mid-telegraph, save/load state, pause behavior, signal disconnects).
    *   Flags anything questionable explicitly rather than staying silent about it.
    *   Only after this review, and a clean QA pass, does the Director mark the step complete and finalize the task log with a sign-off summary.

Skipping the full pipeline is only acceptable for truly trivial changes (typo fixes, comment edits, renames with no behavior change) — these can be done directly by the Director, still logged as a one-line entry in `docs/Worklog.md`.

## 📝 5. WORKLOG LOGGING TEMPLATE
Log every cycle in `docs/Worklog.md`:

```markdown
## [Step ID / Feature] - [YYYY-MM-DD]

### 🧪 Tests
[Objective, Method, Outcome]

### 🔄 Changes
[Modified scripts, scenes, resources]

### 🔴 Bugs/Cause
[Flaw description & root cause]

### 🛠️ Fix/Prevention
[Technical resolution & regression strategy]

### 💪 Game Feel Wins
[Performance/responsiveness notes]
```

Every step also gets a full log file at `docs/Tasks/<date>-<slug>.md` (created from `docs/Tasks/_template.md`) capturing: task brief, research findings, chosen approach + tradeoffs, implementation summary, every QA iteration (attempt + result, not just the final one), and the Director's final sign-off. Nothing is marked complete without its task log fully filled in and its one-line entry added to `docs/Worklog.md`.

## Current status
Repo freshly scaffolded. No steps from the roadmap have been implemented yet. Next action: Director opens the Step 1 task brief (`docs/Tasks/<date>-step-1-project-init.md`) and routes it through the pipeline.
