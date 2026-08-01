using UnityEngine;

/// <summary>
/// Parry/block state owner (charter S.O.L.I.D. split, mirrors DodgeAbility's explicit-tick
/// pattern). Does NOT know about input actions — the only entry point is TryParry(), called
/// by PlayerRoot once it has already consumed the buffered parry action. Advances via an
/// explicit TickParry(deltaTime) called by PlayerRoot's Update(), not a Coroutine, matching
/// every other player component.
/// </summary>
public sealed class ParryController : MonoBehaviour
{
    [SerializeField] private StanceController stanceController;

    private bool _isParrying;
    private float _windowRemaining;

    public bool IsParrying => _isParrying;

    // Standing charter gap (carried forward from the original Godot Step 5 implementation,
    // logged in docs/Worklog.md history): the Input Actions asset has no dedicated "block"
    // action yet. IsBlocking is pure exposed state for a future AI/debug harness to drive,
    // intentionally NOT wired to any player input this task.
    public bool IsBlocking { get; set; }

    public void TryParry()
    {
        if (_isParrying)
            return;

        _isParrying = true;
        _windowRemaining = stanceController != null && stanceController.CurrentStance != null
            ? stanceController.CurrentStance.parryWindowDuration
            : 0.12f;
    }

    /// <summary>
    /// Advances the active parry window (if any) by one explicit tick. No-op when not
    /// currently parrying.
    /// </summary>
    public void TickParry(float deltaTime)
    {
        if (!_isParrying)
            return;

        _windowRemaining -= deltaTime;

        if (_windowRemaining <= 0f)
        {
            _isParrying = false;
        }
    }
}
