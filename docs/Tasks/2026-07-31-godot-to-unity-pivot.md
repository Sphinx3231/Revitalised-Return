# Engine Pivot: Godot 4/GDScript → Unity 6000.5.5f1/C# — 2026-07-31

## Task Brief (Director)
- **Goal:** Pivot Project Return's engine from Godot 4.7/GDScript to Unity 6000.5.5f1/C#,
  per explicit user instruction, confirmed after flagging the scale of the decision (Steps
  1-6 of the 14-step pipeline were already implemented/QA'd in Godot — this is not a
  greenfield choice). Preserve the existing Godot work rather than deleting it. Rewrite the
  charter for the new engine. Stand up a fresh Unity code skeleton that later steps build on.
- **Affected systems:** entire repo root (charter, folder layout, all engineering); design
  docs (`docs/DesignDoc.md`, `docs/ContentPlan.md`) are explicitly out of scope — content
  design does not change with the engine.
- **Constraints:** do not silently discard the Godot implementation (real, QA'd engineering
  work); do not re-derive Steps 9-14's locked design decisions; do not push to the remote
  without separate confirmation (destructive/visible action, outside this task's scope);
  new Unity project must use the same Editor version already installed and validated for
  NinjaGame (`6000.5.5f1`), reusing that project's proven headless-verification convention
  (`Ping.cs` + batchmode `-executeMethod`).
- **Definition of done:** old Godot implementation intact and clearly marked archived; new
  `CLAUDE.md` charter fully converted to Unity/C# terms with no design content lost or
  changed; a real Unity project exists at the repo root with the charter's folder structure,
  compiles clean, and passes a headless `PING_OK` smoke test; pivot decision and all of the
  above logged here and in `docs/Worklog.md`.

## Research Findings (Director, self-researched — see note below)
- **Deviation from the normal pipeline:** the Director did the Godot→Unity API-mapping
  research directly while writing the charter, rather than spawning a separate opus Research
  Agent first. Justification: the mappings needed (`CharacterBody3D`→`CharacterController`,
  `Area3D`→trigger `Collider`, `AnimationPlayer` method tracks→Animation Events,
  `Resource`/`.tres`→`ScriptableObject`, Godot signals→C# events, `Engine.time_scale`→
  `Time.timeScale` + unscaled-time coroutines, etc.) are well-established, stable,
  long-documented Unity API facts, not project-specific unknowns — and several genuinely
  open questions (best MultiMeshInstance3D/Path3D/AnimationTree-timescale/AudioStreamInteractive
  equivalents, exact Input System package version compatibility with 6000.5.5f1) were
  deliberately **not** resolved here and are flagged in the charter itself as "Research to
  confirm at Step N intake" rather than guessed at. Full pipeline research (opus agent) should
  still run at each future step's intake as normal — this was charter-conversion work, not a
  gameplay-system implementation step.
- Confirmed via the NinjaGame precedent (`Docs/Tasks/2026-07-25-bootstrap-unity-project.md`)
  that `6000.5.5f1` at `C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe` is
  installed, licensed, and has a working headless batchmode workflow (`-createProject`,
  `-executeMethod`) — reused directly rather than re-verified from scratch.

## Approach & Tradeoffs (Director sign-off)
- **Archive, don't delete, the Godot work.** Moved `project.godot`, `icon.svg(.import)`,
  `autoload/`, `assets/`, `resources/`, `scenes/`, `scripts/` into `legacy-godot/` via
  `git mv` (history-preserving), added `legacy-godot/README.md` explaining why it's there.
  This includes Step 6's implementation (`hit_stop.gd`, `spark_pool.gd`, `weapon_trail.gd`,
  `camera_trauma.gd`, `hit_flash.gdshader`, `juice_test.*`), which per
  `[[project_return_status]]` memory was implementation-complete and QA-passed but never
  reached Director sign-off/commit before the pivot — those files were untracked at pivot
  time and are now captured under `legacy-godot/` too, not lost.
  Tradeoff: keeps repo size/history a bit heavier than a clean deletion would, but the
  validated game-feel numbers (dodge i-frame ticks, parry windows, hit-stop duration, camera
  trauma decay constants) are genuine engineering value worth keeping as a porting reference
  — re-deriving them from the charter text alone would be strictly worse than having the
  original tested implementation to port from.
- **Charter rewrite scope:** converted all engine-specific API/architecture references
  (Sections 2, 3, 4's step details, Section 6, folder structure) to Unity/C#. Left Section 1
  (World & Acts) and the Steps 9-14 design spec's actual decisions completely unchanged —
  those are content/design, not engine plumbing, and re-litigating them was explicitly out
  of scope per the task brief. Where a Godot mechanism has no clean 1:1 Unity equivalent
  (CSG, MultiMeshInstance3D, Path3D, AnimationNodeTimeScale, AudioStreamInteractive), the
  charter says so explicitly and defers the concrete choice to that step's future Research
  Agent, rather than the Director guessing now.
- **Unity project location:** repo root, mirroring how `project.godot` lived at repo root
  before — "the repo IS the project," not a nested subfolder. Confirmed via Implementation
  that `-createProject` handles a non-empty target directory fine (adds its own folders
  alongside existing files) rather than needing the temp-and-move fallback originally
  anticipated.
- **Scope of the Unity skeleton itself:** deliberately limited to Step 1 (folder structure +
  smoke check) plus a *stub* head start on Step 2 (enum/event declarations only, explicit
  `TODO(Step 2)` markers, no real pause/cursor logic) — matching the user's ask for "a code
  skeleton that can be added to later," not a full re-implementation of Steps 2-6's actual
  logic in one shot. Real Step 2 (and 3-6) work is each its own future pipeline task, porting
  from the validated `legacy-godot/` reference.

## Implementation Summary (Director + Implementation Agent)
- Director: `git mv`'d the Godot files into `legacy-godot/`, wrote `legacy-godot/README.md`,
  rewrote `.gitignore` (Unity ignores + narrowed Godot ignore to `legacy-godot/.godot/`),
  rewrote `CLAUDE.md` in full per the approach above.
- Implementation Agent (sonnet, agentId `a27cbb3bb3962ced9`): created the Unity project via
  `Unity.exe -batchmode -nographics -createProject "<repo root>" -quit -logFile <log>` (exit
  0); built all 27 target directories under `Assets/` (each `.gitkeep`'d); added
  `Assets/Editor/Ping.cs` (copied from NinjaGame's exact convention); added
  `Assets/Scripts/Systems/EventBus.cs` (all 12 events from charter 2.1, declarations only);
  added `Assets/Scripts/Systems/GameState.cs` (7-value `State` enum, `CurrentState`, stub
  `SetState()`/`IsPlayerInputLocked()`, `TODO(Step 2)` markers); added
  `Assets/Scripts/Combat/StanceData.cs` (empty `ScriptableObject` stub so `EventBus` compiles,
  `TODO(Step 5)`). Reimported via `-projectPath ... -quit`, confirmed zero `error CS` in the
  log. Did not touch `legacy-godot/`, `CLAUDE.md`, or `docs/` (Director-owned).

Files touched: `.gitignore`, `CLAUDE.md`, `legacy-godot/*` (moved), `legacy-godot/README.md`
(new), plus everything Unity's `-createProject` generates (`Assets/`, `Packages/`,
`ProjectSettings/`, `UserSettings/`) and the new skeleton scripts listed above.

## QA Iterations (QA/Test Agent)
### Attempt 1
- **Method:** QA Agent (sonnet, agentId `a5b8b7aab5fc3077a`) independently re-ran
  `Unity.exe -batchmode -nographics -projectPath "<repo root>" -executeMethod Ping.Run -quit
  -logFile Logs\RR_qa_ping.log`; independently walked `Assets/` with `find` rather than
  trusting the implementer's directory list; read the actual content of `EventBus.cs`,
  `GameState.cs`, `StanceData.cs`, `Ping.cs` and diffed them against CLAUDE.md's 2.1/2.2 spec
  by hand.
- **Result:** **PASS**. Exit code 0. `PING_OK` present in the log. Zero `error CS` matches.
  All 27 directories present, correctly nested, no extras/omissions. `EventBus.cs` matches
  spec exactly (all 11 named events + correct types/params — QA's count of "11" vs. the
  implementer's "12" is just a counting-label difference, both enumerate the same full set).
  `GameState.cs` enum/property/stub methods match spec; unimplemented transition logic is
  correctly recognized as intentional (`TODO(Step 2)`), not a defect. `StanceData.cs` and
  `Ping.cs` both match convention. No discrepancies found. No fix loop needed.

## Director Final Review
- **Findings:** Reviewed the diff directly. The `git mv` history-preserving archive is clean
  — `git status` shows renames, not delete+add, for every Godot file. `.gitignore` correctly
  scopes the old Godot ignore rule to `legacy-godot/.godot/` instead of a bare `.godot/` (which
  would've been a dead rule now with no Godot project at repo root). The charter rewrite does
  not touch Section 1 (World & Acts) or the Steps 9-14 design decisions — verified by diff,
  content-identical apart from the Godot→Unity type/API translation layer. The Unity skeleton
  is appropriately minimal for a Step-1-plus-stub scope: no premature Step 2 logic, no
  speculative systems beyond what the charter's own folder structure calls for. One thing
  intentionally left open rather than silently resolved: whether Step 2's real
  `SetState()`/pause logic should be the very next task — recommended in QA's report and
  echoed in CLAUDE.md's "Current status," but not started here, correctly out of scope for a
  pivot/skeleton task.
- **Sign-off:** Approved. Engine pivot is complete: Godot implementation archived intact at
  `legacy-godot/`, charter fully converted to Unity/C# with no design content lost, Unity
  project skeleton exists at the repo root, compiles clean, and passes a QA-verified headless
  `PING_OK` smoke test. Marking this task done. **Not pushed to the remote** — local commit
  only, per the standing rule against pushing without separate explicit confirmation. Next
  action for a future session: open a real Step 2 task (`GameState`/`EventBus` full
  implementation, porting the validated pause/cursor/event-flow behavior).
