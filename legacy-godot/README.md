# Archived — Godot 4 implementation (superseded 2026-07-31)

This folder holds the original Godot 4.7 / GDScript implementation of Project Return
(Steps 1-6 of the 14-step pipeline: project init, FSM/EventBus, input buffer,
kinematics/dodge, stance/hitbox combat, juice engine). The project pivoted to
**Unity 6000.5.5f1 / C#** on 2026-07-31 — see `docs/Tasks/2026-07-31-godot-to-unity-pivot.md`
and the root `CLAUDE.md` for the pivot decision and the new charter.

Nothing here is deleted — it stays as reference for the tested game-feel numbers
(dodge i-frame ticks, parry windows, hit-stop duration, camera trauma decay, etc.)
that carry over into the Unity port. It is not part of the active Unity project and
is excluded from Unity's asset import (lives outside `Assets/`).

Step 6 (juice engine) was implementation-complete and QA-passed but never reached
Director sign-off/commit before the pivot — its files here (`autoload/hit_stop.gd`,
`scripts/combat/spark_pool.gd`, `scripts/combat/weapon_trail.gd`,
`scripts/player/camera_trauma.gd`, `assets/shaders/hit_flash.gdshader`,
`scripts/tests/juice_test.*`) are the same untracked-at-pivot-time work, kept for
reference to the same degree as the committed steps.
