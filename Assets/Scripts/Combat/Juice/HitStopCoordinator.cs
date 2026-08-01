using UnityEngine;

/// <summary>
/// Freeze-frame hit-stop engine (charter 6.1). Explicit-Tick pattern (no Update(), no
/// coroutine) per Step 6's approved approach — a WaitForSecondsRealtime coroutine would work
/// at runtime but can't be driven synchronously from an EditMode test the way the project's
/// established Tick(deltaTime) components (DodgeAbility, AttackController) can.
///
/// Request() clamps duration into the charter-locked 0.03s-0.06s freeze-frame range. A
/// second Request() while already active extends the countdown only if the new duration is
/// longer than what remains (correct for overlapping hits landing back-to-back — the freeze
/// should cover the longer of the two, never shorten an in-progress freeze), otherwise the
/// call is ignored.
/// </summary>
public sealed class HitStopCoordinator : MonoBehaviour
{
    private const float MinDuration = 0.03f;
    private const float MaxDuration = 0.06f;
    private const float DefaultDuration = 0.045f;

    private float _remaining;

    /// <summary>True while an active hit-stop freeze (Time.timeScale == 0f, owned by this
    /// coordinator) is in progress.</summary>
    public bool IsActive => _remaining > 0f;

    /// <summary>
    /// Requests a hit-stop freeze. duration is clamped to [0.03s, 0.06s]; pass a value
    /// outside that range and it will be clamped, or omit it for the default 0.045s.
    /// </summary>
    public void Request(float duration = DefaultDuration)
    {
        float clamped = Mathf.Clamp(duration, MinDuration, MaxDuration);

        if (_remaining <= 0f)
        {
            _remaining = clamped;
            Time.timeScale = 0f;
            return;
        }

        // Already active: extend only if the new request is longer than what remains.
        if (clamped > _remaining)
        {
            _remaining = clamped;
        }
    }

    /// <summary>
    /// Advances the active hit-stop (if any) by one explicit unscaled-time tick. No-op when
    /// not currently active. Restores Time.timeScale to 1f the instant the countdown hits
    /// zero.
    /// </summary>
    public void Tick(float unscaledDeltaTime)
    {
        if (_remaining <= 0f)
            return;

        _remaining -= unscaledDeltaTime;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            Time.timeScale = 1f;
        }
    }
}
