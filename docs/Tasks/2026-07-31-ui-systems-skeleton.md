# UI & Core Systems Skeleton Pass (Settings / Main Menu / Keybinds / Minimap / Inventory) — 2026-07-31

> **SUPERSEDED 2026-07-31.** Deliverable 1 (Input Actions asset) was completed instead as part
> of `docs/Tasks/2026-07-31-player-base-character.md` (Phase 1). Deliverables 2-3 (MainMenu/
> Settings scene, no minimap this pass) were completed instead as part of
> `docs/Tasks/2026-07-31-ui-systems-phase2.md` (Phase 2). Deliverables 5-6 (inventory data
> types + UI stub) remain genuinely unstarted — out of scope for both — and are deferred to
> Step 10 proper. This file is kept for its research/blocker-resolution record (the Input
> System `TreeView` compile-blocker investigation below remains accurate and useful), not
> deleted.

## Task Brief (Director)
- **Goal:** at the user's explicit request, begin a breadth-first **skeleton** pass across
  several systems that normally belong to separate later pipeline steps — Keybinds (Step
  3's Input Actions asset), Main Menu + Settings panel and Minimap (Step 11's HUD/UI), and
  Inventory data + a minimal UI stub (Step 10). This is a deliberate deviation from the
  charter's strict sequential step order: the user wants visible skeleton scaffolding across
  these systems now, not a single step taken to full depth. Matches the precedent already set
  by the Unity-pivot task (Step 1 + stub Step 2) — skeletons with `TODO(Step N)` markers, not
  full implementations, so the real step work still happens later through the normal pipeline
  and isn't silently marked done here.
- **Affected systems:** `Packages/manifest.json` (new package deps), `Assets/Settings/`
  (Input Actions asset), `Assets/Scenes/` (new MainMenu scene), `Assets/Scripts/UI/`,
  `Assets/Scripts/Systems/` (Inventory data types), `Assets/Prefabs/` (HUD/minimap/inventory
  panel prefabs).
- **Constraints:** skeleton only — no real save/load wiring, no real rebinding persistence,
  no real minimap icon logic, no real inventory-to-HUD data binding yet; every stubbed
  behavior gets an explicit `TODO(Step N)` comment naming the step that will finish it, per
  the pattern already established in `EventBus.cs`/`GameState.cs`/`StanceData.cs`. Must not
  mark Steps 3, 10, or 11 as "done" in `CLAUDE.md`/`Worklog.md` — this is scaffolding, not
  step completion. Must still compile clean and pass the `Ping.Run()` headless smoke check.
- **Definition of done:** an Input Actions asset exists with the 3.1 keybind list wired
  (keyboard + gamepad); a MainMenu scene exists with Play/Settings/Quit and a Settings panel
  stub (graphics/audio/controls placeholder tabs, including a keybind-rebind-list stub); a
  minimap skeleton exists (second top-down camera → RenderTexture → HUD RawImage); inventory
  data types (`ItemData` ScriptableObject, `ItemStack`, `Inventory`) exist per Step 10's spec
  plus a minimal grid-of-slots UI stub with no real population logic; project reimports clean
  (zero `error CS`) and `Ping.Run()` still returns `PING_OK`.

## Research Findings (Director, self-researched)
- `Packages/manifest.json` currently has no `com.unity.inputsystem` and no `com.unity.ugui`
  package — only built-in engine modules (`com.unity.modules.ui`,
  `com.unity.modules.uielements`, which are the legacy IMGUI/UI Toolkit runtime modules, not
  Canvas-based uGUI). Unity 6's `-createProject` does not add these by default. Both are
  needed for this task (Input System for keybinds per CLAUDE.md 3.1's explicit design choice
  already locked at the pivot; uGUI `Canvas`/`Image`/`Button`/`RawImage` for the Settings/Main
  Menu/Minimap/Inventory UI, since UI Toolkit's runtime UI Document workflow is a bigger
  lift and not what the charter's Step 11 spec assumes). Implementation Agent must add both
  to `manifest.json` and confirm they resolve via a headless reimport (`-batchmode -quit`) —
  if package resolution fails offline/otherwise, report back rather than guessing further.
- Version pinning: not independently verified against 6000.5.5f1 compatibility tables before
  this task — Implementation should let Unity's Package Manager resolve compatible versions
  automatically (omit explicit version numbers or use "latest compatible" default flow) rather
  than hand-picking version strings that might not match this Editor build.

## Approach & Tradeoffs (Director sign-off)
- **Scenes:** put `MainMenu.unity` directly under `Assets/Scenes/` (sibling to `Levels/` and
  `Sandbox/`, not inside either) — it's neither a playable level nor a mechanic-test sandbox.
  Judgment call, not in the charter's literal folder spec; logging it here rather than
  silently deciding.
- **Minimap approach:** a second `Camera` (orthographic, top-down, culling mask limited to a
  `Minimap` layer + terrain) rendering into a `RenderTexture`, displayed via a `RawImage` on
  a HUD `Canvas` — this is the standard, well-documented Unity pattern and avoids any
  dependency decision (no third-party minimap asset).
- **Inventory:** implement the actual Step 10 data shapes now (`ItemData`/`ItemStack`/
  `Inventory`) rather than placeholder stand-ins, since CLAUDE.md 10's spec is already fully
  written and there's no ambiguity to defer — but the UI stays a dumb, unpopulated slot grid
  (no drag-drop, no real item instances yet, no persistence).
- **Keybinds:** build the real Input Actions asset with the real 3.1 binding list (including
  the standing `stance_prev` = Tab amendment) since, again, the charter already fully
  specifies it — no reason to stub what's already fully designed. What stays a stub is the
  buffer/consumption logic (`InputBuffer`, Step 3.2) and any in-game rebind-and-persist UI
  logic — the rebind *screen* exists, wiring it to actually save is `TODO(Step 3)`.

## Implementation Summary (Implementation Agent)
**BLOCKED at deliverable 1 (packages), per the brief's own "stop and report, don't thrash"
instruction — deliverables 2-6 (Input Actions asset, MainMenu/Settings scene, minimap
skeleton, inventory data + UI stub) were never attempted.**

- Added `com.unity.inputsystem` and `com.unity.ugui` to `Packages/manifest.json`.
  `com.unity.ugui` resolved clean (builtin `2.5.0`, zero errors).
  `com.unity.inputsystem` resolves (tried `1.11.2`, `1.14.0` — newest available on the
  reachable registry — and confirmed no newer version exists) but produces **264 `error
  CS0619`** compile errors, all inside the package's own Editor-only source
  (`InputActionEditorWindow.cs`, `InputActionTreeView.cs`, `InputDebuggerWindow.cs`,
  `HIDDescriptorWindow.cs`), not project code.
- **Root cause (verified via grep against `UnityEditor.CoreModule.dll`):** Editor build
  `6000.5.5f1` (revision `d16e074b49fd`) has made the legacy `UnityEditor.IMGUI.Controls.
  TreeView`/`TreeViewItem`/`TreeViewState` APIs a hard compile **error**, not a warning. The
  newest `com.unity.inputsystem` obtainable here (`1.14.0`) still uses that legacy API in its
  own Editor UI. Every version of Input System currently reachable is therefore
  source-incompatible with this specific Editor build — this is an Editor/package version
  mismatch, not something fixable by picking a different Input System version or by anything
  in project code.
- This blocks batchmode entirely (`Aborting batchmode due to failure: Scripts have compiler
  errors.`), so nothing downstream — not even a bare `Ping.Run()` — can be verified right now
  while the packages stay in `manifest.json` in their current form.
- Repo left with `Packages/manifest.json`/`packages-lock.json` reflecting the
  resolved-but-broken package set, and diagnostic logs at
  `Revitalised Return/Logs/reimport_final.log` (+ `reimport1-3.log`) for whoever picks this
  back up. No scene/script/prefab deliverables were created (would be unverifiable against a
  non-compiling project). `CLAUDE.md`/`Worklog.md`/this file were left for the Director.

## QA Iterations (QA/Test Agent)
Not run — task paused at the Director's instruction before Implementation cleared the
package blocker. Nothing to QA yet (see Implementation Summary).

## Blocker Resolution — 2026-07-31 (resumed)
Research (self, Director): the `TreeView`/`TreeViewItem`/`TreeViewState` non-generic APIs
became hard `CS0619` compile errors specifically in Unity `6000.5.0f1`/`6000.5.1f1` (this
project is on `6000.5.5f1`). `com.unity.inputsystem` fixed its own `TreeView` usage for this
Editor line in package version **1.15.0** (fix originally landed for the 6.2 beta), and
Unity separately ships **1.20.0** as the version paired with the 6000.5 Editor branch. The
original attempt topped out at `1.14.0` — one version short of the fix — which was a registry
resolution ceiling in that run, not a real version-availability limit.

Fix applied: pinned `"com.unity.inputsystem": "1.20.0"` in `Packages/manifest.json` (up from
`1.14.0`) and re-ran the headless reimport. Result: **zero `error CS0619` (or any other
compile errors)**, exit code 0. Independently re-verified with `-executeMethod Ping.Run`:
exit 0, `PING_OK` present, zero compile errors — the project is back to a clean, verified
baseline.

**Concurrent, user-initiated change:** while this was being fixed, the user separately ran
`unity-mcp-cli install-plugin` (a third-party open-source tool, `IvanMurzak/Unity-MCP`,
verified legitimate before recommending it), which added `com.ivanmurzak.unity.mcp` and an
OpenUPM scoped registry to the same `manifest.json`. The post-fix reimport verified the
*combined* package set (Input System 1.20.0 + the MCP plugin together) compiles clean — not
just the Input System fix in isolation.

`com.unity.ugui` (added in the original attempt) also remains in `manifest.json`, resolved
clean throughout (was never part of the blocker).

## Director Final Review
**Paused mid-task (user requested "pause" / "save and end for a moment"), then resumed to
clear the blocker above. Deliverables 2-6 (Input Actions asset, MainMenu/Settings scene,
minimap skeleton, inventory data + UI stub) still not started — this session only cleared
the compile blocker so the project is a safe, clean base to build them on next.** Status at
this checkpoint:

- Task brief and approach (above) are still valid and don't need rework.
- Real blocker found: this Unity Editor build (`6000.5.5f1`) rejects the only
  `com.unity.inputsystem` versions currently reachable, because the package's own Editor code
  uses a Unity API (`TreeView`) that this Editor build demoted from deprecation-warning to
  compile-error. This is an external compatibility gap, not a project mistake.
- **Left in a safe, non-broken state:** `Packages/manifest.json` currently references the
  broken package combo, so the *next* headless batchmode run against this project (e.g. a
  QA smoke test, or reopening in the Editor) will show compile errors until this is resolved.
  Whoever resumes should treat clearing this blocker as the first step, before attempting any
  of deliverables 2-6.
- **Options for resuming, not yet decided:** (a) pin `com.unity.inputsystem` to an older
  pre-1.11 version that might predate the package's `TreeView` usage — unverified, would need
  Research; (b) skip the Input System package for now and build keybinds on Unity's legacy
  Input Manager instead (contradicts the charter's already-locked 3.1 design choice — would
  need a Director ruling to amend); (c) wait for a newer Input System / Editor patch release;
  (d) drop back to `com.unity.inputsystem` versions old enough to predate this specific
  breakage (needs a version-compatibility research pass, not guessed at here). No decision
  made yet — next session should open this with a real Research Agent pass on option (a)/(d)
  before picking.
