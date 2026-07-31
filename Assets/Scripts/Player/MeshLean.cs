using UnityEngine;

/// <summary>
/// Cosmetic-only mesh tilt on fast direction changes (charter 4.2). Reads
/// PlayerMotor's current horizontal velocity only — does NOT know about input or
/// dodge state.
/// </summary>
public sealed class MeshLean : MonoBehaviour
{
    private const float LeanScale = 0.1f;
    private const float MaxLeanAngle = 5.0f;

    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Transform meshRoot;

    private float _prevYawDeg;
    private bool _hasPrevYaw;

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<PlayerMotor>();
        }
    }

    /// <summary>
    /// Advances the cosmetic lean by one explicit tick. Called by PlayerRoot's Update()
    /// in the documented explicit order (after the motor tick, so it reads this frame's
    /// freshly-updated HorizontalVelocity, not a stale value from last frame). This class
    /// does NOT implement its own Update().
    /// </summary>
    public void TickLean(float deltaTime)
    {
        if (motor == null || meshRoot == null)
            return;

        Vector3 vel = motor.HorizontalVelocity;
        if (vel.sqrMagnitude < 0.0001f)
            return;

        float currentYawDeg = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;

        if (!_hasPrevYaw)
        {
            _prevYawDeg = currentYawDeg;
            _hasPrevYaw = true;
            return;
        }

        float deltaAngle = Mathf.DeltaAngle(_prevYawDeg, currentYawDeg);
        float omega = deltaAngle / deltaTime;
        float leanAngle = -Mathf.Clamp(omega * LeanScale, -MaxLeanAngle, MaxLeanAngle);

        Vector3 euler = meshRoot.localEulerAngles;
        meshRoot.localRotation = Quaternion.Euler(euler.x, euler.y, leanAngle);

        _prevYawDeg = currentYawDeg;
    }
}
