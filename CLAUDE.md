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
