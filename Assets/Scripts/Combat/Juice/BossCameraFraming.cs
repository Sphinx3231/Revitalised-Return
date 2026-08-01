using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Swaps the existing PlayerFollowCam rig's Follow/LookAt between the player alone and a
/// CinemachineTargetGroup midpoint of player+boss (charter 8.2's camera midpoint tracking:
/// cam_target = (player_position + boss_position) / 2 + isometric_offset). Reuses the rig's
/// existing Body/Aim/Noise/CameraTrauma wiring untouched — Research's decisive finding against
/// a second dedicated camera, since Step 6's CameraTrauma is wired to one specific
/// CinemachineBasicMultiChannelPerlin instance that a second camera would need re-pointed or
/// duplicated.
///
/// Member weight=1/radius=0 per Research's live-verified numeric finding: TargetGroup.Sphere
/// is a radius-weighted average position, not a plain midpoint — radius=0 per member is
/// required to match the charter's literal (player+boss)/2 formula (a nonzero radius biases
/// the average toward whichever member has the larger radius).
/// </summary>
public sealed class BossCameraFraming : MonoBehaviour
{
    [SerializeField] private CinemachineCamera playerFollowCam;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bossTransform;

    /// <summary>
    /// The CinemachineTargetGroup this framing reads. Auto-added to this GameObject in Awake
    /// if not pre-wired (e.g. by a test or by scene setup wiring an existing component).
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
    /// Reframes the existing PlayerFollowCam rig onto the player/boss midpoint for the
    /// duration of a boss encounter.
    /// </summary>
    public void StartEncounter()
    {
        if (playerFollowCam == null || targetGroup == null)
            return;

        playerFollowCam.Follow = targetGroup.transform;
        playerFollowCam.LookAt = targetGroup.transform;
    }

    /// <summary>
    /// Restores the rig's original single-target tracking on the player (e.g. on boss defeat).
    /// </summary>
    public void EndEncounter()
    {
        if (playerFollowCam == null || playerTransform == null)
            return;

        playerFollowCam.Follow = playerTransform;
        playerFollowCam.LookAt = playerTransform;
    }
}
