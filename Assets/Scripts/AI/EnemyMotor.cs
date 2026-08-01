using UnityEngine;

/// <summary>
/// Enemy-side mirror of PlayerMotor (charter 7 Research recommendation: plain
/// CharacterController-based motor, no NavMeshAgent, so it stays compatible with this
/// project's explicit-Tick testability convention). Same locked kinematics constants as
/// PlayerMotor (charter 4.1) plus a SpeedScale multiplier so EnemyBrain can drive Patrol at
/// 50% speed and Investigate/chase at full speed without duplicating the lerp formulas.
/// Does NOT read AI decision state itself (Dependency Inversion) — only accepts a desired
/// direction via SetDesiredDirection, set every tick by EnemyBrain. No own Update(); driven
/// purely by EnemyBrain.Tick(), matching PlayerRoot's single-orchestrator pattern.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class EnemyMotor : MonoBehaviour
{
    /// <summary>Locked base movement speed (charter 4.1's S_speed), same value as PlayerMotor.</summary>
    public const float Speed = 6.0f;
    private const float AlphaAccel = 15.0f;
    private const float AlphaFriction = 20.0f;
    private const float Gravity = 24.5f;
    private const float GroundedVerticalVelocity = -2f;

    /// <summary>
    /// Multiplies the target speed (S_speed * SpeedScale). EnemyBrain sets this to 0.5 for
    /// Patrol (charter 7.2's "50% movement speed") and 1.0 for Investigate/chase.
    /// </summary>
    public float SpeedScale = 1.0f;

    private CharacterController _controller;
    private Vector3 _desiredDirection = Vector3.zero;
    private Vector3 _horizontalVelocity = Vector3.zero;
    private float _verticalVelocity;

    public Vector3 HorizontalVelocity => _horizontalVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Sets the desired movement direction (already flattened, normalized) for this tick.
    /// Called by EnemyBrain, not read internally.
    /// </summary>
    public void SetDesiredDirection(Vector3 direction)
    {
        _desiredDirection = direction;
    }

    /// <summary>
    /// Advances the motor's kinematics by one explicit tick. Called by EnemyBrain's
    /// Tick(deltaTime) in its documented explicit order — this class does NOT implement its
    /// own Update() so there is no risk of it running on a separate, unordered schedule.
    /// </summary>
    public void TickMotor(float deltaTime)
    {
        Vector3 targetVelocity = _desiredDirection * (Speed * SpeedScale);
        float alpha = _desiredDirection != Vector3.zero ? AlphaAccel : AlphaFriction;
        _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, alpha * deltaTime);

        _verticalVelocity -= Gravity * deltaTime;
        if (_controller.isGrounded)
        {
            _verticalVelocity = GroundedVerticalVelocity;
        }

        Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime;
        _controller.Move(motion);
    }
}
