using UnityEngine;

/// <summary>
/// Dodge roll state machine (charter 4.3). Does NOT know about input actions — the
/// only entry point is TryDodge(direction), called by PlayerRoot once it has already
/// consumed the buffered dodge action and resolved a facing/move direction. Overrides
/// PlayerMotor's horizontal velocity for the dodge's full duration. Advances via an
/// explicit TickDodge(deltaTime) called by PlayerRoot's Update() in the documented
/// order, rather than a Coroutine — a coroutine runs on Unity's own scheduling, which
/// doesn't fit an explicitly-ticked-by-parent model cleanly.
/// </summary>
public sealed class DodgeAbility : MonoBehaviour, IInvulnerabilityProvider
{
    private const float TotalDuration = 0.5f;
    private const float BurstMultiplier = 1.8f;
    private const float TaperedMultiplier = 1.0f;
    private const float IFrameStart = 0.15f;
    private const float IFrameEnd = 0.35f;
    private const float StaminaCost = 20.0f;

    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Collider hurtboxCollider;
    [SerializeField] private float stamina = 100f;

    private bool _isDodging;
    private float _elapsed;
    private Vector3 _dodgeDirection;

    public bool IsInvulnerable { get; private set; }
    public bool IsDodging => _isDodging;

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<PlayerMotor>();
        }
    }

    public void TryDodge(Vector3 direction)
    {
        if (_isDodging)
            return;

        if (stamina < StaminaCost)
            return;

        if (motor == null)
            return;

        if (direction == Vector3.zero)
            direction = transform.forward;

        stamina -= StaminaCost;
        _dodgeDirection = direction.normalized;
        _elapsed = 0f;
        _isDodging = true;
    }

    /// <summary>
    /// Advances the active dodge (if any) by one explicit tick. No-op when not
    /// currently dodging. Called by PlayerRoot's Update() after the dodge-trigger
    /// check, before the motor tick, so the motor's VelocityOverride is fresh for
    /// this frame's TickMotor call.
    /// </summary>
    public void TickDodge(float deltaTime)
    {
        if (!_isDodging)
            return;

        float t = _elapsed / TotalDuration;
        float speedMultiplier = Mathf.Lerp(BurstMultiplier, TaperedMultiplier, t);
        motor.VelocityOverride = _dodgeDirection * (PlayerMotor.Speed * speedMultiplier);

        bool inIFrameWindow = _elapsed >= IFrameStart && _elapsed <= IFrameEnd;
        SetInvulnerable(inIFrameWindow);

        _elapsed += deltaTime;

        if (_elapsed >= TotalDuration)
        {
            SetInvulnerable(false);
            motor.VelocityOverride = null;
            _isDodging = false;
        }
    }

    private void SetInvulnerable(bool invulnerable)
    {
        IsInvulnerable = invulnerable;
        if (hurtboxCollider != null)
        {
            hurtboxCollider.enabled = !invulnerable;
        }
    }
}
