# Player Vitals + Stance Switching (Phase 3, slice 1) — 2026-08-01

## Task Brief (Director)
- **Goal:** at the user's explicit request ("make them functional"), give the Phase 2 HUD real,
  changing data to display instead of static defaults. This is the first slice of Phase 3
  (Combat system): a `PlayerVitals` component owning real health/stamina/posture values and
  firing the `EventBus` events the HUD already subscribes to, plus functional stance-switching
  (Q/Tab cycling through the 4 `StanceData` assets from Phase 2, firing `StanceSwapped`).
  **Explicitly out of scope this slice:** hitboxes, damage resolution, parry/block logic,
  light/heavy attack — those need an actual opponent (enemy/hurtbox) to test meaningfully and
  are a larger, separate continuation of Phase 3 (charter Step 5's full spec).
- **Affected systems:** `Assets/Scripts/Player/` (new: `PlayerVitals`, `StanceController`;
  modified: `PlayerInputReader` gains direct stance_next/stance_prev events, `DodgeAbility`
  loses its internal stamina stub in favor of querying `PlayerVitals`, `PlayerRoot` wires the
  new components into its explicit tick order), `docs/Worklog.md`. Does not touch
  `Assets/Scripts/UI/*` — the HUD already correctly subscribes to these events from Phase 2,
  no HUD changes should be needed.
- **Constraints:**
  - **S.O.L.I.D. mandatory**, matching the established Player-script pattern: `PlayerVitals`
    owns exactly health/stamina/posture state + regen, nothing else. `StanceController` owns
    exactly current-stance state + cycling, nothing else. Neither touches Input directly;
    `PlayerRoot` remains the single orchestrator reading input and calling into both, per its
    own documented explicit-tick-order design (do not give either component its own
    `Update()`).
  - Stamina numbers are already locked from Phase 1 research (ported from legacy Godot,
    documented in `docs/Tasks/2026-07-31-player-base-character.md`'s Research Findings):
    dodge cost `20.0`, regen rate `10.0`/s, regen pause `1.2s` after a stamina-costing action.
    Health/posture have no locked numbers yet (no damage system exists) — use `100`/`100` as
    max/current defaults for both, no regen logic needed for them yet (nothing drains them).
  - `DodgeAbility`'s existing internal `stamina` field/stub must be removed in favor of
    querying `PlayerVitals` (via a small interface, e.g. `IStaminaSource` with
    `bool TrySpend(float amount)`, consistent with the `IMovementInput`/`IInvulnerabilityProvider`
    pattern already established) — do not leave two separate stamina trackers.
  - Stance switching is a **direct, unbuffered action** — charter 3.2's 0.15s buffer is
    explicitly scoped to light_attack/heavy_attack/parry/dodge only, not stance_next/prev, so
    `PlayerInputReader` should expose stance switches as a plain event, not push them through
    `InputBuffer`.
  - Must gate on `GameState.IsPlayerInputLocked()`, same as all other player input.
  - Use live Unity-MCP tools for wiring/verification. **Given the two prior lessons this
    session:** (1) never mutate the scene while the Editor is in Play Mode — check via a
    `scene-save` probe first if unsure; (2) a human manual Play Mode pass is required before
    this is treated as gameplay-verified — flag it explicitly, same as the last two phases.
- **Definition of done:**
  - `PlayerVitals` fires `EventBus.RaisePlayerHealthChanged`/`RaisePlayerStaminaChanged`/
    `RaisePlayerPostureChanged` on `Start` (so the HUD syncs to real values immediately, not
    just coincidentally matching its own hardcoded default) and whenever stamina changes.
  - Dodging actually costs stamina now (via `PlayerVitals`, not `DodgeAbility`'s old stub),
    stamina regenerates at the locked rate after the locked pause, and the Stamina bar visibly
    reflects this in Play Mode.
  - Pressing Q/Tab in Play Mode cycles the stance and the HUD's stance diamond visibly
    highlights the new selection.
  - Project compiles clean (MCP `console-get-logs` zero `error CS`).
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Director, self-researched)
This slice reuses patterns and locked numbers already fully researched and QA'd in Phase 1
(`docs/Tasks/2026-07-31-player-base-character.md`) and Phase 2
(`docs/Tasks/2026-07-31-ui-systems-phase2.md`) — no new external API surface is introduced
(no new Unity systems, just new plain C#/MonoBehaviour classes following the exact
input-reading and EventBus-subscription patterns already verified working). A full Research
Agent pass was judged unnecessary for this reason; narrow enough for direct Director
self-research, consistent with the precedent set by `docs/Tasks/2026-07-31-ui-systems-skeleton.md`'s
self-researched sections. Confirmed by direct file read before writing this brief:
`PlayerInputReader.cs` already has the exact `performed +=`/`-=` pattern to extend for
stance_next/stance_prev; `DodgeAbility.cs`'s `stamina` field is a private serialized stub with
no external consumer yet (safe to remove); `PlayerRoot.cs`'s explicit tick order has room to
insert stance-switch handling alongside the existing dodge-consume step without restructuring.

## Approach & Tradeoffs (Director sign-off)
- **`PlayerVitals.cs`** (new, `Assets/Scripts/Player/`): serialized `maxHealth=100f`,
  `maxStamina=100f`, `maxPosture=100f`, current values default to max. Implements a small
  `IStaminaSource` interface: `bool TrySpend(float amount)` (returns false if insufficient,
  otherwise deducts and fires `RaisePlayerStaminaChanged`, resets the regen-pause timer).
  Exposes `TickRegen(float deltaTime)` called explicitly by `PlayerRoot` (not its own
  `Update()`, consistent with the existing pattern) — regen only applies to stamina (health/
  posture have no drain source yet, so no regen logic needed for them this slice, avoids
  building unused code per the "don't build for hypothetical future requirements" standard).
  Fires all three `Changed` events once in `Start()` so the HUD's own hardcoded `Awake()`
  defaults get immediately superseded by the real authoritative source — establishes
  `PlayerVitals` as the single source of truth per the task brief's DoD.
- **`DodgeAbility.cs`** changes: remove the `[SerializeField] private float stamina` field;
  add `[SerializeField] private PlayerVitals vitals` (assigned via Inspector, read through the
  `IStaminaSource` interface internally in `Awake`, matching the `PlayerRoot`/`IMovementInput`
  DIP pattern already established); `TryDodge` calls `_staminaSource.TrySpend(StaminaCost)`
  and bails if it returns false, instead of the old inline `stamina < StaminaCost` check.
- **`StanceController.cs`** (new, `Assets/Scripts/Player/`): serialized `StanceData[] stances`
  (4 entries, Inspector-wired to the same Phase 2 assets — independent array from
  `StanceDiamond`'s own, matching the established UI/gameplay decoupling precedent from
  Phase 2's approach), private current index (default 0 = Stone). Public methods
  `CycleNext()`/`CyclePrevious()` wrap the index and fire `EventBus.RaiseStanceSwapped
  (stances[index])`. No `Update()` of its own — purely reactive to `PlayerRoot` calls.
- **`PlayerInputReader.cs`** changes: add two new `event Action StanceNextPressed;`/
  `event Action StancePrevPressed;`, wired to `_controls.Player.stance_next.performed`/
  `stance_prev.performed` the same way the existing four buffered actions are (gated on
  `!GameState.IsPlayerInputLocked()`), but firing the event directly instead of pushing to
  `InputBuffer` — per the task brief's explicit "stance switching is unbuffered" ruling.
  Symmetric `-=` cleanup in `OnDestroy`, matching the existing four.
- **`PlayerRoot.cs`** changes: add serialized `PlayerVitals vitals` and
  `StanceController stanceController` fields; subscribe to `inputReader.StanceNextPressed`/
  `StancePrevPressed` in `Awake` (calling `stanceController.CycleNext()`/`CyclePrevious()`
  directly — these are instant, not part of the per-frame tick order, since they're
  discrete events not continuous state); add `vitals.TickRegen(deltaTime)` as a new step in
  the existing explicit `Update()` order (after motor tick, since regen doesn't depend on
  movement — ordering here is a minor judgment call, logged rather than silently chosen).
  Must add `OnDestroy` to unsubscribe the two new stance events (currently `PlayerRoot` has
  no `OnDestroy` at all — a small pre-existing gap being closed as part of this change, not
  scope creep, since it's the same file being touched for the same reason: event
  subscription hygiene).
- **Verification:** live MCP tools per established convention; mandatory human Play Mode pass
  before sign-off, per the standing lesson from Phases 1-2.

## Implementation Summary (Implementation Agent)
- Created `IStaminaSource.cs`, `PlayerVitals.cs`, `StanceController.cs` exactly per the
  approved design. Edited `DodgeAbility.cs` (stamina stub removed, now queries
  `IStaminaSource.TrySpend` — dodge timing/i-frame constants left untouched, no regression),
  `PlayerInputReader.cs` (two new unbuffered stance-switch events, symmetric `+=`/`-=`),
  `PlayerRoot.cs` (new `OnDestroy()` — didn't exist before — unsubscribes the two new events;
  `vitals.TickRegen` inserted into the explicit tick order after motor, before lean).
- Live-wired on **both** the `Player.prefab` asset and the `MovementTest.unity` scene
  instance, per the Phase 1 lesson about the two being separate serialized objects — verified
  the scene instance actually inherited from the prefab save (connected instance, no override
  conflict) rather than assuming.
- Cross-checked `StanceController.stances`' asset order against `StanceDiamond`'s own
  `stanceOrder` array (Stone/Water/Flame/Wind, same order) before wiring, since a mismatch
  would silently highlight the wrong diamond icon on a real stance swap.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** Independently re-read all 6 changed/new files, independently re-verified live
  wiring on both the prefab (direct YAML read, fileID cross-reference) and the scene instance
  (live instanceID resolution via `gameobject-find`/`gameobject-component-get`), independently
  re-derived the `CycleNext`/`CyclePrevious` wraparound math rather than trusting the claim,
  independently cross-checked `StanceController` vs `StanceDiamond`'s asset ordering by GUID.
- **Result: PASS, no deviations found.** All locked numbers/patterns correctly implemented;
  dodge timing constants confirmed unchanged (no regression into already-QA'd Phase 1 logic);
  stance-switch events confirmed genuinely unbuffered (never touch `InputBuffer`); both prefab
  and scene-instance wiring independently confirmed non-null and pointing at real, matching
  components. One informational note (not a defect): `PlayerRoot.Update()`'s existing
  `IsPlayerInputLocked()` early-return also pauses stamina regen while paused — this is the
  same pre-existing gating pattern shared by every other per-frame system in this class, not
  something newly introduced, and is actually the *desired* behavior (no passive regen while
  the game is paused) rather than a bug.
- **Runtime behavior explicitly unverified**, same standing limitation as Phases 1-2 — no
  Play-Mode-control MCP tool exists in this environment. QA confirmed the Editor was in Edit
  Mode throughout (no risk of the Play-Mode scene-mutation bug recurring) but could not itself
  observe stamina draining/regenerating or the stance diamond highlight changing.

## Director Final Review
- Re-read the diff directly. S.O.L.I.D. holds: `PlayerVitals` owns exactly vitals+regen,
  `StanceController` owns exactly stance-cycling, neither touches Input directly;
  `PlayerRoot` remains the sole orchestrator. `DodgeAbility` now depends on the
  `IStaminaSource` abstraction, not a concrete `PlayerVitals` reference internally — genuine
  Dependency Inversion, consistent with the existing `IMovementInput`/`IInvulnerabilityProvider`
  pattern rather than a one-off. `PlayerVitals` fires its `Changed` events exactly once, at
  `Start()`, establishing itself as authoritative — closing the gap Phase 2 explicitly flagged
  (HUD showing "sensible defaults" that happened to match, not real synchronized state).
- The `PlayerRoot.OnDestroy()` gap (event subscriptions with no unsubscription path at all)
  that this task closed was a real pre-existing hygiene gap from Phase 1 that nobody had
  hit yet only because nothing had triggered a `PlayerRoot` destroy/reload cycle in testing —
  worth noting as a class of gap to keep watching for as more events get wired into this class.
- **Known gap, not hidden, same as every phase so far:** no agent has run Play Mode and
  watched the stamina bar drain on dodge or the stance diamond highlight change on Q/Tab.
  Human verification required before this is gameplay-verified, not just
  compile-and-wiring-verified.
- **Sign-off: Player Vitals + Stance Switching (Phase 3, slice 1) complete**, with the above
  gap explicitly noted. Full combat (hitboxes, damage, parry) remains the next slice of Phase 3.