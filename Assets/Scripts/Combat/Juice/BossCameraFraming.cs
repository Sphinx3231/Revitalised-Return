using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Charter 8.2's boss camera midpoint tracking (cam_target = (player_position +
/// boss_position) / 2 + isometric_offset) -- SCOPE DOWN per the 2026-08-02 first-person
/// camera pivot (docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). This class's
/// original entire purpose was repointing PlayerFollowCam's Follow/LookAt to a
/// CinemachineTargetGroup midpoint of player+boss. The player camera rig is now a
/// CinemachineHardLockToTarget mount rigidly tracking the player's own EyeSocket child
/// Transform (first-person, Damping 0) -- repointing that same hard-locked camera to a
/// floating midpoint in space would no longer be a first-person camera at all, so the
/// Follow/LookAt repoint is INCOMPATIBLE and has been removed, not adapted.
///
/// Research reported three feasible alternatives (a second boss vcam + priority blend, a
/// degrade-to-no-op, or a CinemachineGroupFraming extension needing a non-hard-locked
/// camera). The Director chose to keep the FPS rig live through boss fights and explicitly
/// descope the literal midpoint framing rather than build a new PanTilt-recenter-toward-boss
/// system in this already-large pivot -- that recenter system is a real, named follow-up,
/// not silently dropped (see the task file's Approach section).
///
/// StartEncounter()/EndEncounter() are therefore explicit no-ops with respect to camera
/// framing. BossPhaseController.OnEnable()/TriggerDefeat() still call them unconditionally
/// (untouched per this task's scope -- only this file's internals changed), so the
/// descope decision stays visible at those call sites rather than the calls just vanishing.
///
/// EnsureTargetGroup()/TargetGroup are kept: the midpoint-tracking CinemachineTargetGroup
/// itself is harmless to keep computing (nothing repoints a camera to it anymore) and is
/// exactly the building block a future PanTilt-recenter follow-up would reuse rather than
/// reinvent.
/// </summary>
public sealed class BossCameraFraming : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bossTransform;

    /// <summary>
    /// The CinemachineTargetGroup this framing computes. Auto-added to this GameObject in
    /// Awake if not pre-wired (e.g. by a test or by scene setup wiring an existing
    /// component). No longer consumed as a camera Follow/LookAt target by this class (see
    /// class doc comment) -- retained as the reusable midpoint-tracking building block for
    /// a future recenter system.
    /// </summary>
    [SerializeField] private CinemachineTargetGroup targetGroup;

    public CinemachineTargetGroup TargetGroup => targetGroup;

    private void Awake()
    {
        EnsureTargetGroup();
    }

    /// <summary>
    /// Ensures targetGroup exists and has player/boss added as members (weight=1, radius=0
    /// each). Idempotent — safe to call multiple times (e.g. once from Awake, once explicitly
    /// from a test that doesn't pump Unity's lifecycle). Does not add a member twice.
    /// </summary>
    public void EnsureTargetGroup()
    {
        if (targetGroup == null)
        {
            targetGroup = GetComponent<CinemachineTargetGroup>();
        }

        if (targetGroup == null)
        {
            targetGroup = gameObject.AddComponent<CinemachineTargetGroup>();
        }

        if (playerTransform != null && targetGroup.FindMember(playerTransform) < 0)
        {
            targetGroup.AddMember(playerTransform, 1f, 0f);
        }

        if (bossTransform != null && targetGroup.FindMember(bossTransform) < 0)
        {
            targetGroup.AddMember(bossTransform, 1f, 0f);
        }
    }

    /// <summary>
    /// No-op with respect to camera framing (see class doc comment) -- the hard-locked FPS
    /// rig stays on the player's EyeSocket throughout boss encounters. Kept as a call site
    /// (rather than removed) so BossPhaseController.OnEnable's existing call documents the
    /// descope decision instead of silently vanishing.
    /// </summary>
    public void StartEncounter()
    {
    }

    /// <summary>No-op with respect to camera framing — see StartEncounter().</summary>
    public void EndEncounter()
    {
    }
}
