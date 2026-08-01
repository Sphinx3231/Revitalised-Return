# PROJECT RETURN — SYSTEM CHARTER (UNITY 6 / C#)

This project is independent — it has no relation to any other project in sibling directories
(e.g. NinjaGame, which is a separate Unity project). It lives in its own repo at
`https://github.com/Sphinx3231/Revitalised-Return.git`, built in **Unity 6000.5.5f1 (C#),
in 3D** (isometric/third-person camera over a 3D world — not a 2D top-down game).

**Engine pivot (2026-07-31):** this project was originally built in Godot 4.7/GDScript
through Step 6 of the pipeline below. It has pivoted to Unity/C#. The Godot implementation
is preserved, not deleted, at `legacy-godot/` for reference (validated game-feel numbers,
formulas, timing windows) but is no longer active. See
`docs/Tasks/2026-07-31-godot-to-unity-pivot.md` for the full decision record. All node/API
references throughout this charter are now Unity types (`CharacterController`, trigger
`Collider`s, `Camera`, `Physics.Raycast`, `ScriptableObject`, `Animator`) unless noted.

## 🗺️ 1. WORLD & ACTS
*   **Hero:** Jin Takakura (Samurai returning to Kaze-no-Tani to bury his lord's medallion).
*   **Prologue:** Ashes of Sekigahara | Boss: Captain Renzo.
*   **Act I:** Ashlands | Boss: Kuroda | Reward: Stone Stance (Heavy break).
*   **Act II:** Sunken Pines | Boss: Soren | Reward: Water Stance (Rapid strikes).
*   **Act III:** Mount Shindai | Boss: General Masato | Reward: Flame Stance (Crowd cleave).
*   **Act IV:** Outskirts | Boss: Madame Mei | Reward: Wind Stance (Deflects).
*   **Act V:** Ancestral Estate | Final Boss: Lord Osamu (Dynamic Stance Mirror).

## 💻 2. ARCHITECTURE & FORMULAS
*   **Persistent singletons:** `EventBus.cs`, `GameState.cs`, `SoundManager.cs` — plain C#
    singletons living on a single `Bootstrap` scene/`DontDestroyOnLoad` `GameObject`, loaded
    first (Unity has no first-class "autoload" concept; this is the closest equivalent —
    see 2.1/2.2).
*   **Node Communication:** Call Down (direct component references / `GetComponent`), Signal
    Up (`EventBus` static C# events).
*   **Persistence:** Plain C# data classes (`PlayerData`) serialized to JSON
    (`JsonUtility`/`System.Text.Json`) in `Application.persistentDataPath/saves/` — **not**
    Unity's `BinaryFormatter` (arbitrary-code-execution risk on a user-editable save file,
    same reasoning as the Godot charter's `ResourceLoader` mitigation below).
*   **Formulas:**
    *   EXP = 100 × (Level)^1.5
    *   Damage = (Base + Weapon) × (1 - Armor / (Armor + 100))
*   **Input Queue:** Rolling 0.15s C# buffer (`List<BufferedInput>` or ring buffer) for combo
    buffering — same design as Godot's Array buffer, just a C# collection.
*   **Coding standard — S.O.L.I.D.:** all C# code (systems, player, AI, combat, UI) must
    adhere to the S.O.L.I.D. principles — Single Responsibility (one class, one reason to
    change — e.g. don't fold input handling into `GameState`), Open/Closed (extend via new
    types/interfaces rather than branching on type checks — e.g. new enemy behaviors as new
    FSM state components, not `if` chains in one god-state), Liskov Substitution (a subclass
    must be usable anywhere its base type is expected — e.g. any `Interactable` subclass must
    honor the base contract), Interface Segregation (small, role-specific interfaces over one
    fat interface — e.g. don't force every `Interactable` to implement HUD-prompt methods it
    doesn't need), and Dependency Inversion (depend on abstractions, not concrete types — e.g.
    `EventBus` static events instead of hard references between systems, per the Signal Up
    pattern above). Implementation Agents apply this while writing; the Director's Section 6
    final review explicitly checks for SOLID violations alongside its other review criteria.

## 🛠️ 3. GAME FEEL & POLISH RULES
*   **Silhouettes:** Top-knot, haori cloth physics (Unity `Cloth` component or bone-spring
    solution — Research to confirm at Step 13), waist scabbard facing direction.
*   **Kinematics:** Velocity `Vector3.Lerp()` dampening, sharp turn lean angle, drop shadow
    (blob shadow projector or a simple decal).
*   **Juice Engine:**
    *   **Hit-Stop:** 0.03s–0.06s freeze frame via `Time.timeScale` + an unscaled-time
        coroutine (`WaitForSecondsRealtime`) to resume — Unity has no `SceneTreeTimer`
        equivalent that auto-ignores `timeScale`, so the resume timer must explicitly use
        unscaled time.
    *   **Camera:** Trauma-decay shake using Unity's `Mathf.PerlinNoise` or
        `Unity.Mathematics.noise.snoise` (package `com.unity.mathematics`) as the
        FastNoiseLite equivalent.
    *   **VFX:** White flash via a URP/Built-in shader with a `_FlashIntensity` property,
        Unity `ParticleSystem` (Shuriken) sparks, `TrailRenderer` for the weapon slash arc
        (Unity's native ribbon-trail component — closer to Godot's `Trail3D` than to
        `Line2D`, which was already explicitly ruled out in the original charter).

## 🚦 4. STRICT 14-STEP PIPELINE
1.  **Project Init:** Unity project + `Assets/` directory architecture (`Scripts/Systems`,
    `Scripts/Player`, `Scripts/AI`, `Scripts/Combat`, `Scenes`, `ScriptableObjects`, `Docs`).
2.  **Core FSM & EventBus:** State machine + `EventBus` static C# events.
3.  **Input System:** Unity Input System package (Input Actions asset) + 0.15s buffer engine.
4.  **Kinematics & Dodge:** `CharacterController` physics, `Vector3.Lerp()` velocity, 0.2s–0.35s
    dodge i-frames.
5.  **Stances & Hitboxes:** `ScriptableObject` stances + trigger-`Collider` hit/hurtbox sweeps
    & parry timing.
6.  **Juice Engine:** Hit-stop, camera trauma, hit flashes, particle sparks, slash arcs.
7.  **AI Architecture:** 90° raycast FOV, sound radiuses, FSM behavior components.
8.  **Boss Mechanics:** Multi-phase HP/posture thresholds, arena locks, `Camera` tracking.
    See 8.1–8.2 below for detail.
9.  **World Greybox:** Block out 5 regions with ProBuilder/primitive meshes & pacing checks;
    see Stage D below for dressing/culling passes.
10. **Interactions & Inventory:** Shrine/chest trigger-`Collider`s, inventory data structures.
11. **HUD & Save/Load:** Reactive UI Toolkit (or `uGUI`) via `EventBus` + JSON serialization.
12. **Narrative & Quests:** Quest FSM + branching dialogue tree data + UI.
13. **Art, Blend Trees & Audio:** Final models, `Animator` layers/masks, shaders, footstep SFX.
14. **Balancing & Optimization:** Economic curves, object pooling, locked 60 FPS (<16.6ms).

Each of these 14 steps is executed through the **Director/subagent workflow** in Section 6,
not implemented directly.

**Steps 9–14 design pass:** still locked (originally 2026-07-29, carried over unchanged by
the engine pivot — this is design content, not engine-specific) — see the "STEP DETAIL
SPECIFICATIONS (Steps 9–14)" section below for the condensed, implementation-facing spec,
and `docs/DesignDoc.md` for the full Genshin Impact / Elden Ring-referenced rationale,
sources, and the Director decisions that resolved every open question. Where that section
still references Godot types, they translate per the mapping table in Section 2/this
section — no design decision in it changes because of the engine pivot.

## 🔧 STEP DETAIL SPECIFICATIONS (Steps 2–7)

### STEP 2: Core Game State Machine & Global Event Bus

**2.1 Global Event Dispatcher (`Assets/Scripts/Systems/EventBus.cs`)**
Pub/sub messenger that decouples systems (e.g. UI updates on damage without referencing the
Player or Enemy `GameObject` directly). Implemented as a static class of C# `event Action<...>`
members (or `UnityEvent<...>` if Inspector-wiring is needed later — default to plain C#
events for the same low-overhead decoupling Godot's signals gave).

*Player & Vital Events:*
```csharp
public static event Action<float, float> PlayerHealthChanged;   // current, max
public static event Action<float, float> PlayerStaminaChanged;  // current, max
public static event Action<float, float> PlayerPostureChanged;  // current, max
public static event Action<StanceData> StanceSwapped;
public static event Action PlayerDied;
```
*Combat & Damage Events:*
```csharp
public static event Action<Transform, float, bool> EntityDamaged;   // target, amount, isCritical
public static event Action<Transform> PostureBroken;                // target
public static event Action<Transform, Transform> ParryExecuted;     // attacker, defender
public static event Action<Transform, int> EnemyKilled;             // enemy, expReward
```
*World & UI Events:*
```csharp
public static event Action<string, int> QuestStateUpdated;   // questId, state
public static event Action<Transform> InteractionTriggered;
public static event Action<string, float> ShowNotice;        // text, duration
```

**2.2 Game State Manager (`Assets/Scripts/Systems/GameState.cs`)**
Top-level control over global game loops, menu states, pausing, and cursor lock.

FSM state enum:
```csharp
public enum State { Initializing, MainMenu, Playing, Paused, Dialogue, Cutscene, GameOver }
public static State CurrentState { get; private set; } = State.Initializing;
```
State transition logic (`SetState(State newState)`):
*   Updates `CurrentState`.
*   `Playing`: `Cursor.lockState = CursorLockMode.Locked`, `Time.timeScale = 1f`.
*   `Paused` / `MainMenu`: `Cursor.lockState = CursorLockMode.None`, `Time.timeScale = 0f`.
*   `Dialogue` / `Cutscene`: `Cursor.lockState = CursorLockMode.Locked`, freezes player input
    while keeping environment/animation ticking (`Time.timeScale` stays `1f`).

**Divergence from the Godot original — no engine-level pause propagation.** Godot's
`get_tree().paused` + per-node `process_mode = PROCESS_MODE_ALWAYS` stops/exempts whole node
subtrees automatically. Unity's `Time.timeScale = 0` does **not** stop `Update()`/`FixedUpdate()`
from being called — it only zeroes `Time.deltaTime`. Anything that must keep animating through
a Unity pause (UI) or must stop reacting to input while paused cannot rely on `timeScale`
alone: gameplay scripts must explicitly check `GameState.IsPlayerInputLocked()` (Unity port of
the same helper) at the top of their input-handling code, same as before, but UI elements that
need to animate under `Paused` must use `Time.unscaledDeltaTime`, not `Time.deltaTime`. Flag
this again at Step 11 (HUD) intake — it is easy to regress.

### STEP 3: Abstracted Input System & Rolling Action Buffer

**3.1 Hardware Abstraction Layer (Unity Input System package, Input Actions asset)**
Configure an Input Actions asset (`Assets/Settings/PlayerControls.inputactions`) with both
keyboard/mouse and gamepad bindings per action:
*   `move_forward` (W / Left Stick Up), `move_back` (S / Left Stick Down)
*   `move_left` (A / Left Stick Left), `move_right` (D / Left Stick Right)
*   `light_attack` (LMB / Controller X)
*   `heavy_attack` (RMB / Controller Y)
*   `parry` (F / Controller LB)
*   `dodge` (Space / Controller A)
*   `stance_next` (Q / Controller RB), `stance_prev` (**Tab**, not E — carrying forward the
    Godot charter's own Step 3 amendment verbatim: keyboard `E` is already bound to
    `interact`, so this project has never actually shipped `E` for `stance_prev` / Controller LT
*   `interact` (E / Controller D-Pad Up)

Requires the `com.unity.inputsystem` package (Research to confirm exact version against
Unity 6000.5.5f1 at Step 3 intake — do not assume compatibility without checking).

**3.2 Rolling Action Queue Buffer**
Inputs pressed during an animation's wind-up or recovery window are queued and executed as
soon as the active state permits. Same design as the Godot version, ported to C#:

*   **Buffer data structure:** a small ring buffer or `List<BufferedInput>` of
    `{ string Action, float Timestamp }` entries.
*   **Buffer ingestion:** on each action's `InputAction.performed` callback for
    `light_attack`, `heavy_attack`, `parry`, or `dodge`, append
    `{ Action = actionName, Timestamp = Time.time }`.
*   **Buffer clean-up & consumption:** expiration window `T_expire = 0.15s`. At the start of
    an animation recovery frame, check if `Time.time - t_action <= 0.15`. If valid, trigger
    the buffered action and remove that entry.

### STEP 4: 3D Kinematics, Movement Physics & Dodge Roll

**4.1 Directional Movement & Inertia Interpolation**
Calculates character movement relative to the active `Camera` transform using lerped
acceleration/deceleration vectors, via `CharacterController.Move()`.

Camera-relative vector derivation — **flattened before normalizing**, carrying forward the
Godot charter's own Step 4 amendment (the literal camera-basis multiply injects a vertical
component and shortens horizontal magnitude under any camera pitch):
```
rawInput = camera.transform.TransformDirection(inputVector);
D_input = new Vector3(rawInput.x, 0, rawInput.z).normalized();
```
Kinematic velocity equations:
```
V_target = D_input * S_speed
V_horizontal = Vector3.Lerp(V_horizontal, V_target, alpha * Time.deltaTime)
```
Where `alpha_accel = 15.0` when `D_input != Vector3.zero`, and `alpha_frict = 20.0` when
`D_input == Vector3.zero`.

Gravity & air resistance:
```
V_y = V_y - g * Time.deltaTime   (g = 24.5 m/s^2)
```

**4.2 Dynamic Character Mesh Lean**
Applies subtle mesh tilting during fast direction changes to convey weight:
```
Lean Angle (Z-axis) = -Mathf.Clamp(omega_yaw * 0.1f, -5.0f, 5.0f)   // degrees
```

**4.3 Dodge Roll Mechanics & Invincibility Window (i-Frames)**
*   **Execution:** locks movement direction to `D_input` (or mesh facing direction if idle).
*   **Speed profile:** initial burst at `1.8x S_speed`, tapering linearly to `1.0x S_speed`
    over `0.5s`.
*   **i-Frame timing:** total duration `0.5s`; i-frame window `0.15s <= t <= 0.35s`. During the
    active window, disable the hurtbox `Collider` (`enabled = false`) or set
    `isInvulnerable = true`.
*   **Resource cost:** consumes `20.0` Stamina. Triggers a `1.2s` pause on passive stamina
    regeneration.
*   Tick-count framing from the Godot version (i-frames at ticks 9/21 of a 60Hz physics step)
    carries over as **time-based** windows here (`0.15s`/`0.35s`) since Unity's `FixedUpdate`
    rate is configurable (default 50Hz, not 60Hz) — Step 4 intake must confirm
    `Time.fixedDeltaTime` is set to `1/60` in Project Settings if exact tick parity with the
    original QA'd timing matters, otherwise use the time-based bounds directly.

### STEP 5: Stance Engine, Hitbox Registration & Parry Logic

**5.1 Custom Stance ScriptableObject Architecture (`StanceData.cs`)**
Stances are defined via `[CreateAssetMenu]` `ScriptableObject`s, saved as `.asset` files —
the direct Unity equivalent of Godot's `Resource`/`.tres` pattern.

Fields:
*   `stanceName: string`
*   `baseDamageMultiplier: float` (e.g. `1.2x`)
*   `postureDamageMultiplier: float` (e.g. `1.8x`)
*   `attackSpeedScalar: float` (e.g. `0.85x`)
*   `parryWindowDuration: float` (e.g. `0.12s`)
*   `icon: Sprite`

The 4 stances (unchanged):
*   **Stone:** Heavy posture damage, slow recovery, high poise.
*   **Water:** Rapid fluid thrusts, low stamina cost per strike.
*   **Flame:** Wide horizontal arc cleaves for crowd control.
*   **Wind:** High deflection efficiency and specialized anti-spear counters.

**5.2 Frame-Accurate Trigger-Collider Sweep Validation & Parry Logic**
*   **Hitbox/hurtbox setup:** weapons carry a trigger `Collider` (hitbox) attached to the
    hand bone; entities carry a trigger `Collider` (hurtbox) encompassing the body mesh, on
    separate Unity **Physics Layers** with the Layer Collision Matrix restricting hitbox
    layers to only their opposing hurtbox layer (structural self-hit prevention — same
    approach Godot's layer/mask system used).
*   **Active window monitoring:** hitbox `Collider.enabled` is toggled strictly during key
    animation frames using Unity **Animation Events** on the attack `AnimationClip`
    (`EnableHitbox()` / `DisableHitbox()`) — the direct equivalent of Godot's
    `AnimationPlayer` method-call tracks.
*   **Collision resolution** (`OnTriggerEnter`):
    *   **Parry check:** if target is in `Parry` state AND `t_parry <= parryWindowDuration`:
        attacker gets interrupted (plays stagger animation, posture depleted by `40%`),
        defender triggers parry counter animation, fire `EventBus.ParryExecuted(attacker, defender)`.
    *   **Block check:** if target is blocking, reduce incoming damage by block mitigation
        factor (`80%`), apply full posture damage to target's posture bar.
    *   **Hit check:** deduct `Health = BaseDamage * StanceMultiplier * (1 - ArmorMitigation)`;
        deduct posture; fire `EventBus.EntityDamaged`.

### STEP 6: 3D "Juice" Engine & Impact Feedback

**6.1 Freeze-Frame Hit-Stop Engine**
On successful heavy hit or parry, momentarily freeze gameplay to give attacks weight:
*   Set `Time.timeScale = 0f` for `0.03s - 0.06s` (2-4 frames at 60 FPS).
*   Resume `Time.timeScale = 1f` via a coroutine using `WaitForSecondsRealtime` (unscaled —
    the direct equivalent of Godot's `ignore_time_scale=true` `SceneTreeTimer` argument;
    a normal `WaitForSeconds` would never fire once `timeScale` hits `0`).

**6.2 Camera Trauma Shake System**
Non-repetitive shake based on Perlin/simplex noise (`Mathf.PerlinNoise` or
`Unity.Mathematics.noise.snoise`).

Trauma decay:
```
Trauma(t) = clamp(Trauma(t-1) - Decay * Time.deltaTime, 0.0, 1.0)   (Decay = 1.5)
```
Rotational offset calculation:
```
Pitch = MaxPitch * Trauma^2 * Noise(seed1, t)
Yaw   = MaxYaw   * Trauma^2 * Noise(seed2, t)
Roll  = MaxRoll  * Trauma^2 * Noise(seed3, t)
```
Applied as a local rotation offset on the camera rig, composed on top of (not replacing) the
Step 8.2 arena-tracking target rotation.

**6.3 Visual Impact VFX**
*   **Hit-flash material:** a URP/Built-in shader with a `_FlashIntensity` property (`0.0` to
    `1.0`). On damage hit, tween `_FlashIntensity` to `1.0` (pure white albedo) and decay to
    `0.0` over `0.08s`.
*   **Directional spark particles:** a pooled Unity `ParticleSystem` (never instantiated
    per-hit — see Step 14's particle budget), triggered at the contact point, oriented along
    the contact surface normal.
*   **Weapon arc trails:** Unity `TrailRenderer` on the sword tip (and, if a two-point ribbon
    is needed for a wider blade silhouette, a second `TrailRenderer` on the hilt point),
    active only during active attack frames — driven by the same Animation Events as 5.2's
    hitbox toggle.

### STEP 7: 3D AI Architecture, Perception & Behavior

**7.1 Multi-Sensory Perception Engine**
Enemies evaluate player detection every `0.1s` via physics sweeps.

Vision cone verification:
*   Vector to target: `D_target = (P_player - P_enemy).normalized()`.
*   Dot product with facing vector: `cos(theta) = F_enemy . D_target`.
*   Angle check: if `theta <= 45deg` (90 degree total cone) and distance `d <= 18.0m`: cast
    `Physics.Raycast` from enemy eye position to player center. If the raycast hits the
    Player collider (and nothing closer on an obstruction layer), increment detection meter
    by `30.0 / d * Time.deltaTime`.

Acoustic detection sphere: if the player enters an enemy's sound trigger `Collider`
(`d <= 8.0m`) while sprinting, rolling, or swinging a weapon, instantly force enemy state to
`Investigate`.

**7.2 Enemy Finite State Machine (FSM) Execution**
Each enemy controller runs an isolated state machine:
```
[Idle] ----(Sight/Sound)----> [Investigate] ----(In Range)----> [Telegraph]
   ^                                                                 |
   |                                                                 v
[Recovery] <------------------(Animation End)------------------- [Attack]
```
State behaviors:
*   **Idle / Patrol:** follows waypoints (a simple `Transform[]` array, or Unity's Splines
    package `com.unity.splines` if curved paths are needed — Research to confirm at Step 7
    intake; Godot's `Path3D` has no single 1:1 Unity equivalent) at `50%` movement speed
    while running visual sweeps.
*   **Investigate:** moves toward target noise/last-seen position at full speed, looking
    around for `3.0s`.
*   **Telegraph:** locks movement, turns smoothly toward player (`Quaternion.Slerp`/
    `Mathf.LerpAngle`), plays wind-up animation, and activates eye-glint particle indicator
    (`0.3s - 0.6s`).
*   **Attack:** enables weapon hitbox `Collider` during active swing frames (same Animation
    Event mechanism as 5.2).
*   **Recovery:** pauses attack logic for a configurable cooldown window (`0.8s - 1.5s`),
    circling or backstepping relative to the player before re-engaging.

### 8.1 Boss Phase Logic (Step 8 detail)
*   **Phase 1 (100%–50% HP):** On crossing the 50% HP threshold, trigger invincibility
    (`invulnerable = true`), execute an area-of-effect knockback attack, activate arena wall
    hazards, and switch the active behavior tree.
*   **Phase 2 (50%–0% HP):** Enraged attack speeds, flaming blade particle trails, expanded
    multi-hit combo strings, and stance-swapping logic (e.g., Lord Osamu mirroring Jin's
    active stance).

### 8.2 Camera Arena Bounds Lock (Step 8 detail)
*   Seal arena entrances using barrier `GameObject`s with colliders (blocked out with
    ProBuilder/primitives at greybox time, matching Step 9) that activate on crossing the
    boss trigger `Collider`.
*   Adjust camera target position to track the midpoint between player and boss:
    `cam_target = (player_position + boss_position) / 2 + isometric_offset`
*   Research to confirm at Step 8 intake whether Cinemachine (`com.unity.cinemachine`) should
    own this framing instead of a hand-rolled camera script — it is the standard Unity
    solution for exactly this kind of dynamic target-following/bounds-locked camera and
    would likely replace a chunk of hand-written math here.

## 🌐 STAGE D: WORLD BUILD (Step 9 detail)
*   Render foliage, cherry blossom petals, and terrain debris via GPU Instancing
    (`Graphics.RenderMeshInstanced`/`DrawMeshInstanced`, or the Visual Effect Graph for
    particle-scale foliage) to consolidate thousands of meshes into few draw calls — the
    Unity equivalent of Godot's `MultiMeshInstance3D` (no single drop-in replacement;
    Research to confirm the best-fit approach at Step 9 intake against the actual asset
    density).
*   **Occlusion Culling:** Unity's built-in Occlusion Culling bake (Window > Rendering >
    Occlusion Culling) for indoor and mountain terrain assets.
*   **Target Performance Metric:** Maintain a locked **60 FPS** with frame execution times
    strictly below **16.6ms**.

## 🔧 STEP DETAIL SPECIFICATIONS (Steps 9–14)

Design content locked 2026-07-29 via a Genshin Impact / Elden Ring design-reference research
pass — full rationale, sources, and the Director decisions resolving every open question live
in `docs/DesignDoc.md`. **None of this changed with the engine pivot** — it is game/content
design, not engine plumbing. Only the data-structure notation below is translated from Godot
`Resource` classes to Unity `ScriptableObject`/plain C# classes; the design decisions
themselves (itemization philosophy, HUD layout, save cadence, quest structure, etc.) are
unchanged from the original.

### STEP 9: World Generation, Terrain Greyboxing & Level Pacing
*   **Topology:** semi-open per-region "bowls" (Elden Ring's Limgrave model) with an authored
    critical-path spine + 2–4 optional lobes, NOT a Genshin-style continuous open continent —
    this is a linear 5-act campaign, not a live-service world. Each region gets one skyline
    anchor landmark for sightline-driven navigation instead of minimap icons. Act V (the
    Estate) is a fully linear legacy-dungeon exception, no open terrain.
*   **Rest shrines:** placed at every region entrance (map-reveal on first rest) and within
    15–25s walk of every boss trigger `Collider` (non-negotiable — avoids bad runbacks into
    Step 8's arena-locked fights). Spacing target: 60–120s travel between consecutive shrines.
    An invisible `GraveMarker` `GameObject` auto-placed at every boss arena entrance (and, per
    the death-penalty ruling below, at every death location) provides an Elden-Ring-style
    respawn-bypass.
*   **Data structure:** `RegionGraph` (plain C# class or `ScriptableObject`: `regionId`,
    `skylineAnchor`, `RegionNode[]`, `RegionEdge[]`, `criticalPath: string[]`) + a waypoint
    spine (Splines package or `Transform[]`) that doubles as the Step 7 AI patrol path source.
    `RegionNode.Kind` enum: `Entrance, Shrine, Encounter, Arena, Vista, Loot, Npc, Gate, Boss,
    SideDomain`.
*   **Stage D caveat:** occlusion baking only considers real meshes — greybox ProBuilder
    geometry must be a `MeshFilter`/`MeshRenderer` (ProBuilder objects are, by default — just
    don't leave them as editor-only helper geometry) or interiors/mountain terrain occlude
    nothing.

### STEP 10: Interactive Objects, Inventory Data & Gathering Economy
*   **Itemization philosophy: ~80% Elden Ring restraint, 20% Genshin.** No RNG-substat gear,
    no resin/energy gating, no real-world respawn timers (nodes respawn on shrine rest only).
    Keep exactly one local specialty material per region (Genshin's regional-material pattern)
    as a soft region-lock flavor. Weapon upgrade is a single linear +0→+10 line costing
    **Tamahagane Ore** + **Mon**; Talisman-equivalent charms (*Omamori*, 3 slots, ~12
    hand-authored, flat deterministic effects, no rolls) replace Genshin-style artifacts
    entirely.
*   **Data structures:** `ItemData` (`ScriptableObject`: itemId, category enum
    `Material/LocalSpecialty/Consumable/Charm/KeyItem/Recipe/UpgradeMat`, maxStack, valueMon,
    regionTag, description as a first-class narrative surface), `ItemStack` (item +
    quantity), `Inventory` (`List<ItemStack>` + a non-serialized runtime index `Dictionary`
    rebuilt on load). **Mon is a scalar int on `PlayerData`, never an `ItemStack`.**
*   **Interaction pattern:** `Interactable` base `MonoBehaviour` (trigger `Collider`,
    layer=Interactable, Physics Layer mask=Player) with `Shrine`/`Chest`/`HarvestNode`/
    `NpcInteractable`/`DoorInteractable` subclasses/components. Player's
    `InteractionResolver` ranks candidates by `0.7·camera-forward-dot + 0.3·proximity`, gates
    on `GameState.IsPlayerInputLocked()`, and consumes the Step 3 input-buffer entry on
    successful interact to prevent double-fire.

### STEP 11: Reactive HUD, UI Systems & Persistence Engine
*   **HUD philosophy:** Elden Ring's minimalist always-on vitals skeleton (no minimap, no
    party switcher — single character) + a top-centre compass strip for sightline
    navigation, plus a Genshin-style full map screen (M key, per-region reveal on first
    shrine rest). **Posture placement follows Sekiro, not either reference game:**
    self-posture directly under player HP, target posture centered under the lock-on
    reticle. Stance shown as a 4-icon diamond, bottom-right. No damage-number spam — it
    works against a posture/parry read. Vitals fade to low alpha after 5s out of combat.
    Build with Unity UI Toolkit (UXML/USS) or `uGUI` — Research to confirm the better fit
    at Step 11 intake against this HUD's reactive/data-binding needs.
*   **Save policy: single live save per playthrough, autosaved at checkpoints** (shrine
    rest, boss defeat, region transition, quest update, key-item pickup, stance unlock,
    quit-to-menu) — Elden Ring's model, not Genshin's account-bound cloud save (no server
    exists). 3 playthrough slots at the main menu.
*   **Save serialization:** `PlayerData` is a plain C# class (not a `ScriptableObject` — those
    are asset-time data, not runtime save state), serialized via `JsonUtility`/
    `System.Text.Json` to a file in `Application.persistentDataPath/saves/`. **Never**
    `BinaryFormatter` — same arbitrary-code-execution concern the Godot charter's
    `ResourceLoader full_objects=false` note was guarding against, and JSON sidesteps it
    entirely by construction. Write to a `.tmp` file and rename over the live save (atomic
    write, avoids mid-crash corruption); keep one rolling `.bak`.
*   **PlayerData fields include:** `saveVersion`, `ngCycle: int = 0` (New Game+, approved —
    cheap to add now), progression (`level`, `expTotal`, `expUnbanked`, `mon`,
    `statPointsUnspent`, `stats: Dictionary<string,int>` for `body/breath/blade/spirit`),
    world state (`currentRegionId`, `discoveredShrines`, `bossesDefeated`,
    `lootedContainers`, `worldFlags`), `inventory: Inventory`, `equippedCharms`, and
    narrative state (`questStates`, `dialogueSeen`, `npcStates`).

### STEP 12: Narrative Engine, Dialogue Trees & Quest State Machine
*   **Quest structure — hybrid "Archon spine, Grace threads":** the 5 Acts + Prologue are
    full Genshin-Archon-Quest-style linear, unmissable, camera-framed dialogue trees; the 6
    named boss fights get full FromSoft-style pre/post-fight monologues (Soren's scene gets
    real branching — a "lower your blade" choice that is allowed to fail, because that's the
    point); 8–12 optional NPC threads use Elden-Ring-style multi-stage, relocating-NPC
    questlines but are journal-hint-tracked (a `journalHint` string field, Genshin-adjacent
    affordance) rather than opaque or GPS-waypoint-tracked. Item descriptions carry
    environmental lore FromSoft-style.
*   **The single deterministic quest-tick point is shrine rest** — NPC states and quest
    progression advance when the player rests, exactly like Elden Ring's Grace-tick
    convention; this keeps world mutation, quest advancement, and autosave all happening at
    the same moments.
*   **Quest FSM: `Unstarted, Active, ObjectiveComplete, Completed, Failed`** (fifth state
    added to the roadmap's original four — approved, needed for missable NPC threads and the
    Soren branch).
*   **Dialogue data structure:** `DialogueTree` (`ScriptableObject`: dialogueId, startNode,
    `nodes: Dictionary<string, DialogueNode>` — ID-keyed dictionary, not a cyclic
    node-reference graph, so it serializes cleanly and supports hub-and-spoke "ask about…"
    menus). `DialogueNode` carries speaker, text, portrait, voice clip, camera shot marker,
    animation cue, choices, and flag/quest mutations applied on node *display* (not exit, so
    an early quit doesn't lose a visibly-triggered state change). Conditions use a
    whitelisted `DialogueCondition` data type (source/key/op/value), never a dynamically
    evaluated expression string on authored data (code-execution + untestability risk — same
    reasoning the Godot charter's `Expression.parse()` ban gave, generalized: no
    `eval`-equivalent on user- or design-authored strings in this engine either).

### STEP 13: Production Art, Animation Blend Trees, Shaders & Audio Pass
*   **Animator (Unity's `AnimationTree` equivalent):** a 2D Blend Tree (Freeform Directional)
    for locomotion (driven from Step 4's lerped velocity, not raw input, so inertia reads in
    the animation) → per-stance locomotion variant via a blend parameter or sub-state → a
    combat sub-state machine (`Idle/LightA1-3/Heavy1-2/Parry/Block/Hit*/PostureBreak/
    Deathblow/Death/Draw/Sheathe/GripChange`) → clip playback speed scaled by
    `StanceData.attackSpeedScalar` (`Animator.SetFloat` driving a state's Speed multiplier —
    Unity has no direct per-layer timescale node the way Godot's `AnimationNodeTimeScale`
    does; Research to confirm at Step 13 intake whether this needs the Playables API instead
    of vanilla Animator states) → an upper-body Avatar Mask + Animator layer (Spine/Neck/
    Head/Arms/Weapon) for combat masking over locomotion → a full-body override layer
    (weight 1, no mask) for actions that must bypass masking (dodge, posture-break,
    deathblow, death, draw, sheathe).
*   **Stance swap: NO sheathe/draw beat** (would break combo-cancel flow — reference is
    Nioh's ~10-frame cancelable stance switch, not a weapon-sheathe animation). Instead: a
    0.18s upper-body-only "grip change" flourish, new-stance multipliers apply immediately,
    and the 0.15s commitment window matches the Step 3 input buffer exactly so a buffered
    attack during a stance swap lands in the new stance. Full sheathe/draw is reserved for
    combat entry/exit and the post-boss victory flourish only.
*   **Root motion:** OFF for locomotion (Step 4's lerped `V_horizontal` governs movement,
    `Animator.applyRootMotion = false` on the locomotion layer), ON for combat attack states
    only (`OnAnimatorMove()` root-motion-derived velocity overrides the lerp during those
    states specifically — Step 4's formula is scoped to locomotion, not combat animation;
    Research to confirm at Step 13 intake how per-state root motion toggling is best done in
    Unity, since `applyRootMotion` is a single Animator-wide flag, not per-layer).
*   **Footsteps:** material-tagged via a `PhysicMaterial`-keyed lookup or a `SurfaceType`
    component on colliders, sampled via an Animation Event at the contact frame (same
    convention as Step 5's hitbox enable/disable events). Per-region surface sets (Ashlands:
    ash/cinder/stone; Sunken Pines: mud/shallow_water/wet_wood/moss; Mount Shindai:
    snow/ice/stone/gravel; Outskirts: dirt/grass/gravel/plank; Estate: wood/tatami/stone/
    gravel_garden).
*   **Music: Elden Ring sparseness, not Genshin layering.** No melody during exploration or
    normal combat (ambience only, silence is what makes posture/parry reads and boss themes
    land); full FromSoft-style orchestral/choral treatment reserved for the 6 named bosses.
    Research to confirm at Step 13 intake the best Unity equivalent to Godot's
    `AudioStreamInteractive` (multi-clip transitions, fade modes, filler clips) — likely
    Unity's `AudioMixer` snapshots + manual clip-transition scripting, or FMOD/Wwise if this
    project takes on a middleware dependency (open question, not yet decided — do not assume
    either way before Step 13 research). The Step 8.1 50%-HP phase transition must trigger a
    musical phase swell at the exact same frame as the mechanical phase change, whatever the
    chosen audio system.

### STEP 14: Game Economy Balancing, Profiling, Optimization & QA
*   **EXP formula clarification (does not change the locked formula):** `EXP Required = 100 ×
    (Level)^1.5` is the **per-level cost** to advance from that level, not a cumulative
    total. Cumulative totals are derived by summing.
*   **Act-gated level cap (new system, approved):** Prologue 8 / Act I 20 / Act II 35 / Act
    III 50 / Act IV 68 / Act V 85, raised at each boss defeat alongside the existing
    stance-unlock reward — corrects the formula's pacing (it's ~4× flatter than a typical
    Soulslike rune-cost curve and would otherwise let the player over-level).
*   **What a level grants (new, previously undefined):** one player-allocated stat point per
    level across four stats — `body` (HP), `breath` (stamina+posture), `blade` (damage),
    `spirit` (posture damage) — Elden-Ring-style build choice, not automatic growth.
*   **Posture regeneration (new formula):** `R(hp) = R_base × (0.35 + 0.65 × hp_ratio)` —
    enemies regenerate posture more slowly as their HP drops, the mechanical core of the
    Sekiro "chip HP to enable a deathblow" loop.
*   **Crits are not a probability roll:** `EntityDamaged`'s `isCritical` flag is true only on
    deathblow/posture-break executions, never randomized — keeps RNG out of a parry-timing
    genre.
*   **Death penalty (new system):** lose 30% of unbanked EXP (earned since last shrine rest)
    on death, recoverable once by returning to the death location (a `GraveMarker`, same
    `GameObject` type as the boss-arena respawn marker).
*   **Damage formula mitigation budget:** `Damage = (Base+Weapon) × (1 − Armor/(Armor+100))`
    knees at Armor=100 (50% mitigation) — budget regular enemies 0–250 armor, bosses
    150–300, keeping effective mitigation in the 0–75% band.
*   **Performance budgets (60 FPS / 16.6ms, already locked):** ≤2,000 draw calls/frame
    (Unity Profiler / Frame Debugger, not Godot's monitor), ≤40 visible GPU-instanced batches,
    ≤400 active trigger-`Collider` pairs in a 6-enemy fight, ≤8,000 total live `GameObject`s,
    ≤768 live particles (pool 24 `ParticleSystem` emitters, never `Instantiate()` per-hit).
    Toggle `Collider.enabled` (not the parent `GameObject.SetActive`) to toggle hitbox
    monitoring cheaply. Stagger Step 7 perception-sweep ticks per enemy
    (`_perceptionOffset = Random.value * 0.1f`) to avoid synchronized spike frames. Bake
    greybox geometry to real meshes before occlusion-culling bake (helper-only geometry is
    otherwise invisible to the occlusion baker).
*   **Campaign length target: ~20 hours** first playthrough — the single dial that
    region/shrine/NPC-thread/EXP-budget counts above are all calibrated against.

## 📁 FOLDER STRUCTURE
```
Assets/
├── Scripts/
│   ├── Systems/       (EventBus.cs, GameState.cs, SoundManager.cs, SaveSystem)
│   ├── Player/         (movement, dodge, camera rig, stance handling)
│   ├── AI/              (enemy FSM, perception, patrol)
│   ├── Combat/       (Combatant, hitbox/hurtbox resolution, juice engine)
│   ├── Interaction/     (shrines, chests, doors, NPCs)
│   ├── UI/                    (HUD, menus, dialogue UI)
│   └── Utils/               (shared helpers, extension methods)
├── Editor/                (editor-only scripts, batch-mode/QA hooks — e.g. Ping.cs)
├── Scenes/
│   ├── Levels/                (Prologue + Acts I-V, per region)
│   └── Sandbox/            (isolated mechanic test scenes)
├── Prefabs/
│   ├── Player/ Enemies/ Bosses/ Interactables/ Environment/
├── ScriptableObjects/       (StanceData, ItemData, DialogueTree, RegionGraph assets)
├── Art/
│   ├── Models/ Materials/ Animations/ Shaders/
├── Audio/
│   ├── Music/ SFX/ Ambience/
├── Settings/                    (Input Actions asset, difficulty/tuning ScriptableObjects)
├── Plugins/                     (third-party assets/SDKs, isolated)
└── Tests/
    ├── EditMode/                 (pure logic unit tests — damage math, EXP curve, save round-trip)
    └── PlayMode/                  (runtime behavior tests)
docs/                             (Worklog.md, Tasks/, DesignDoc.md, ContentPlan.md — repo root, not under Assets/)
legacy-godot/                (archived Godot 4 implementation — repo root, not under Assets/, excluded from Unity import)
```

## 👥 6. STUDIO HIERARCHY — DIRECTOR/SUBAGENT WORKFLOW

Work runs as a small studio with distinct roles, implemented as separate subagents (via the
Agent tool) coordinated by the **Director** — the top-level agent in the conversation. The
Director never does research, implementation, or testing itself; it assigns, routes,
reviews, and is the only one allowed to mark a step done.

Every step in Section 4 goes through this pipeline:

1.  **Director — intake**
    *   Turns the roadmap step into a task brief: goal, affected systems (Scripts/Scenes/
        ScriptableObjects/Prefabs), constraints, definition of done.
    *   Opens `docs/Tasks/<date>-<slug>.md` from the template (see below) before assigning
        anything.

2.  **Research Agent** (`Explore` for quick lookups, `general-purpose` with WebSearch/WebFetch
    for anything needing Unity docs or external references)
    *   Investigates existing systems it touches, relevant Unity APIs/docs (`CharacterController`,
        trigger `Collider`s, `Physics.Raycast`, `Animator`/Animation Events, `ScriptableObject`,
        Input System package, Cinemachine, etc.), how similar mechanics are usually implemented
        in Unity 3D, and constraints (physics tick rate, script execution order, `Time.timeScale`
        semantics, occlusion culling bake times, package/version compatibility with
        6000.5.5f1).
    *   Never guesses at Unity API behavior — verifies against docs or existing project code.
    *   Reports findings back to the Director; does not write implementation code.
    *   Findings get logged in the task file before implementation starts.

3.  **Director — approach sign-off**
    *   For anything nontrivial, states the chosen approach and tradeoffs (informed by
        Research) before implementation starts — no silently picking one.

4.  **Implementation Agent** (`general-purpose`)
    *   Given the approved approach and research findings, writes the C#/scene changes. Does
        not re-do research or decide the approach.
    *   Reports back a summary of what changed and why.

5.  **QA/Test Agent** (`general-purpose`)
    *   Runs the project (Unity headless batchmode or Editor Play mode) and exercises the
        feature — not just "it compiles." Where unit tests make sense (damage formulas, EXP
        scaling, save/load round-trips), it runs or writes Unity Test Framework (`EditMode`/
        `PlayMode`) tests.
    *   Reports pass/fail with specifics: repro steps, exact error, affected file/line.
    *   Never fixes issues itself — only reports them back to the Director.

6.  **Fix loop**
    *   If QA reports a failure, the Director routes the QA report back to the Implementation
        Agent (with the research doc still available). Re-run Implementation → QA until QA
        reports a clean pass. Log every iteration (attempt number, what was tried, what QA
        found) in the task file — do not overwrite prior attempts.
    *   No step is marked done while QA reports unresolved issues.

### Standing test-coverage gate (added 2026-08-01)
Every future step's QA pass must include a **real, tool-measured ≥80% line coverage** of the
step's newly-added logic-bearing code (`Assembly-CSharp`, measured via
`com.unity.testtools.codecoverage` through the verified batchmode CLI mechanism — see
`docs/Tasks/2026-08-01-test-coverage-pass-1.md` for the exact command and `pathFilters`
syntax), not an estimate. Generated code (e.g. Input System `.inputactions` C# codegen),
zero-IL files (interfaces, field-only data classes), and classes that would require a
production-code restructure to test (e.g. an `Assembly-CSharp`/PlayMode-asmdef split) may be
excluded from a step's coverage denominator **only with an explicit, logged Director
justification in that step's task file** — never silently, and never by writing low-value/
tautological tests just to move the number. If a measured pass falls short of 80%, the
Director either (a) rules on a scope-boundary correction with justification and re-measures,
or (b) reports the shortfall and keeps the step open — it is not committed/pushed as if
satisfied. (Test Coverage Pass 1, covering all pre-existing work through Phase 3 slice 1,
landed at 96.4% against an 85% target — see the task file above for the full precedent this
convention is modeled on.)

### Model tiering for subagents
When spawning pipeline subagents via the Agent tool, use:
*   **Research Agent:** `opus` — research/design judgment calls get the strongest model.
*   **Implementation Agent:** `sonnet` (default, no override needed).
*   **QA/Test Agent:** `sonnet` (default, no override needed).
*   **Director:** whatever model fits the session — not spawned via the Agent tool, so no
    per-call override applies; left to judgment rather than fixed.

7.  **Director — final review**
    *   Re-reads the diff critically as if reviewing a coworker's PR: correctness, obvious
        bugs, dead code, inconsistent naming, missed edge cases (e.g. enemy losing perception
        mid-telegraph, save/load state, pause behavior, event unsubscription/leaks — Unity's
        C# events need explicit `-=` cleanup on destroy or they leak references, unlike
        Godot's signal auto-disconnect-on-free), and S.O.L.I.D. violations per Section 2's
        coding standard (god-classes, type-check branching that should be polymorphism, fat
        interfaces, concrete-type coupling that should go through `EventBus`/interfaces).
    *   Flags anything questionable explicitly rather than staying silent about it.
    *   Only after this review, and a clean QA pass, does the Director mark the step complete
        and finalize the task log with a sign-off summary.

Skipping the full pipeline is only acceptable for truly trivial changes (typo fixes, comment
edits, renames with no behavior change) — these can be done directly by the Director, still
logged as a one-line entry in `docs/Worklog.md`.

**Headless smoke-check convention** (carried over from the NinjaGame project's established
pattern): `Assets/Editor/Ping.cs` defines a static `Ping.Run()` that logs `PING_OK`. QA
verifies the project actually runs (not just compiles) via:
```
Unity.exe -batchmode -nographics -projectPath "Revitalised Return" -executeMethod Ping.Run -quit -logFile <log>
```
and checks the log for `PING_OK` with a clean exit code.

## 📝 5. WORKLOG LOGGING TEMPLATE
Log every cycle in `docs/Worklog.md`:

```markdown
## [Step ID / Feature] - [YYYY-MM-DD]

### 🧪 Tests
[Objective, Method, Outcome]

### 🔄 Changes
[Modified scripts, scenes, ScriptableObjects, prefabs]

### 🔴 Bugs/Cause
[Flaw description & root cause]

### 🛠️ Fix/Prevention
[Technical resolution & regression strategy]

### 💪 Game Feel Wins
[Performance/responsiveness notes]
```

Every step also gets a full log file at `docs/Tasks/<date>-<slug>.md` (created from
`docs/Tasks/_template.md`) capturing: task brief, research findings, chosen approach +
tradeoffs, implementation summary, every QA iteration (attempt + result, not just the final
one), and the Director's final sign-off. Nothing is marked complete without its task log
fully filled in and its one-line entry added to `docs/Worklog.md`.

## Current status
**2026-07-31: engine pivot from Godot 4/GDScript to Unity 6000.5.5f1/C#.** The prior Godot
implementation (Steps 1-5 committed + QA'd, Step 6 implemented/QA-passed but never closed
out) is archived at `legacy-godot/`, not deleted — see that folder's `README.md` and
`docs/Tasks/2026-07-31-godot-to-unity-pivot.md` for the full decision record. The Steps
9-14 design pass (`docs/DesignDoc.md`, `docs/ContentPlan.md`) is unaffected by the pivot —
it's content/design work, not engine-specific.

A fresh Unity project skeleton now exists at the repo root (Editor `6000.5.5f1`, matching
NinjaGame's installed version) with the `Assets/` folder structure above in place and the
`Ping.cs` headless smoke-check script. The Unity-MCP plugin (`IvanMurzak/Unity-MCP`) is
installed and connected — pipeline agents use its live-Editor tools (scene/GameObject/
component creation, `console-get-logs`, `script-execute`, etc.) to verify changes against the
running Editor rather than editing files blind. **Step 2 (Unity port) is done:**
`EventBus.cs`/`GameState.cs` are fully implemented per 2.1/2.2 (event raise-helpers, the real
`SetState()` cursor-lock/`Time.timeScale` transition table, a `RuntimeInitializeOnLoadMethod`
singleton safety net for entering Sandbox scenes directly), and `Assets/Scenes/Bootstrap.unity`
exists as the registered build-index-0 entry scene holding the `GameState` singleton — see
`docs/Tasks/2026-07-31-step-2-unity-eventbus-gamestate.md`. There's also a separate paused
side-task, `docs/Tasks/2026-07-31-ui-systems-skeleton.md` (Input Actions asset, MainMenu/
Settings scene, minimap, inventory data + UI stub) — its Input System compile blocker is
resolved but its 5 deliverables are still unstarted; not yet resumed.

**2026-07-31: user-directed sequencing deviation, logged per Director ruling.** At the user's
explicit request, work is reprioritized to **Player base character → UI → Combat system**
(user-facing/playable-first order) rather than continuing the charter's strict Step 3→14
numeric order. The 14-step pipeline in Section 4 remains the documented reference sequence —
this is a called-out reprioritization, not an amendment to that section — so each piece of
this work should still be reconciled back to the step number(s) it actually satisfies (e.g.
"Player base character" = Steps 3+4 compressed: Input System + the 0.15s action buffer +
`CharacterController` kinematics + dodge roll, with a placeholder primitive mesh standing in
for Step 13's real art). **Next action:** Director opens a task brief for the Player base
character (functional placeholder: `CharacterController`, placeholder mesh, camera rig, real
input-driven movement — not just a static mesh), using `legacy-godot/`'s validated Step 3/4
formulas/timings as the porting reference. UI and Combat system phases follow after, each
gets its own task brief per the Director/subagent pipeline in Section 6. All C# code across
all three phases must follow the S.O.L.I.D. standard already locked in Section 2.

**Player base character (Phase 1) is done and user-confirmed working in Play Mode.** Input
Actions asset (`Assets/Settings/PlayerControls.inputactions`), a S.O.L.I.D.-split script set
under `Assets/Scripts/Player/` (`InputBuffer`, `IMovementInput`, `IInvulnerabilityProvider`,
`CameraRelativeInput`, `PlayerInputReader`, `PlayerMotor`, `DodgeAbility`, `MeshLean`,
`PlayerRoot` — the last as a single explicit per-frame orchestrator, not relying on Script
Execution Order), a Cinemachine 3.x third-person camera rig, `Assets/Prefabs/Player/Player.prefab`,
and `Assets/Scenes/Sandbox/MovementTest.unity` to exercise it — see
`docs/Tasks/2026-07-31-player-base-character.md`. A real post-signoff bug was caught by the
user's manual Play Mode test (controls silently no-op'd because `GameState` never reached
`Playing` in a Sandbox scene, which skips the `Bootstrap` flow) — fixed via
`Assets/Scripts/Systems/SandboxAutoPlay.cs`, confirmed working. **Standing lesson:** no
Play-Mode-control MCP tool has been found in this toolset across two pipeline cycles now — a
human manual Play Mode pass is required to catch this class of bug; static/compile
verification alone is not sufficient sign-off for gameplay-behavior tasks.

**UI systems (Phase 2) is done, QA passed:** a S.O.L.I.D.-split reactive HUD per Step 11's
locked design (`Assets/Scripts/UI/`: `HealthBar`/`StaminaBar`/`PostureBar` each independently
subscribed to their one `EventBus` event, a shared `VitalsFader` for the 5s-idle-fade rule,
`StanceDiamond`, `NoticeDisplay`, thin `HUDRoot`), built live into `MovementTest.unity`; a
`MainMenu.unity` scene (Play/Settings/Quit, Settings stub, intentionally not wired into
`EditorBuildSettings` yet); and 4 placeholder `StanceData` assets
(`Assets/ScriptableObjects/Stances/`) since the stance diamond needed something real to
reference. Everything works against zero real emitters right now (no health/stamina/posture
system exists yet — that's Combat/Phase 3) with sane defaults, by design. See
`docs/Tasks/2026-07-31-ui-systems-phase2.md`; supersedes the paused
`docs/Tasks/2026-07-31-ui-systems-skeleton.md`'s HUD/menu deliverables (kept for its still-useful
Input System blocker-resolution research, not deleted). **Same standing gap as Phase 1:** HUD
render/update behavior not yet manually confirmed in Play Mode.

**Phase 3, slice 1 (Player Vitals + Stance Switching) is done, QA passed clean, and
user-confirmed working in Play Mode** (dodge visibly drains/regens stamina, Q/Tab visibly
cycles the stance diamond) — `PlayerVitals`/`StanceController` per
`docs/Tasks/2026-08-01-player-vitals-stance-switching.md`. A related cursor bug (same root
cause class as the Phase 1 `SandboxAutoPlay` fix — `MainMenu.unity` never called
`GameState.SetState(MainMenu)`, so the cursor never unlocked) was found and fixed via
`Assets/Scripts/UI/MainMenuAutoState.cs`.

**2026-08-01: the ad-hoc weapon-hitbox/enemy-AI slice work (a "combat-resolution-core" task
brief, `docs/Tasks/2026-08-01-combat-resolution-core.md`) was cancelled by the user before
implementation started** — no code was written against it. Per the user's explicit
instruction, the project **reverts to the charter's strict Step 3→14 numeric order** rather
than continuing the character→UI→combat reprioritization further. Steps 1-4 are done (via the
reprioritized phases above, which map cleanly onto them). Step 5 (Stance Engine, Hitbox
Registration & Parry Logic) is **partially done** — the stance data model (`StanceData`,
`StanceController`) and vitals (`PlayerVitals`) exist, but hitbox/hurtbox trigger-collider
resolution, damage math, and parry/block logic do not yet.

**2026-08-01: Test Coverage Pass 1 is done.** At the user's request, established a real,
tool-measured coverage baseline across all pre-existing work (Steps 1-4 + partial 5/11) before
continuing the roadmap: 133 EditMode tests, **96.4% line coverage** (target 85%), 0 failures,
QA-verified as meaningful (no tautological/padding tests) — see
`docs/Tasks/2026-08-01-test-coverage-pass-1.md`. Two justified, logged scope exclusions:
generated code (`PlayerControls.cs`) and two classes genuinely blocked by a real Unity
assembly-definition constraint (`NoticeDisplay.cs`/`VitalsFader.cs` — PlayMode-testing them
would require restructuring `Assets/Scripts/` into its own `.asmdef`, deferred as a named gap
rather than forced through as a side effect of a testing task). **This established a standing
80%-coverage gate on every future step**, now logged in Section 6.

**2026-08-01: Step 5 (Unity port, full spec) is done.** `WeaponHitbox.cs` implements charter
5.2's full collision-resolution order (parry check → block check → hit check) — a parry fully
skips the defender's damage and deals 40% posture to the attacker while interrupting their
swing; a block reduces damage ×0.2 but leaves posture damage at full value (the explicit
"bleed-through" rule); a normal hit uses the locked `Damage = (Base+Weapon) × (1−Armor/
(Armor+100))` formula with stance multipliers. `StanceData`'s 4 assets now carry
differentiated combat-tuning values. Self-hit prevention verified via a real Physics Layer
collision matrix (`PlayerHitbox`/`PlayerHurtbox`/`EnemyHitbox`/`EnemyHurtbox`). A
`TrainingDummy` prefab exists as a hittable target. `IsBlocking` is deliberately unwired to
any input this task — a standing charter gap (no dedicated `block` action exists in the Input
Actions asset) carried forward from this project's own Godot-era Step 5 precedent, not
silently invented. **183 tests, 97% measured line coverage** (target 80%, standing gate),
independently double-confirmed by both QA and the Director directly. See
`docs/Tasks/2026-08-01-step-5-stances-hitboxes-parry.md`. **Logged scope boundary, not a
hidden gap:** parry/block resolution against a live attacker cannot be manually Play-Mode-
verified by a human until Step 7/8 delivers an actual enemy that can attack — proven instead
via unit tests scrutinized specifically for this risk (genuinely-distinct attacker/defender
mocks, not a shared mock that could mask a wrong-target bug).

**2026-08-01: Step 6 (Unity port) is done, pending human Play Mode confirmation.**
`HitStopCoordinator`/`CameraTrauma`/`HitFlash`/`SparkPool`/`TrailActivator` (all
`Assets/Scripts/Combat/Juice/`) implement charter 6.1-6.3: freeze-frame hit-stop (locked
0.03-0.06s range, correctly restores `Time.timeScale=1f`), camera trauma shake via
Cinemachine's own `CinemachineBasicMultiChannelPerlin` (Ruling: engine-idiom substitution for
the noise sampler, locked trauma-squared-decay formula unchanged — `AmplitudeGain =
maxAmplitude × trauma²`, decay `1.5/s`), a hand-written ShaderLab `_FlashIntensity` hit-flash
applied via `MaterialPropertyBlock`, a 24-instance pooled spark `ParticleSystem` (never
`Instantiate()`s per-hit), and a `TrailRenderer` mirroring `AttackController`'s attack window.
Contact point/normal for VFX are approximated directly in `WeaponHitbox` (Ruling: local direct
calls, not an `EventBus.EntityDamaged` signature change — that event stays untouched). **227
tests, 97.1% measured coverage** (target 80%), independently double-confirmed by QA's static
review (MCP was unavailable that pass — a server-side enrollment issue, not a shortcut) and
the Director's own direct batchmode re-measurement. See
`docs/Tasks/2026-08-01-step-6-juice-engine.md`. **This step is unusually feel-dependent — the
mandatory human Play Mode pass has not happened yet**, more so than any prior step.

**Next action:** a human Play Mode pass on `MovementTest.unity` (hit-stop freeze, camera
shake, hit-flash, spark VFX — attack the training dummy to trigger all of it), then Director
opens the Step 7 task brief (AI Architecture, Perception & Behavior) in strict 14-step order,
carrying the same 80% coverage gate.

**2026-08-01: Step 7 (AI Architecture, Perception & Behavior) is done.** (Implementation
Attempt 1 was cut off mid-task by a session limit — committed as an explicit WIP checkpoint,
then resumed and completed in Attempt 2 the same day, with the checkpoint's claims
independently re-verified before continuing, not trusted blindly.) `EnemyPerception.cs`
implements the vision cone (45° half-angle, 18m range, 0.1s tick with the charter-14
`_perceptionOffset` stagger) and acoustic detection (8m sphere, dodge+attack as the noise
triggers — no sprint mechanic exists yet, logged gap, not invented). A real, live-verified
finding worth remembering for any future physics-query work: **`Physics.autoSyncTransforms`
is `false` in EditMode** — a raycast/`OverlapSphere` against a just-moved transform silently
returns false/empty unless `Physics.SyncTransforms()` is called first, and
`Physics.queriesHitTriggers` defaults `true` so LOS raycasts need
`QueryTriggerInteraction.Ignore` or trigger hurtboxes falsely block sight — both fixes are in
`EnemyPerception.cs` and specifically proven by dedicated tests. `EnemyBrain.cs` implements
the full charter 7.2 FSM (enum+switch, matching `GameState.cs`'s house style — a full
per-state-class pattern was explicitly rejected as over-engineering for a closed 5-state
graph with one enemy type): `Patrol`→`Investigate`→`Telegraph`→`Attack`→`Recovery`→back to
`Investigate` (not directly to `Attack` — the charter's own non-obvious edge, confirmed
correct). The enemy reuses Step 5's `AttackController`/`WeaponHitbox` **completely
unmodified** — this is load-bearing, not just convenient: it means the player's parry/block
resolution (interrupt, posture damage, `ParryExecuted`) now works against a real opponent for
the first time, finally closing the scope boundary Step 5 explicitly logged at its own
sign-off. **272 tests, 97% measured coverage** (target 80%), independently double-confirmed
by both QA and the Director directly. See `docs/Tasks/2026-08-01-step-7-ai-perception-fsm.md`.
**Known, still-open item:** the mandatory human Play Mode pass — specifically including an
attempt to parry the enemy's attack — hasn't happened yet. This is the single most meaningful
manual test remaining in the project so far.

**Next action:** a human Play Mode pass on `MovementTest.unity` (does the enemy patrol,
notice the player, investigate, telegraph, attack — and critically, does parrying its attack
actually work), then Director opens the Step 8 task brief (Boss Mechanics) — though Research
should confirm at that step's intake whether a full boss deserves to come before Steps 9-12's
world/interaction/narrative work, since a boss needs an arena (Step 9 territory) to mean
anything, rather than assuming the charter's strict numeric order is still the right call
verbatim.
