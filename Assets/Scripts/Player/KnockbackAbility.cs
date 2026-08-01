using UnityEngine;

/// <summary>
/// AoE knockback state machine, externally triggered (charter 8.1's Phase-2 boss knockback
/// attack), mirroring DodgeAbility's VelocityOverride mechanism exactly (Research: reuse the
/// same pathway rather than a Rigidbody/force system, matching PlayerMotor's CharacterController
/// -only movement model). Explicit TickKnockback(deltaTime), no own Update() — called by
/// PlayerRoot in its documented per-frame tick order, matching DodgeAbility's pattern.
///
/// Unlike DodgeAbility (which no-ops a second TryDodge while already dodging, protecting player
/// agency/stamina spend), a new ApplyKnockback call while one is already active RESTARTS it
/// rather than no-opping: knockback here is externally triggered by a boss, not player-
/// initiated, so there's no player-agency reason to guard against re-triggering — a fresh
/// knockback should simply take over from wherever the previous one was.
/// </summary>
public sealed class KnockbackAbility : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;

    private bool _isActive;
    private float _elapsed;
    private float _duration;
    private Vector3 _direction;
    private float _force;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<PlayerMotor>();
        }
    }

    /// <summary>
    /// Starts (or restarts, if already active) a knockback in the given direction (normalized
    /// internally) at the given initial force, tapering linearly to zero over duration seconds.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (motor == null)
            return;

        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        _force = force;
        _duration = Mathf.Max(duration, 0.0001f); // guard against a zero/negative duration causing a div-by-zero taper.
        _elapsed = 0f;
        _isActive = true;
    }

    /// <summary>
    /// Advances the active knockback (if any) by one explicit tick. No-op when inactive.
    /// Sets motor.VelocityOverride while active (tapering from full force to zero across
    /// _duration), then clears it and deactivates once _elapsed reaches _duration.
    /// </summary>
    public void TickKnockback(float deltaTime)
    {
        if (!_isActive)
            return;

        float t = Mathf.Clamp01(_elapsed / _duration);
        motor.VelocityOverride = _direction * (_force * (1f - t));

        _elapsed += deltaTime;

        if (_elapsed >= _duration)
        {
            motor.VelocityOverride = null;
            _isActive = false;
        }
    }
}
