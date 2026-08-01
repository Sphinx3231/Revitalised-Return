using UnityEngine;

/// <summary>
/// Boss-side implementation of IStanceSource (charter 8.1's Phase-2 "stance mirroring" — the
/// boss's AttackController tracks the player's own StanceController.CurrentStance, reusing the
/// existing stance-multiplier system rather than inventing a parallel one). Trivial pass-
/// through by design (Approach's explicit "intentionally trivial" note).
///
/// Wired into the boss's AttackController from scene setup, from the start — no runtime field
/// re-wiring needed. Stays inert (CurrentStance returns null, i.e. stance-neutral 1.0x
/// multipliers, same as an unwired StanceController) until BossPhaseController calls
/// SetActive(true) at the Phase 2 transition — Phase 1 boss attacks are intentionally stance-
/// neutral per Approach's "mirroring is a Phase-2-specific enrage detail, not a Phase-1
/// behavior" ruling.
/// </summary>
public sealed class BossStanceMirror : MonoBehaviour, IStanceSource
{
    [SerializeField] private StanceController playerStanceController;

    private bool _active;

    /// <summary>True once BossPhaseController has engaged Phase-2 stance mirroring.</summary>
    public bool IsActive => _active;

    /// <summary>Engaged/disengaged by BossPhaseController. Not player/input driven.</summary>
    public void SetActive(bool active)
    {
        _active = active;
    }

    public StanceData CurrentStance =>
        _active && playerStanceController != null ? playerStanceController.CurrentStance : null;
}
