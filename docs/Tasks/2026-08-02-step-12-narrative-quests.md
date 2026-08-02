# Step 12 (Unity port): Narrative Engine, Dialogue Trees & Quest State Machine — 2026-08-02

## Task Brief (Director)
- **Goal:** implement charter Step 12's full spec — the `DialogueTree`/`DialogueNode`/
  `DialogueCondition` data model, dialogue UI/playback, and the `QuestState` FSM
  (`Unstarted, Active, ObjectiveComplete, Completed, Failed`). This is fully greenfield: zero
  narrative/dialogue/quest scripts exist anywhere in the project today (confirmed by a full
  `Assets/Scripts/**/*.cs` glob). It lands on real infrastructure from Steps 10/11:
  `PlayerData.questStates`/`dialogueSeen`/`npcStates` are already locked fields (currently
  serializing empty, per Step 11's own logged note), `PlayerSaveDto` already has the
  Dictionary-flattening pattern proven for exactly this shape, and charter Step 12 explicitly
  locks shrine rest as "the single deterministic quest-tick point" — `Shrine.Interact` already
  exists and already calls `SaveSystem.Save()` (Step 11), so this is the second real consumer
  of that same seam, not a new one.
- **Affected systems:** `Assets/Scripts/Narrative/` (new folder — charter doesn't pre-declare
  this one the way `Interaction/`/`World/` were, Research to confirm best placement/naming),
  `Assets/Scripts/Systems/PlayerData.cs`/`PlayerSaveDto.cs` (quest/dialogue state was locked
  in shape at Step 11 but never exercised — Research to confirm the existing
  `questStates`/`npcStates` Dictionary shape is actually sufficient or needs adjustment now
  that a real consumer exists), `Assets/Scripts/UI/` (new dialogue display, extending or
  sitting alongside `NoticeDisplay`), `Assets/ScriptableObjects/Dialogue/` (new, matching the
  `Stances/`/`Regions/`/`Items/` precedent), `Assets/Scripts/Interaction/` (an `NpcInteractable`
  — deferred at Step 10 pending exactly this system, now unblocked), `Assets/Tests/EditMode/
  Editor/` (new tests, 80% gate). `docs/Worklog.md`.
- **Constraints:**
  - **Quest structure locked** (charter Step 12, "hybrid Archon spine, Grace threads"): 5 Acts
    + Prologue get full linear unmissable dialogue trees; the 6 named bosses get pre/post-fight
    monologues (Soren's gets real branching — out of scope this task, no Soren content exists);
    8-12 optional NPC threads are journal-hint-tracked. **This task's scope is the mechanism,
    not the content** — same "prove one of each" discipline as every prior step. Build: one
    real `DialogueTree` asset (a Prologue-appropriate NPC or narrator line), one real `Quest`
    with a `journalHint`, wired to one interactable NPC in `Prologue.unity`. Do not author all
    6 acts or 12 NPC threads.
  - **`DialogueTree` data structure locked**: `ScriptableObject`, `dialogueId`, `startNode`,
    `nodes: Dictionary<string, DialogueNode>` (ID-keyed, not a cyclic node-reference graph, for
    clean serialization + hub-and-spoke "ask about..." menus). **Research must confirm whether
    Unity's default Inspector serializer can actually author a `Dictionary<string,DialogueNode>`
    field directly** — this is a different question from Step 11's JsonUtility-at-runtime
    limitation; it's about `SerializedProperty`/Inspector authoring of `ScriptableObject`
    fields, which has its own well-known Dictionary gap requiring `[SerializeReference]`/a
    custom drawer/an `List<KeyValuePair>`-backed wrapper. Do not assume either way.
  - **`DialogueNode` locked fields**: speaker, text, portrait, voice clip, camera shot marker,
    animation cue, choices, flag/quest mutations. **Mutations apply on node *display*, not
    exit** (explicit charter rule — an early quit doesn't lose a visibly-triggered state
    change). Portrait/voice/animation-cue fields are locked in now even though no
    portrait art/VO/animation content exists yet (inert data, zero dependencies — same
    "data field isn't a method" precedent Step 10 used for `promptText`).
  - **`DialogueCondition` locked as a whitelisted data type** (source/key/op/value) — **never**
    a dynamically-evaluated expression string on authored data. This is a hard security/
    correctness rule carried verbatim from the original Godot charter's `Expression.parse()`
    ban, generalized to this engine. Do not implement anything resembling an eval.
  - **Quest FSM locked**: `Unstarted, Active, ObjectiveComplete, Completed, Failed` (5 states,
    the 5th — `Failed` — was already approved above the roadmap's original 4-state ask,
    needed for missable NPC threads and the eventual Soren branch).
  - **Shrine-rest quest tick locked**: quest/NPC state progression advances when the player
    rests at a shrine — the same moment as autosave. `Shrine.Interact` is the real, already-
    proven hook (Step 11 already extends it for `discoveredShrines`/`SaveSystem.Save()`) —
    Research to confirm the cleanest way to add a quest-tick call here without turning
    `Shrine` into a god-class (a `QuestManager`/`NarrativeState` singleton `Shrine` calls into,
    matching the `EventBus`/`GameState` static-singleton convention, is the likely answer but
    confirm against existing precedent before locking).
  - **`interact` vs `NpcInteractable`**: Step 10 deferred `NpcInteractable`/`DoorInteractable`
    explicitly pending "no NPCs or doors exist yet" — this task is what unblocks
    `NpcInteractable` specifically (still not `DoorInteractable`, no doors are in scope here
    either). It should subclass `Interactable` per Step 10's existing base-class API
    (`abstract Interact(Transform)`, `virtual CanInteract(Transform) => true`), triggering
    dialogue playback rather than an inventory grant.
  - **80% test coverage gate applies.** Dialogue tree traversal (node lookup, choice
    resolution, condition evaluation against the whitelisted type), the quest FSM's state
    transitions, and the DTO flatten/unflatten for `questStates`/`npcStates`/`dialogueSeen`
    are all pure-logic/EditMode-testable. UI display components should follow the
    `HealthBar`/`MapScreen` EditMode-testable pattern already proven twice in this project,
    not be waved off as Play-Mode-only.
  - Use live Unity-MCP tools per established convention. **Mandatory human Play Mode pass
    required before sign-off** — approach the NPC, confirm dialogue displays/advances
    correctly, confirm a choice mutates quest state, confirm resting at a shrine ticks it, and
    (per the still-outstanding item from Step 11) also re-confirm Step 11's own compass/map/
    save-slot content while in there, since that pass hasn't happened yet either.
- **Definition of done:**
  - `DialogueTree`/`DialogueNode`/`DialogueCondition`/`Quest`(state machine) compile clean
    with the locked structure, confirmed authorable in the actual Unity Inspector (not just
    compiling in code).
  - One real `DialogueTree` asset + one real `Quest` (with `journalHint`) exist; one
    `NpcInteractable` in `Prologue.unity` triggers the dialogue, a choice inside it mutates
    quest state, and shrine rest visibly ticks/advances it.
  - Mutations apply on node display, not exit — proven by a test, not just asserted in a
    comment.
  - Project compiles clean; ≥80% measured coverage on newly-added logic-bearing code.
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Research Agent)
1. **Dictionary fields do NOT render in the Unity 6000.5.5f1 Inspector — confirmed against version-pinned docs.** Unity added first-class Dictionary serialization with a two-column editor, but only from **6000.6** onward; the 6000.5 serialization-rules page explicitly lists dictionaries as unsupported, and the 6000.6 feature's doc URL 404s at 6000.5. Failure mode is silent: the field compiles, no error, just never appears in the Inspector. **Anyone re-checking this later via a web search will likely land on the 6000.6 page and get it wrong — recorded here specifically so it isn't re-litigated.** Ruling: `DialogueTree.nodes` is a `List<DialogueNode>` (each node carrying its own `id`), non-serialized `Dictionary<string,DialogueNode>` index built via `Rebuild()` — the third use of a pattern already proven twice (`Inventory.Rebuild()`, `ItemDatabase.Rebuild()`).
2. **`PlayerData`'s Step 11 quest/dialogue fields, audited as their first real consumer:** `questStates: Dictionary<string,int>` and `npcStates: Dictionary<string,string>` both hold up as-is (`int` matches `EventBus.QuestStateUpdated`'s existing `Action<string,int>` signature with zero conversion layer; free-form `string` NPC stages avoid forcing one shared vocabulary across 8-12 different threads). **`dialogueSeen` is a real, if minor, defect inherited from Step 11**: it's `List<string>`, but Step 11's own doc comment argues `discoveredShrines` should be a `HashSet` because discovery is "a pure membership question... never ordered/indexed" — `dialogueSeen` is exactly that same shape, and a `List` means unbounded duplicate growth + O(n) `Contains` checks over a 20-hour campaign. Zero-cost fix: change `PlayerData.dialogueSeen` to `HashSet<string>` (the DTO already has the exact List↔HashSet conversion idiom at `discoveredShrines`, on-disk format doesn't change).
3. **Shrine-rest quest tick: a pure `static class QuestSystem`** (matching `EventBus`'s shape, not `GameState`'s `MonoBehaviour`+`Instance` shape — quest ticking needs no `GameObject`/lifecycle). `Shrine.Interact` gets a one-line addition, and **ordering is load-bearing: `QuestSystem.TickOnRest(data)` must run BEFORE `SaveSystem.Save()`**, or every rest persists the pre-tick state and a reload loses one tick of progression. `QuestSystem` takes `PlayerData` as a parameter rather than reaching for `SaveSystem.CurrentPlayerData` itself, keeping it pure/testable.
4. **`DialogueCondition` concrete shape**, grounded in what's actually queryable today (not speculative): `Source` enum (`QuestState, WorldFlag, ItemOwned, NpcState, DialogueSeen, PlayerLevel`), `Op` enum (`Equals, NotEquals, GreaterOrEqual, LessOrEqual` — ordering ops meaningless on `NpcState`'s free-form strings, evaluator returns `false` not throws for that combination), `key`/`intValue`/`stringValue`. Evaluated by a pure static `DialogueConditionEvaluator.Evaluate(...)` — a closed `switch` over two enums, structurally incapable of an eval-equivalent path. **Named gap, not silently filled:** `Inventory.HasItem` is boolean-only (no quantity accessor), so `ItemOwned` conditions are boolean-only this task — adding a `CountOf` accessor is a Director call, not assumed.
5. **Dialogue UI: `DialogueDisplay.cs`** — `NoticeDisplay`'s uGUI idiom for speaker/text/portrait, `MapScreen.Rebuild()`'s instantiate-into-parent-loop pattern for choice buttons (both proven EditMode-testable via `TestReflectionUtil`, confirmed by reading the actual test files, not assumed). **Critical Play Mode risk found:** `PlayerRoot.Update()` early-returns whenever `GameState.IsPlayerInputLocked()` is true, which includes `Dialogue` state — so once dialogue starts, `PlayerRoot`'s own input handling (including Step 10's interact-consume) stops entirely. Dialogue advance/choice input **must** be handled by the dialogue component's own `Update()`/event subscription, not routed through `InputBuffer`/`PlayerRoot`, and whatever ends dialogue must call `GameState.SetState(Playing)` or the player is permanently softlocked. This is the single highest-risk item in the whole task and is invisible to unit tests — flagged explicitly for the mandatory Play Mode pass.
6. **`NpcInteractable`**: `sealed`, subclasses `Interactable` exactly like `Chest`/`Shrine` (confirmed base API by reading the file directly), does **not** override `CanInteract` (NPCs stay talkable, unlike a one-shot chest). Kicks off dialogue via a **direct call** to a new `DialogueRunner` (`static Instance`, `MonoBehaviour`-based like `GameState` since it needs a real per-frame lifecycle) — not through `EventBus`, per the charter's own Call-Down/Signal-Up distinction (starting dialogue is a command to one specific subsystem with a specific payload, not a fire-and-forget notification). `EventBus.InteractionTriggered` has zero production callers currently; left unraised this task rather than half-adopted in one subclass only.
7. **Quest FSM: static/stateless functions over `PlayerData.questStates`, NOT instance objects.** Decisive reason beyond mere consistency: `questStates` is already the save-file source of truth via the DTO layer — an instance-per-quest object (`PlayerVitals`-style) would create a second source of truth requiring hand-written save/load sync, exactly the bug class Step 11's DTO layer exists to prevent. `Quest` (definitions) = `ScriptableObject` matching `ItemData`/`StanceData` (questId, journalHint, objectives, advancement conditions); `QuestState` = enum pinned to `questStates`' stored ints; `QuestSystem` = static pure functions (`GetState`/`SetState`/`TickOnRest`), matching the already-proven `CompassProjection`/`ScoreCandidate` shape.

## Approach & Tradeoffs (Director sign-off)
- **Adopt all 7 Research findings as-is**, including the `dialogueSeen` List→HashSet fix (logged as a Step-11-originated defect closed here, not silently patched) and the tick-before-save ordering rule.
- **Named gaps: log, do not fill.** `Inventory.CountOf` (quantity-based conditions) is out of scope — `ItemOwned` stays boolean-only, same discipline as Step 5's `IsBlocking`/Step 7's missing sprint mechanic. `DoorInteractable` remains deferred (still no doors). Full 6-act/12-NPC-thread content authoring is explicitly out of scope — this task proves the mechanism with one `DialogueTree` + one `Quest` + one `NpcInteractable`.
- **Locked file/class layout:** `Assets/Scripts/Narrative/` — `DialogueTree.cs` (ScriptableObject, `List<DialogueNode>` + `Rebuild()`-built index, per Research), `DialogueNode.cs` (`[Serializable]`: id, speaker, text, portrait, voiceClip, cameraShotMarker, animationCue, choices, mutations-on-display), `DialogueCondition.cs` + `DialogueConditionEvaluator.cs` (pure static, per Research's exact shape), `Quest.cs` (ScriptableObject: questId, displayName, journalHint, objectives), `QuestState.cs` (enum), `QuestSystem.cs` (static, `GetState`/`SetState`/`TickOnRest` — raises `EventBus.RaiseQuestStateUpdated`), `DialogueRunner.cs` (MonoBehaviour, static `Instance`, owns `GameState.SetState(Dialogue)`/`SetState(Playing)` transition and its own advance-input handling per Research's Play-Mode-risk finding). `Assets/Scripts/UI/DialogueDisplay.cs`. `Assets/Scripts/Interaction/NpcInteractable.cs` (sealed, subclasses `Interactable`, no `CanInteract` override). `PlayerData.cs`'s `dialogueSeen` field type fixed to `HashSet<string>`; `PlayerSaveDto`'s existing List-conversion idiom reused unchanged.
- **`Shrine.Interact` gets exactly one new line** (`QuestSystem.TickOnRest(data)`) inserted before the existing `SaveSystem.Current?.Save(...)` call — ordering is a locked, tested requirement, not a style preference.
- **Content proof:** one real `DialogueTree` asset (a short Prologue-appropriate NPC exchange with at least one branching choice that mutates a quest state), one real `Quest` asset with a `journalHint`, one `NpcInteractable` placed in `Prologue.unity`, wired so: approaching and interacting starts dialogue, a choice inside advances the quest via `QuestSystem.SetState`, and resting at the (already-present) `ShrineMarker` visibly ticks/persists it.
- **Verification:** live MCP tools per convention; ≥80% coverage on `DialogueTree`/`DialogueConditionEvaluator`/`QuestSystem`/DTO changes (all pure-logic) and `DialogueDisplay`/`NpcInteractable` via the proven `HealthBar`/`MapScreen` EditMode pattern; mandatory human Play Mode pass explicitly covering Research's flagged softlock risk (dialogue must not freeze the player permanently) alongside the still-outstanding Step 11 content (compass/map/save-slot) from the prior task.

## Implementation Summary (Implementation Agent)
(pending)

## QA Iterations (QA/Test Agent)
(pending)

## Director Final Review
(pending)
