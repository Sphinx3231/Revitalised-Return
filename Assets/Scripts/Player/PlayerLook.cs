using UnityEngine;

/// <summary>
/// First-person camera pivot (2026-08-02 charter deviation, see
/// docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). Accumulates raw look-input
/// (mouse delta / gamepad right-stick) into yaw and pitch. Yaw is applied directly to this
/// component's own Transform.rotation (this component is expected to sit on the Player
/// root, same as MeshLean/PlayerRoot) as an absolute value each tick, not an incremental
/// quaternion multiply -- this keeps YawDegrees a single authoritative accumulator with no
/// drift, and makes the class trivially pure-logic-testable. Pitch is NOT applied to any
/// Transform here -- it is only accumulated and exposed via PitchDegrees for the
/// Cinemachine PanTilt Aim component (on the camera, a child of this root) to read.
///
/// This is the standard "body yaws, camera-child pitches" FPS pattern, chosen specifically
/// to sidestep a feedback-loop/1-frame-lag risk: reading the camera's rotation back AFTER
/// Cinemachine's LateUpdate to drive the root would always be one frame stale. Instead the
/// root (this component) is the single source of truth for yaw, set the same frame the
/// input is read -- Cinemachine's own LateUpdate then reads this already-updated root
/// transform (via the EyeSocket child) later in the same frame, so the camera never lags.
///
/// Ticked explicitly from PlayerRoot's per-frame orchestration (this project's house style
/// -- no implicit Update() here). Gating on GameState.IsPlayerInputLocked() is inherited for
/// free from PlayerRoot.Update()'s own top-level guard, exactly the same way PlayerMotor's
/// continuous MoveRaw read is gated -- Tick() is simply never called while input is locked.
/// </summary>
public sealed class PlayerLook : MonoBehaviour
{
    private const float MinPitchDeg = -80f;
    private const float MaxPitchDeg = 80f;

    [SerializeField] private float yawSensitivity = 0.02f;
    [SerializeField] private float pitchSensitivity = 0.02f;
    [SerializeField] private bool invertPitch;

    private float _yawDeg;
    private float _pitchDeg;

    /// <summary>Accumulated yaw in degrees, applied each Tick to this Transform's rotation.</summary>
    public float YawDegrees => _yawDeg;

    /// <summary>
    /// Accumulated pitch in degrees, clamped to [-80, 80]. Positive = looking up. Read by
    /// the Cinemachine PanTilt Aim component's Tilt axis -- never applied to a Transform by
    /// this class directly.
    /// </summary>
    public float PitchDegrees => _pitchDeg;

    /// <summary>
    /// Advances yaw/pitch accumulation by one explicit tick from a raw look delta (mouse
    /// delta or gamepad right-stick value). Applies the new yaw to this Transform
    /// immediately (root yaw, charter 4.1's WeaponPivot-facing bug fix as a side effect).
    /// </summary>
    public void Tick(Vector2 lookDelta)
    {
        _yawDeg += lookDelta.x * yawSensitivity;

        float pitchDelta = lookDelta.y * pitchSensitivity * (invertPitch ? -1f : 1f);
        _pitchDeg = Mathf.Clamp(_pitchDeg + pitchDelta, MinPitchDeg, MaxPitchDeg);

        transform.rotation = Quaternion.Euler(0f, _yawDeg, 0f);
    }
}
