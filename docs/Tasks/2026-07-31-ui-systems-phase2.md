# UI Systems (Phase 2: character → UI → combat) — 2026-07-31

## Task Brief (Director)
- **Goal:** Phase 2 of the user-directed reprioritization. Build the reactive HUD per charter
  Step 11's locked design (vitals skeleton, posture placement, stance diamond, compass strip,
  full map screen) and a MainMenu scene, on top of the now-working Player base character. This
  satisfies (and supersedes) the still-unstarted deliverables of the paused
  `docs/Tasks/2026-07-31-ui-systems-skeleton.md` side-task (Input Actions asset and inventory
  data types from that task are no longer needed here — Input Actions already shipped in Phase
  1; full `Inventory`/`ItemData` data types are Step 10 territory, out of scope for this UI
  pass, which only needs a dumb slot-grid visual stub per that task's own original scoping).
- **Affected systems:** `Assets/Scripts/UI/` (new), `Assets/Prefabs/` (HUD/menu prefabs),
  `Assets/Scenes/` (new `MainMenu.unity`), `docs/Worklog.md`. Consumes `EventBus`
  (`PlayerHealthChanged`/`PlayerStaminaChanged`/`PlayerPostureChanged`/`StanceSwapped`/
  `ShowNotice`) — these events exist (Step 2) but nothing fires them yet (Player has no
  health/stamina/posture system — that's Combat-phase/Step 5 territory). HUD must therefore
  work correctly against **zero real emitters** right now: bind to the events, display
  sensible defaults, and update live once Phase 3 starts firing them — no HUD rework should be
  needed when that happens.
- **Constraints:**
  - **S.O.L.I.D. mandatory.** Each HUD element (health bar, stamina bar, posture bar, stance
    diamond, notice/toast display) is its own component, single responsibility, subscribing
    only to the specific `EventBus` event(s) it displays — no monolithic `HUDController` god
    class wiring every event into one file. A thin `HUDRoot` may exist purely to
    instantiate/parent the sub-elements, not to own their logic.
  - HUD philosophy is already locked (charter Step 11, do not re-derive): Elden-Ring-style
    minimalist always-on vitals, no minimap/party-switcher; posture placement is Sekiro-style
    (self-posture under player HP, target posture under lock-on reticle — no lock-on system
    exists yet, so target-posture UI is a stub/hidden element this phase); stance shown as a
    4-icon diamond bottom-right; no damage-number spam; vitals fade to low alpha after 5s out
    of combat (needs a simple "time since last vitals change" tracker).
  - Build with **uGUI (Canvas)**, not UI Toolkit — matches the already-Director-approved
    judgment call from the paused skeleton task (`com.unity.ugui` already resolves clean in
    the manifest), avoid re-litigating.
  - MainMenu: Play/Settings/Quit buttons, Settings panel stub (graphics/audio/controls
    placeholder tabs) — skeleton only, no real save/load or rebind persistence wiring (that's
    Step 11's save-policy territory and Step 3's rebind-persistence territory respectively,
    both out of scope here). Must not be wired as the actual game entry point yet
    (`Bootstrap.unity` stays build index 0) — this is a reachable-but-not-yet-linked scene for
    now, avoids prematurely changing the boot flow before there's an actual Playing-state
    handoff to wire it to.
  - Respect `GameState.IsPlayerInputLocked()`/`GameState.CurrentState` where relevant (e.g. the
    Settings panel should be reachable from `Paused`, not just `MainMenu`).
  - Use live Unity-MCP tools for scene/prefab/Canvas/UI-element construction and compile
    verification, same convention as Phases prior. **Given the SandboxAutoPlay lesson:** any
    new Sandbox test scene for this phase needs the same auto-play bootstrap, and — critically
    — a human must manually enter Play Mode and confirm the HUD actually renders/updates
    before this is treated as gameplay-verified, not just compile-and-wiring-verified.
- **Definition of done:**
  - `Assets/Scripts/UI/` contains separate, single-responsibility components for: health bar,
    stamina bar, posture bar (self), stance diamond, notice/toast display, and a thin
    `HUDRoot` orchestrator.
  - A HUD prefab/Canvas exists in `MovementTest.unity` (or a new dedicated UI test scene) showing
    all elements with sensible default/placeholder values, confirmed live via a manual Play
    Mode pass (bars visible, stance diamond visible, no console errors).
  - A `MainMenu.unity` scene exists with Play/Settings/Quit and a Settings panel stub.
  - Project compiles clean (MCP `console-get-logs` zero `error CS`).
  - Worklog + this task file updated through Director sign-off, `docs/Tasks/2026-07-31-ui-systems-skeleton.md`
    marked superseded/closed by this task (not deleted — cross-referenced).

## Research Findings (Research Agent)
Verified live: `com.unity.ugui 2.0.0` resolves clean, zero `error CS` in current project.
`Assets/Scripts/UI/` and `Assets/ScriptableObjects/` are both currently empty.
`assets-find t:StanceData` returns zero results — no stance assets exist yet, and
`StanceData.cs` has no `[CreateAssetMenu]`, so none could even be authored via the Editor menu
today.
1. **Subscribe/unsubscribe:** `OnEnable`/`OnDisable` symmetric `+=`/`-=` (not `Awake`/
   `OnDestroy`) — `OnDisable` strictly dominates `OnDestroy` (covers scene-unload and
   simply-disabled cases `OnDestroy` would miss), matching the exact leak class CLAUDE.md
   Section 6's own final-review checklist already calls out for static C# events.
2. **Vitals bars:** `Image` with `type=Filled` (`fillMethod=Horizontal`), not `Slider` —
   cheaper (no Selectable/EventSystem/pointer overhead for a non-interactive bar), one
   component per bar. `raycastTarget=false` on every HUD graphic.
3. **5s idle fade:** one reusable `VitalsFader` component (`CanvasGroup` + idle-timer +
   unscaled-time lerp — must use `Time.unscaledDeltaTime` per the charter's own Paused/
   `timeScale=0` divergence note, or the fade itself would freeze under Pause) shared by the
   whole vitals group via a `Notify()` call/event, not duplicated per-bar.
4. **Stance diamond:** since no stance assets exist, this task should (a) add
   `[CreateAssetMenu]` + minimal `stanceName`/`icon` fields to `StanceData.cs` (UI-owned
   fields already in charter 5.1's list; Step 5's tuning fields stay `TODO`), (b) create 4
   placeholder `.asset` instances (Stone/Water/Flame/Wind, name-only), (c) diamond references
   them via a serialized `StanceData[]` array + index-of highlighting on `StanceSwapped`, must
   handle `null`/unknown gracefully since nothing fires the event yet.
5. **Canvas render mode: Screen Space - Overlay** — sidesteps Cinemachine-vs-Camera-canvas
   jitter/rescale/null-`worldCamera` issues entirely (Screen Space - Camera would have all
   three). Needs an in-scene `EventSystem` using `InputSystemUIInputModule` (not the legacy
   `StandaloneInputModule`) for MainMenu button interaction, flagged for implementation.

## Approach & Tradeoffs (Director sign-off)
- **Adopt all 5 Research recommendations as-is** — no open design questions left unresolved.
- **Component split (S.O.L.I.D.):** `HealthBar`/`StaminaBar`/`PostureBar` (each: subscribe to
  its one `EventBus` event in `OnEnable`, unsubscribe in `OnDisable`, drive its own `Image
  .fillAmount`, call `VitalsFader.Notify()` on change), `VitalsFader` (shared idle-fade timer,
  single responsibility), `StanceDiamond` (subscribes to `StanceSwapped` only, highlights by
  array index), `NoticeDisplay` (subscribes to `ShowNotice` only), `HUDRoot` (thin, no event
  subscriptions of its own — parents the sub-elements only). No god-`HUDController`.
  Target-posture UI stub exists but stays `SetActive(false)` this phase — no lock-on system
  exists to drive it yet, per the task brief's own scoping.
  - **Note on `EventBus`'s current shape:** `PlayerHealthChanged`/`StaminaChanged`/
    `PostureChanged` are bare events with no default-value guarantee before first fire.
    `HealthBar` etc. must initialize their `fillAmount` to a sane default (e.g. `1.0`) in
    `Awake`/`OnEnable` rather than showing an uninitialized `0` until Phase 3 starts firing —
    this satisfies the brief's "sensible defaults with zero real emitters" requirement.
- **Stance assets:** create the 4 placeholder `.asset` files now (name-only) since the UI
  needs *something* concrete to reference and index — explicitly logging this as touching
  `StanceData.cs` (adding fields, not behavior) and `Assets/ScriptableObjects/Stances/`,
  outside this task's nominal `Assets/Scripts/UI/` scope, because the alternative (a
  string-keyed stance lookup) is worse per Research and would need rework at Step 5 anyway.
- **MainMenu.unity:** Play/Settings/Quit buttons (Settings panel a stub with placeholder
  tabs), own `EventSystem` (`InputSystemUIInputModule`), not wired into `Bootstrap`'s boot
  flow yet per the task brief's explicit constraint.
- **Verification:** live MCP tools per established convention, AND — given the SandboxAutoPlay
  lesson from Phase 1 — a mandatory human manual Play Mode pass before this is treated as
  gameplay-verified, not just compile-and-wiring-verified. QA will flag this as an open item
  regardless of static-verification outcome, matching Phase 1's practice.

## Implementation Summary (Implementation Agent)
- `StanceData.cs`: added `[CreateAssetMenu(fileName="NewStanceData", menuName="Return/Stance Data")]`
  plus `stanceName`/`icon` fields only — no combat-tuning fields added (Step 5's territory).
- `Assets/ScriptableObjects/Stances/{Stone,Water,Flame,Wind}.asset`: 4 name-only instances.
- `Assets/Scripts/UI/`: `VitalsFader`, `HealthBar`, `StaminaBar`, `PostureBar`, `StanceDiamond`,
  `NoticeDisplay`, `HUDRoot`, `MainMenuController` — S.O.L.I.D. split per approach, each vitals
  bar subscribes/unsubscribes in `OnEnable`/`OnDisable`, `VitalsFader` uses unscaled time.
- Live-built the HUD in `MovementTest.unity` (Screen Space-Overlay Canvas, shared
  `VitalsPanel`/`CanvasGroup`/`VitalsFader`, 3 vitals bars, inactive `TargetPostureGroup` stub,
  `StanceDiamondPanel` wired to the 4 stance assets in order, `NoticeDisplay`, an `EventSystem`
  using `InputSystemUIInputModule`) and `Assets/Scenes/MainMenu.unity` (Play/Settings/Quit,
  inactive `SettingsPanel` stub, own `EventSystem`, Settings button's `onClick` wired via a
  real persistent listener to `MainMenuController.ToggleSettings`, confirmed not just claimed).
  `MainMenu.unity` confirmed NOT added to `EditorBuildSettings.scenes`, per constraint.
- Used `script-execute` (full-code mode) for hierarchy construction rather than many individual
  MCP round-trips — same live-Editor mechanism, verified via read-back per the task's own
  wiring-verification requirement.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** Independently re-read all 8 script files + `StanceData.cs`, cross-referenced
  every serialized-reference instanceID live (fillImage/fader on each bar, stanceOrder/icons
  array order, EventSystem's actual module type, Canvas render mode field), independently
  re-verified `MainMenu.unity`'s button persistent-listener wiring via reflection
  (`GetPersistentEventCount`/`GetPersistentMethodName`, not just a claimed screenshot),
  independently re-confirmed `EditorBuildSettings.scenes` excludes `MainMenu.unity`.
- **Result: PASS.** All locked design decisions from Approach correctly implemented:
  `Time.unscaledTime`/`unscaledDeltaTime` used throughout `VitalsFader` (not scaled time, per
  the charter's Paused/`timeScale=0` divergence note), symmetric `OnEnable`/`OnDisable`
  subscription on every HUD element (no `Awake`/`OnDestroy` leak-risk pattern), sane
  `fillAmount=1f` defaults, divide-by-zero guards, `StanceDiamond` null/-1-safe,
  `HUDRoot` genuinely thin (zero event subscriptions). **One minor hardening deviation found:**
  `NoticeDisplay.OnDisable()` didn't stop its running hide-coroutine, leaving a dangling
  reference if the element was disabled mid-display (not a functional bug today, since nothing
  else reads that field, but a real latent issue). **Play Mode not exercised by QA** (no
  Play-Mode-control MCP tool exists, confirmed again this cycle) — flagged as required before
  gameplay sign-off, consistent with Phase 1's practice.
- **Director ruling:** the `NoticeDisplay` gap was small enough to fix directly rather than
  round-trip through another Implementation Agent pass — added `StopCoroutine`+null-out to
  `OnDisable()`, re-verified zero `error CS` via `console-get-logs` after the edit.

## Director Final Review
- S.O.L.I.D. re-checked: each vitals bar/element owns exactly one `EventBus` subscription and
  one visual concern; `VitalsFader` is the single shared timer (not duplicated 3x);
  `StanceDiamond` and `NoticeDisplay` are equally narrow; `HUDRoot`/`MainMenuController` stay
  thin. No god-class introduced. The current empty-project state (nothing fires
  health/stamina/posture yet, since Combat/Phase 3 hasn't been built) is exactly what the
  task brief's "sensible defaults with zero real emitters" requirement was designed for —
  defaults are sane, not just "happens to show 0."
- `docs/Tasks/2026-07-31-ui-systems-skeleton.md` (the paused prior side-task) is superseded by
  this task for the HUD/MainMenu/Settings-stub deliverables; its Input Actions deliverable was
  already satisfied by Phase 1, and its Inventory-data-types deliverable remains genuinely
  out of scope for both (Step 10 territory) — cross-referenced, not deleted.
- **Known gap, explicitly not hidden:** no agent has run Play Mode and visually confirmed the
  HUD actually renders/updates correctly (bars visible, stance diamond in the right corner,
  no runtime errors) — same standing limitation as Phase 1. A human manual Play Mode pass on
  `MovementTest.unity` is required before this is gameplay-verified, not just
  compile-and-wiring-verified.
- **Sign-off: UI systems (Phase 2) complete** with the above gap explicitly noted. Ready for
  Phase 3 (Combat system) next, per the user-directed reprioritization.
