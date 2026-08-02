# Step 10 (Unity port): Interactive Objects, Inventory Data & Gathering Economy — 2026-08-01

## Task Brief (Director)
- **Goal:** implement charter Step 10's full spec — the `ItemData`/`ItemStack`/`Inventory` data
  model, the `Interactable` base + subclasses (`Shrine`/`Chest`/`HarvestNode` at minimum —
  `NpcInteractable`/`DoorInteractable` deferred, no NPCs or doors exist yet, logged as scope
  boundary not oversight), and the player's `InteractionResolver` (camera-dot + proximity
  candidate ranking). This finally gives Step 9's inert `ShrineMarker`/`GraveMarker`
  placeholders real behavior — the `ShrineMarker` becomes an actual `Shrine` Interactable.
- **Affected systems:** `Assets/Scripts/Interaction/` (new — matches the charter's own
  pre-declared, currently-empty folder), `Assets/Scripts/Systems/` (`ItemData`/`ItemStack`/
  `Inventory` — arguably `Systems/` since Inventory is core game state, not combat/AI;
  Research to confirm folder placement), `Assets/Scripts/Player/` (`InteractionResolver`,
  wired into `PlayerRoot`), `Assets/ScriptableObjects/Items/` (new, matching the `Stances/`/
  `Regions/` precedent), `Assets/Scenes/Levels/Prologue.unity` (the `ShrineMarker` gets a real
  `Shrine` component; a `Chest` and `HarvestNode` added as pattern proof, same "prove one of
  each, not a full itemization pass" discipline every prior step has used).
  `Assets/Tests/EditMode/Editor/` (new tests, 80% gate). `docs/Worklog.md`.
- **Constraints:**
  - **S.O.L.I.D. mandatory** — `Interactable` is a base class with subclass-specific behavior
    (real inheritance is appropriate here, charter explicitly says "subclasses/components"),
    `InteractionResolver` only ranks/selects candidates and gates on input lock, it doesn't
    own interaction *behavior* (that's each `Interactable` subclass's job). `Inventory`
    manages `ItemStack`s, doesn't know about UI (Step 11's job) or specific item behaviors.
  - **Itemization philosophy locked** (charter 10, "~80% Elden Ring restraint, 20% Genshin"):
    no RNG-substat gear, no resin/energy gating, no real-world respawn timers (nodes respawn
    on shrine rest only — **no rest/save system exists yet**, Step 11's territory, so
    `HarvestNode` respawn-on-rest is a stubbed hook this task, not functional yet, logged).
    `Mon` is a scalar `int`, **never** an `ItemStack` — do not model currency as an item.
  - **`ItemData` fields locked** (charter 10): `itemId`, category enum (`Material,
    LocalSpecialty, Consumable, Charm, KeyItem, Recipe, UpgradeMat` — locked list), `maxStack`,
    `valueMon`, `regionTag`, `description` (charter explicitly calls this "a first-class
    narrative surface" — not an afterthought field).
  - **`Inventory` locked structure**: `List<ItemStack>` + a non-serialized runtime index
    `Dictionary` rebuilt on load (not persisted — this task has no save system yet, so "on
    load" means "on `Awake`/construction" for now, the real load-from-disk hook is Step 11's).
  - **`InteractionResolver` ranking formula locked**: `0.7 × camera-forward-dot + 0.3 ×
    proximity`, gated on `!GameState.IsPlayerInputLocked()`, consumes the input-buffer entry
    on successful interact to prevent double-fire. **`interact` is not currently consumed by
    anything** (bound in the Input Actions asset since Phase 1, never wired past that) —
    Research to confirm whether it should be buffered (charter 3.2 only lists light_attack/
    heavy_attack/parry/dodge as buffered) or direct like the stance-switch pattern
    (`PlayerInputReader.StanceNextPressed`-style), since the charter text says "consumes the
    Step 3 input-buffer entry" but 3.2's own buffered-action list doesn't include `interact`.
  - **Interactable layer/detection**: charter specifies "trigger `Collider`, layer=Interactable,
    Physics Layer mask=Player" — this is a **new** Physics Layer (Step 5 already claimed
    `PlayerHitbox`/`PlayerHurtbox`/`EnemyHitbox`/`EnemyHurtbox` at 8-11), Research to confirm
    a free layer slot and the correct collision-matrix setup (an `Interactable`-layer trigger
    should detect the player, not fight the existing combat layers).
  - **Shrine's actual "rest" behavior is out of scope** — no save/map-reveal system exists yet
    (Step 11). This task's `Shrine` component fires a placeholder `EventBus.ShowNotice` (reusing
    Step 2's already-wired UI notice display) on interact, proving the interaction pipeline
    works end-to-end without inventing save/rest behavior prematurely.
  - **80% test coverage gate applies** — `ItemData`/`ItemStack`/`Inventory`/
    `InteractionResolver`'s ranking math are all pure-logic/EditMode-testable. `Interactable`
    subclasses' trigger-based candidate detection may need the same `Physics.SyncTransforms()`
    treatment Step 7 established for physics queries in EditMode — Research to confirm.
  - Use live Unity-MCP tools, established safety checks. **Mandatory human Play Mode pass
    required before sign-off** — approach the shrine/chest/harvest node, confirm the
    interaction prompt/candidate-ranking actually picks the right target and interacting does
    something visible (notice text, log, inventory change).
- **Definition of done:**
  - `ItemData`/`ItemStack`/`Inventory` compile clean with the locked field/structure spec.
  - `Interactable` base + `Shrine`/`Chest`/`HarvestNode` subclasses exist; the Prologue scene's
    `ShrineMarker` is now a real `Shrine`, plus one placed `Chest` and one `HarvestNode`.
  - `InteractionResolver` correctly ranks multiple simultaneous candidates by the locked
    formula and interacting with the closest/most-forward one triggers that Interactable's
    behavior, confirmed in Play Mode.
  - Project compiles clean; ≥80% measured coverage on newly-added logic-bearing code.
  - Worklog + this task file updated through Director sign-off.

## Research Findings (Research Agent)
1. **`interact` confirmed bound (E / D-Pad Up) but never consumed** — verified by direct file
   read, matching the task brief's claim exactly. **Recommendation: buffer it** (add
   `BufferedAction.Interact` to `InputBuffer`, matching the existing four buffered actions,
   not the direct-event stance-switch pattern) — satisfies the charter's literal "consumes the
   input-buffer entry" wording, gets double-fire prevention structurally via `TryConsume`
   rather than a hand-rolled guard, and stays 100% EditMode-testable. **Charter inconsistency
   flagged:** Step 3.2's locked buffered-action list didn't originally include `interact` —
   Director to log this as a Step 3.2 amendment, same form as the project's existing verbatim
   `stance_prev=Tab` amendment precedent.
2. **New layer: 12 (`Interactable`)** — 0-7 are Unity built-in/read-only (confirmed, not just
   assumed), 8-11 already claimed by Step 5. **Detection mechanism: manual
   `Physics.OverlapSphere` scan** (mirroring `EnemyPerception`'s exact pattern), not
   OnTriggerEnter/Exit tracking — decisive reasoning: the ranking formula's camera-dot term
   changes every frame with no collider event, so an enter/exit-tracked candidate set would
   still need a full per-frame re-rank anyway, paying event-tracking complexity for nothing.
   Requires `Physics.SyncTransforms()` before the query (confirmed project-wide, not just
   EditMode-specific) and `QueryTriggerInteraction.Collide` explicitly (inverse of Step 7's
   `.Ignore` — Interactable colliders **are** triggers, on purpose, this time).
   **⚠️ Research also flagged a suspected Step 5 collision-matrix bug (`EnemyHitbox`/
   `PlayerHurtbox` mutually disabled) — Director independently verified this via the live
   `Physics.GetIgnoreLayerCollision` API (the authoritative source, not a manual bitmask
   decode of the packed asset format) and confirmed it's a FALSE ALARM: both directions
   correctly collide (`ignored=False`), self-pairs correctly don't (`ignored=True`). Step 5's
   collision matrix is fine — Research's manual decode of `TagManager.asset`'s packed format
   was simply wrong. No action needed, logged here so it isn't independently re-investigated
   again by a future task hitting the same file and drawing the same false conclusion.**
3. **Folder: all of Step 10 under `Assets/Scripts/Interaction/`** (existing, empty, charter's
   own pre-declared folder) — `ItemData`/`ItemStack`/`Inventory` included, not split into
   `Systems/` (reserved for cross-cutting singletons, `Inventory` is a domain model, not one).
   New assets at `Assets/ScriptableObjects/Items/`.
4. **EditMode testability confirmed, no new limitation** — the exact `EnemyPerceptionTests.cs`
   pattern (real GameObjects/Colliders in `[SetUp]`, `TestReflectionUtil`, `SyncTransforms()`
   inside production code not test scaffolding) transfers directly. Recommend exposing the
   ranking formula as a `static` pure function for the cheapest possible coverage path.
5. **`Interactable` API recommended and adopted:** `abstract Interact(Transform interactor)`,
   `virtual CanInteract(Transform interactor) => true` (the Open/Closed/Liskov hook letting
   `HarvestNode`/`Chest` self-exclude once depleted without `InteractionResolver` branching on
   concrete types), and a serialized `promptText` field **included now** despite no UI hookup
   this task — Research's reasoning adopted: it's inert data with zero dependencies, deferring
   it would mean re-touching every subclass and re-authoring every already-placed scene
   instance later, and charter Section 2 itself uses exactly this "don't force premature
   methods, but a data field isn't a method" distinction as its own worked example.

## Approach & Tradeoffs (Director sign-off)
- **Adopt all 5 Research recommendations as-is** (with the collision-matrix false alarm
  resolved as above) — no open design questions left unresolved.
- **`InputBuffer.BufferedAction` gains `Interact`**, `PlayerInputReader` wires
  `interact.performed` exactly like the existing four buffered actions (gated on
  `!GameState.IsPlayerInputLocked()`). Logged as the Step 3.2 amendment Research recommended.
- **`Assets/Scripts/Interaction/`**: `ItemCategory.cs` (enum: `Material, LocalSpecialty,
  Consumable, Charm, KeyItem, Recipe, UpgradeMat` — locked), `ItemData.cs` (`ScriptableObject`,
  `[CreateAssetMenu(menuName = "Return/Item Data")]`: `itemId`, `category`, `maxStack`,
  `valueMon`, `regionTag`, `description`), `ItemStack.cs` (`[System.Serializable]`: `ItemData
  item`, `int quantity`), `Inventory.cs` (`List<ItemStack> stacks` + a non-serialized
  `Dictionary<string, ItemStack> _index` rebuilt in a `Rebuild()`/constructor method — not
  persisted, per the task brief's explicit "no save system yet" scope), `Interactable.cs`
  (abstract base per Research's exact API), `Shrine.cs`/`Chest.cs`/`HarvestNode.cs`
  (subclasses — `Shrine.Interact` fires `EventBus.RaiseShowNotice("Rested at the shrine.",
  3f)` as its placeholder behavior; `Chest.Interact` grants one `ItemStack` to a
  serialized-reference `Inventory` and disables itself, `CanInteract` returns false once
  looted; `HarvestNode.Interact` similarly, with a stubbed `// TODO(Step 11): respawn on
  shrine rest` comment, `CanInteract` false once harvested this session).
  `InteractionResolver.cs` (`Assets/Scripts/Player/` per Research's cohesion note — a player
  component wired into `PlayerRoot`): exposes a `static float ScoreCandidate(...)` pure
  function implementing the locked `0.7×dot + 0.3×proximity` formula (testable without
  physics), plus the `Tick(deltaTime)`-driven `OverlapSphere` scan + candidate ranking +
  `TryConsume(Interact)` + calling the winning candidate's `Interact()`.
- **New layer 12 (`Interactable`)**, `QueryTriggerInteraction.Collide` on the resolver's
  overlap query, `Physics.SyncTransforms()` called inside `InteractionResolver` production
  code (not test-only).
- **Prologue wiring:** `ShrineMarker` gains a `Shrine` component (finally real behavior, per
  this task's own stated purpose); one `Chest` and one `HarvestNode` placed nearby as pattern
  proof (matching every prior step's "prove one of each, not full content" discipline) with
  1-2 simple `ItemData` assets to actually grant.
- **Verification:** live MCP tools per established convention; mandatory human Play Mode pass
  (approach each Interactable, confirm ranking picks the right one among simultaneous
  candidates, confirm interacting does something visible); ≥80% measured coverage via the
  batchmode CLI.

## Implementation Summary (Implementation Agent)
(pending)

## QA Iterations (QA/Test Agent)
(pending)

## Director Final Review
(pending)
