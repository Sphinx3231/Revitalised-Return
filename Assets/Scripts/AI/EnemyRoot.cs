using UnityEngine;

/// <summary>
/// Enemy-side mirror of PlayerRoot's single-orchestrator role (charter S.O.L.I.D. split).
/// A REAL Update() — the per-frame driver for the enemy, same exception-to-the-no-Update()-
/// rule justification JuiceCoordinator uses on the player side. Fixes the Step 7 sign-off bug:
/// nothing anywhere in the project called EnemyBrain.Tick(), so the Step 7 enemy was completely
/// inert in Play Mode (see docs/Tasks/2026-08-01-step-7-ai-perception-fsm.md's "Post-Signoff
/// Bug Found" section). Mandatory on every enemy, including the base (non-boss) chassis.
///
/// Deviation from the Step 8 task brief's literal driver order: the brief specifies calling
/// perception.Tick(deltaTime) THEN brain.Tick(deltaTime) explicitly. Reading EnemyBrain.cs
/// (verified, not assumed) shows Tick() already calls perception?.Tick(deltaTime) internally
/// as its first line, and also already calls motor?.TickMotor(deltaTime) as its last line.
/// Calling perception.Tick() again here would double-tick its 0.1s accumulator every frame,
/// silently halving the perception interval — a real behavior bug, not just redundant. So
/// EnemyRoot.Update() calls ONLY brain.Tick(deltaTime); perception and motor are still ticked,
/// exactly once each, via EnemyBrain's own existing internal calls.
///
/// Also drives an optional BossPhaseController.Tick(deltaTime) (null for base enemies, wired
/// for the Boss prefab variant) — kept on this same class rather than a separate boss-only
/// root so the enemy chassis stays a single reusable class for both base enemies and bosses.
/// </summary>
public sealed class EnemyRoot : MonoBehaviour
{
    [SerializeField] private EnemyMotor motor;
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyBrain brain;

    /// <summary>Boss-only. Left unassigned (null) on base enemies.</summary>
    [SerializeField] private BossPhaseController bossPhaseController;

    private void Update()
    {
        if (brain == null)
            return;

        float deltaTime = Time.deltaTime;

        // EnemyBrain.Tick() already ticks perception internally (first line) and the motor
        // internally (last line) -- see the class doc comment above for why this class does
        // NOT also call perception.Tick()/motor.TickMotor() directly.
        brain.Tick(deltaTime);

        bossPhaseController?.Tick(deltaTime);
    }
}
