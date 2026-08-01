using UnityEngine;

/// <summary>
/// Weapon arc trail activator (charter 6.3). Mirrors AttackController.IsAttacking onto a
/// TrailRenderer's emitting flag every explicit tick — genuinely one line of logic, thin by
/// design (Step 6 approved approach). Real weapon-mesh-driven trail timing (Animation-Event
/// gated, per charter 5.2's hitbox-toggle convention) is Step 13 territory; this is an
/// acknowledged placeholder-art limitation, not a bug, since no visible weapon mesh exists
/// yet (see the Step 6 task brief).
/// </summary>
public sealed class TrailActivator : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private AttackController attackController;

    /// <summary>
    /// Exposed (Step 8) so BossPhaseController can tint the trail's start/end colors for the
    /// Phase-2 "enraged" visual cue (charter 8.2's flavor text, reusing this existing trail
    /// per Research's finding rather than a new VFX asset) without this class needing to know
    /// anything about boss phases itself.
    /// </summary>
    public TrailRenderer TrailRenderer => trailRenderer;

    /// <summary>
    /// Syncs the wired TrailRenderer's emitting state to the wired AttackController's
    /// IsAttacking state. No-op if either reference is unwired.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (trailRenderer == null || attackController == null)
            return;

        trailRenderer.emitting = attackController.IsAttacking;
    }
}
