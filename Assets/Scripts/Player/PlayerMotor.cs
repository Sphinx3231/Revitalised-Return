using UnityEngine;

/// <summary>
/// Owns the CharacterController and the locked kinematics formulas (charter 4.1).
/// Does NOT read input directly (Dependency Inversion) — only accepts a desired
/// camera-relative direction vector via SetDesiredDirection, set every frame by
/// PlayerRoot. DodgeAbility can override the horizontal velocity for its active
/// window via VelocityOverride.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMotor : MonoBehaviour
{
    /// <summary>Locked base movement speed (charter 4.1's S_speed), exposed so
    /// DodgeAbility can derive its burst/taper multiplier from the same constant.</summary>
    public const float Speed = 6.0f;
    private const float AlphaAccel = 15.0f;
    private const float AlphaFriction = 20.0f;
    private const float Gravity = 24.5f;
    private const float GroundedVerticalVelocity = -2f;

    private CharacterController _controller;
    private Vector3 _desiredDirection = Vector3.zero;
    private Vector3 _horizontalVelocity = Vector3.zero;
    private float _verticalVelocity;

    /// <summary>
    /// When set, this overrides the lerped horizontal velocity for the current frame
    /// (used by DodgeAbility during its active window). Set to null to resume normal
    /// motor-driven kinematics.
    /// </summary>
    public Vector3? VelocityOverride { get; set; }

    public Vector3 HorizontalVelocity => _horizontalVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Sets the desired movement direction (already camera-relative, flattened,
    /// normalized) for this frame. Called by PlayerRoot, not read internally.
    /// </summary>
    public void SetDesiredDirection(Vector3 direction)
    {
        _desiredDirection = direction;
    }

    /// <summary>
    /// Advances the motor's kinematics by one explicit tick. Called by PlayerRoot's
    /// Update() in the documented explicit order — this class does NOT implement its
    /// own Update() so there is no risk of it running on a separate, unordered schedule.
    /// </summary>
    public void TickMotor(float deltaTime)
    {
        if (VelocityOverride.HasValue)
        {
            _horizontalVelocity = VelocityOverride.Value;
        }
        else
        {
            Vector3 targetVelocity = _desiredDirection * Speed;
            float alpha = _desiredDirection != Vector3.zero ? AlphaAccel : AlphaFriction;
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, alpha * deltaTime);
        }

        _verticalVelocity -= Gravity * deltaTime;
        if (_controller.isGrounded)
        {
            _verticalVelocity = GroundedVerticalVelocity;
        }

        Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime;
        _controller.Move(motion);
    }
}
