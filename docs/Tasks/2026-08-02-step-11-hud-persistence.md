# Step 11 (Unity port): Reactive HUD, UI Systems & Persistence Engine — 2026-08-02

## Task Brief (Director)
- **Goal:** implement the remaining, not-yet-built portion of charter Step 11's full spec.
  UI Systems Phase 2 (2026-07-31, during the user-directed Player→UI→Combat reprioritization)
  already delivered the reactive vitals HUD (`HealthBar`/`StaminaBar`/`PostureBar`,
  `VitalsFader`'s 5s-idle-fade), `StanceDiamond`, `NoticeDisplay`, `HUDRoot`, and a
  `MainMenu.unity` stub (Play/Settings/Quit) — see
  `docs/Tasks/2026-07-31-ui-systems-phase2.md`. **This task covers what that pass explicitly
  did not:** the compass strip, the full map screen (M key, per-region reveal), and — the
  larger piece — the entire save/load persistence engine (`PlayerData`, JSON serialization,
  checkpoint autosave, 3 playthrough slots). Zero persistence code exists anywhere in the
  project today (confirmed: `Assets/Scripts/Systems/` has only `EventBus.cs`/`GameState.cs`/
  `SandboxAutoPlay.cs`).
- **Affected systems:** `Assets/Scripts/Systems/` (new `PlayerData.cs`, `SaveSystem.cs`),
  `Assets/Scripts/UI/` (new compass strip + map screen components, extending the existing
  `HUDRoot`), `Assets/Scenes/` (`MainMenu.unity` gains real slot-select UI wired to
  `SaveSystem`), `Assets/Tests/EditMode/Editor/` (new tests, 80% gate). `docs/Worklog.md`.
- **Constraints:**
  - **Save policy locked** (charter Step 11): single live save per playthrough, autosaved at
    checkpoints (shrine rest, boss defeat, region transition, quest update, key-item pickup,
    stance unlock, quit-to-menu). **This task has no shrine-rest/quest/region-transition
    system wired to fire these triggers yet** (shrine rest is a placeholder `ShowNotice` per
    Step 10's own logged scope boundary) — Research to confirm which checkpoint triggers are
    actually wireable today vs which must be stubbed/logged as a future hook, same discipline
    as Step 10's `HarvestNode` respawn-on-rest stub.
  - **Serialization locked:** plain C# `PlayerData` class (not `ScriptableObject`), JSON via
    `JsonUtility`/`System.Text.Json` to `Application.persistentDataPath/saves/`, **never**
    `BinaryFormatter`. Atomic write (`.tmp` then rename over the live save), keep one rolling
    `.bak`. Research must confirm which of `JsonUtility`/`System.Text.Json` actually handles
    `PlayerData`'s locked field list correctly — `JsonUtility` has known, real limitations
    (no `Dictionary<TKey,TValue>` support, no top-level collection support) that directly
    collide with the locked `stats: Dictionary<string,int>` field and `Inventory`'s own
    `List<ItemStack>` + non-serialized index — do not assume compatibility, verify.
  - **`PlayerData` fields locked** (charter Step 11): `saveVersion`, `ngCycle: int = 0`,
    progression (`level`, `expTotal`, `expUnbanked`, `mon`, `statPointsUnspent`,
    `stats: Dictionary<string,int>` for `body/breath/blade/spirit`), world state
    (`currentRegionId`, `discoveredShrines`, `bossesDefeated`, `lootedContainers`,
    `worldFlags`), `inventory: Inventory`, `equippedCharms`, narrative state (`questStates`,
    `dialogueSeen`, `npcStates`). **No quest/dialogue/charm systems exist yet** (Step 12/10's
    Charm-equip territory) — those fields get their locked types/shapes now (so the save
    format doesn't need a breaking migration later) but will serialize as empty/default,
    logged explicitly, not silently omitted from the class.
  - **3 playthrough slots at the main menu** — `MainMenu.unity` (existing stub from Phase 2)
    needs real slot-select UI (new save / continue-from-slot / slot metadata display at
    minimum: region name, playtime or level, last-played date).
  - **HUD additions locked:** top-centre compass strip (sightline navigation, no minimap);
    full map screen on `M` key, per-region reveal on first shrine rest (ties into
    `discoveredShrines`/`currentRegionId` from the save data model above). **No lock-on
    system exists yet**, so the charter's "target posture centered under the lock-on reticle"
    placement is out of scope this task — self-posture-under-player-HP is already correctly
    placed by Phase 2's existing `PostureBar`, confirmed, not re-touched.
  - **80% test coverage gate applies** — `PlayerData` (de)serialization round-trip and
    `SaveSystem`'s atomic-write/`.bak`-rotation logic are pure-logic/EditMode-testable
    (Research to confirm whether file-IO-touching tests need a temp directory sandbox
    pattern, not the real `persistentDataPath`, to stay hermetic and CI-safe).
  - Use live Unity-MCP tools per established convention. **Mandatory human Play Mode pass
    required before sign-off** — same standing gap carried from Steps 6-10; log it as
    outstanding rather than skip documenting the requirement.
- **Definition of done:**
  - `PlayerData` compiles clean with the full locked field list (types locked even where a
    dependent system doesn't exist yet).
  - `SaveSystem` can serialize/deserialize a `PlayerData` round-trip correctly, writes
    atomically (`.tmp` + rename) with one rolling `.bak`, and exposes a save-slot API (3
    slots).
  - At least one real checkpoint trigger actually calls `SaveSystem.Save()` end-to-end (even
    if others are stubbed/logged per the constraint above) — proves the mechanism works, same
    "prove one of each" discipline as every prior step.
  - `MainMenu.unity` shows real per-slot metadata and can start a new game / continue an
    existing save.
  - Compass strip and map screen exist and render against real player/world state (position,
    discovered shrines).
  - Project compiles clean; ≥80% measured coverage on newly-added logic-bearing code.
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Research Agent)
(pending)

## Approach & Tradeoffs (Director sign-off)
(pending)

## Implementation Summary (Implementation Agent)
(pending)

## QA Iterations (QA/Test Agent)
(pending)

## Director Final Review
(pending)
