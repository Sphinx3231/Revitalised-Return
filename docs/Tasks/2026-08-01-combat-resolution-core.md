# Combat Resolution Core: Weapon Hitbox + Hurtbox + Damage Math (Phase 3, slice 2) — 2026-08-01

## Task Brief (Director)
- **Goal:** at the user's request to continue the combat system (weapons + enemies with unique
  attack patterns), build the actual hit-resolution core first — charter Step 5.2's
  hitbox/hurtbox trigger-collider sweep, damage/posture math (charter 2's locked formula), and
  stance-driven multipliers (charter 5.1) — validated against a stationary "training dummy"
  target, since there's no point building enemy AI (slice 3) before the combat math it will
  use is proven correct. This is charter Step 5 proper (stance engine was already
  data-modeled in Phase 2/slice 1; this task adds the actual combat resolution logic).
- **Affected systems:** `Assets/Scripts/Combat/` (new: `IDamageable`, `WeaponHitbox`,
  `Hurtbox`, `AttackController`, `TrainingDummy`; modified: `StanceData.cs` gains the combat
  tuning fields its own `TODO(Step 5)` comment already named), `Assets/Scripts/Player/`
  (`PlayerVitals` implements `IDamageable`; `PlayerRoot`/`PlayerInputReader` wire attack input
  consumption), Physics Layers (`ProjectSettings/TagManager.asset` — new layers +
  Layer Collision Matrix), `Assets/Prefabs/Player/Player.prefab` (weapon hitbox +  hurtbox
  child objects), a new `Assets/Prefabs/Enemies/TrainingDummy.prefab`, `docs/Worklog.md`.
- **Constraints:**
  - **S.O.L.I.D. mandatory.** `WeaponHitbox` only detects/reports overlap + resolves the hit
    via `IDamageable`, it does not own attack timing. `AttackController` owns attack-input
    consumption + hitbox enable/disable timing, not damage math. `IDamageable` is the single
    abstraction both `PlayerVitals` and the training dummy's health component implement, so
    hit-resolution code (`WeaponHitbox`) never needs to know whether it hit the player or an
    enemy — genuine Dependency Inversion, matching the pattern already established for
    `IStaminaSource`/`IMovementInput`.
  - **Damage formula is locked** (charter 2): `Damage = (Base + Weapon) × (1 − Armor /
    (Armor + 100))`. Stance multipliers (charter 5.1): `baseDamageMultiplier`,
    `postureDamageMultiplier` applied on top. No armor system exists yet — treat `Armor = 0`
    for both player and dummy this slice (mitigation term becomes a no-op multiplier of 1,
    not skipped/hardcoded away — so armor can be wired in later without touching this math).
  - **Self-hit prevention via Physics Layers** (charter 5.2/14, non-negotiable): separate
    `PlayerHitbox`/`PlayerHurtbox`/`EnemyHitbox`/`EnemyHurtbox` layers, Layer Collision Matrix
    restricted so a hitbox only overlaps its opposing hurtbox layer. Toggle `Collider.enabled`
    (never `GameObject.SetActive`) to open/close the active hitbox window, per charter 14's
    explicit performance rule (already the pattern `DodgeAbility` uses for i-frames).
  - **No real attack animations exist yet** (Step 13 territory, placeholder capsule mesh, no
    Animator). Charter 5.2 specifies Animation-Event-driven hitbox timing — not achievable yet.
    This slice uses a **timed-window placeholder** instead (hitbox opens for a fixed duration
    after attack input, scaled by the current stance's `attackSpeedScalar`), explicitly marked
    `TODO(Step 13)` for the real Animation Event handoff — consistent with the project's
    existing placeholder-marking convention (`TODO(Step N)` comments throughout).
  - **Crits are not a probability roll** (charter 14, already locked) — `isCritical` in
    `EventBus.EntityDamaged` stays `false` for all hits this slice (no deathblow/posture-break
    execution system exists yet to legitimately set it true).
  - Parry/block resolution (charter 5.2's second half) is **out of scope this slice** — no
    target will attempt to parry/block yet (training dummy is passive), and building
    unused parry-response logic now would be exactly the kind of speculative code the
    project's own conventions warn against. Logged as deferred to a future slice once an
    enemy can actually initiate an attack the player might want to parry.
  - Must respect `GameState.IsPlayerInputLocked()` for attack input, same as all other player
    input.
  - Use live Unity-MCP tools for scene/prefab/Layer construction and compile verification, and
    the by-now-standard safety checks (Edit-mode-only mutation, wire both prefab AND scene
    instance, verify wiring by read-back). **Mandatory human Play Mode pass required before
    sign-off**, per the standing lesson from every prior phase this session.
- **Definition of done:**
  - `StanceData.cs` has real `baseDamageMultiplier`/`postureDamageMultiplier`/
    `attackSpeedScalar` fields (per charter 5.1), the 4 existing placeholder assets get
    reasonable placeholder values (not left at C#'s default `0`, which would zero all damage).
  - Pressing light/heavy attack near the training dummy opens a weapon hitbox for a timed
    window, and a successful overlap correctly reduces the dummy's health via the locked
    damage formula and fires `EventBus.EntityDamaged`.
  - Physics Layers + Layer Collision Matrix set up so a player hitbox cannot hit the player's
    own hurtbox (self-hit prevention verified, not just configured).
  - A `TrainingDummy` prefab exists in `MovementTest.unity`, stationary, with visible
    health that can be seen decreasing (simple debug/console log or a minimal health-bar
    stub is acceptable — a full enemy HUD is out of scope, that's Step 11 territory already
    covered for the player only).
  - Project compiles clean (MCP `console-get-logs` zero `error CS`).
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Director, self-researched)
Charter Step 5.2 already fully specifies the hitbox/hurtbox layer-separation pattern, the
`OnTriggerEnter` resolution structure, and the Animation-Event hitbox-toggle convention — no
new external Unity API surface beyond what charter 14's "toggle `Collider.enabled`" rule and
the already-used trigger-`Collider` pattern cover. Confirmed by direct file read:
`DodgeAbility.cs` already has an unassigned `hurtboxCollider` serialized field from Phase 1,
placed there in anticipation of exactly this slice — this task is what finally gives it a
real `Collider` to reference. `StanceData.cs`'s own `TODO(Step 5)` comment already names the
exact fields this task adds. No separate Research Agent pass judged necessary — narrow,
already-specified surface, consistent with the precedent set by two prior self-researched
sections this session (`ui-systems-skeleton.md`, `player-vitals-stance-switching.md`).
One judgment call resolved here rather than left ambiguous: Unity Physics Layers are a
project-wide resource (32 max, shared across all future work) — this task claims 4 new layer
slots (`PlayerHitbox`, `PlayerHurtbox`, `EnemyHitbox`, `EnemyHurtbox`) now, rather than
generic `Hitbox`/`Hurtbox` layers with a runtime team-tag check, because charter 5.2 explicitly
specifies layer-based (not tag-based) self-hit prevention, and a 4-layer split scales cleanly
to the multi-enemy-type future (slice 4) without rework.

## Approach & Tradeoffs (Director sign-off)
- **`IDamageable.cs`** (new interface, `Assets/Scripts/Combat/`): `void ApplyDamage(float
  amount, bool isCritical)`, `void ApplyPostureDamage(float amount)`, `Transform DamageTransform
  { get; }` (for `EventBus.EntityDamaged`'s `Transform target` parameter). `PlayerVitals`
  implements it (deducts health/posture, fires `EventBus.RaisePlayerHealthChanged`/
  `RaisePlayerPostureChanged` — reuses its own existing event-raising, no duplicate logic).
  A new minimal `DummyHealth.cs` implements it for the training dummy (no stance-multiplier
  concerns of its own — it's the *target*, multipliers are applied by the *attacker's*
  hitbox before calling `ApplyDamage`).
- **`WeaponHitbox.cs`** (new, `Assets/Scripts/Combat/`): trigger `Collider` (assumed a `BoxCollider`
  on a weapon-pivot child, `isTrigger=true`, starts `enabled=false`). Serialized `float
  baseDamage`, `StanceData currentStance` (assigned externally by `AttackController` before
  each opening, not owned here — keeps `WeaponHitbox` a dumb resolver). `OnTriggerEnter`:
  resolve `IDamageable` on the other collider (or its parent — check both, matching how a
  hitbox child sits under a differently-shaped hurtbox root), compute
  `damage = (baseDamage) × currentStance.baseDamageMultiplier × (1 − 0f/(0f+100f))` — the
  `Armor=0` no-op term written explicitly per the brief's constraint — call
  `ApplyDamage(damage, false)` and `ApplyPostureDamage(baseDamage × currentStance
  .postureDamageMultiplier)`, then fire `EventBus.RaiseEntityDamaged(target.DamageTransform,
  damage, false)`. Tracks already-hit targets this activation window in a `HashSet` to avoid
  multi-hit-per-swing from a single overlap staying resident across frames (a real bug class
  trigger colliders are prone to) — cleared when the hitbox closes.
- **`AttackController.cs`** (new, `Assets/Scripts/Player/` — player-specific orchestration,
  mirrors `DodgeAbility`'s pattern): serialized `WeaponHitbox weaponHitbox`, `StanceController
  stanceController` (to read the current stance's `attackSpeedScalar`), `float
  lightAttackWindowSeconds = 0.2f`/`heavyAttackWindowSeconds = 0.35f` (placeholder timings,
  explicitly `TODO(Step 13)` to replace with Animation Events). Public `TickAttack(float
  deltaTime)` explicit-tick method (no own `Update()`, called by `PlayerRoot`, matching the
  established pattern) advances an open-window timer and closes the hitbox
  (`weaponHitbox.Collider.enabled = false`) when it expires. `TryLightAttack()`/
  `TryHeavyAttack()` (called by `PlayerRoot` on buffered-action consumption) open the hitbox,
  set `weaponHitbox.baseDamage`/`currentStance`, start the window timer scaled by
  `stanceController.CurrentStance.attackSpeedScalar`.
- **`PlayerRoot.cs`** changes: add `AttackController attackController` field; in `Update()`,
  consume `InputBuffer.TryConsume(LightAttack)`/`TryConsume(HeavyAttack)` (mirroring the
  existing dodge-consume step) and call the matching `TryXAttack()`; add
  `attackController.TickAttack(deltaTime)` to the explicit tick order (after dodge tick,
  alongside motor tick — attacks and dodges are mutually exclusive states in the eventual
  full design, but nothing enforces that yet this slice; logged as a known simplification,
  not a hidden gap, since there's no animation layer yet to make that mutual exclusion
  visible/meaningful).
- **Physics Layers:** add `PlayerHitbox`/`PlayerHurtbox`/`EnemyHitbox`/`EnemyHurtbox` via
  `script-execute` (Tag Manager is asset-based, not a dedicated MCP tool — same fallback
  approach already used for `EditorBuildSettings` in Step 2). Layer Collision Matrix:
  `PlayerHitbox` × `EnemyHurtbox` = true, `EnemyHitbox` × `PlayerHurtbox` = true, all other
  combinations among these 4 = false (explicit self-hit prevention).
- **`StanceData.cs`**: add `baseDamageMultiplier=1.0f`, `postureDamageMultiplier=1.0f`,
  `attackSpeedScalar=1.0f` as sensible neutral defaults on the field declarations themselves,
  then set the 4 existing assets to charter-flavor-appropriate placeholder values (Stone:
  high posture/slow speed, Water: fast/low posture, Flame: balanced, Wind: balanced) — real
  tuning is explicitly a later balancing pass (charter Step 14), these are directionally
  correct placeholders, not final numbers, logged as such.
- **Training dummy:** a simple static capsule/cylinder primitive with `DummyHealth`
  (`IDamageable`) + an `EnemyHurtbox`-layer trigger `Collider`, placed in `MovementTest.unity`
  a few meters from the player spawn. No AI, no movement, no counter-attack — purely a
  hittable target to validate the combat math end-to-end. `DummyHealth` logs to the Console
  on damage (`Debug.Log`) as its "visible health" stub, per the DoD's explicit minimal-bar
  allowance.