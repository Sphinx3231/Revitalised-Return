using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Bridges PlayerLook's accumulated pitch into the first-person camera's
/// CinemachinePanTilt Tilt axis (charter first-person camera pivot, 2026-08-02 --
/// docs/Tasks/2026-08-02-first-person-camera-and-weapon.md). Single responsibility:
/// read-and-write one value, every frame -- no camera-relative math, no input reading of
/// its own (PlayerLook already owns pitch accumulation/clamping to +-80deg).
///
/// This deliberately does NOT use CinemachineInputAxisController (Research's finding: that
/// component binds via InputActionReference assets and has no GameState.
/// IsPlayerInputLocked() gating, inconsistent with this project's house style). Instead
/// PlayerLook.Tick() (driven by PlayerRoot's gated per-frame orchestration) is the only
/// place raw look input is ever read; this class merely relays the already-computed,
/// already-gated result into Cinemachine's own axis field.
///
/// Lives on the CinemachineCamera GameObject itself, not the Player root -- it isn't a
/// player-owned component, so it is NOT part of PlayerRoot's explicit per-frame
/// orchestration (that orchestrator only ticks components under its own authority). A plain
/// Update() here is safe because Cinemachine's own pipeline update (which reads this axis)
/// runs in LateUpdate/the Cinemachine Brain's own update phase, always after regular
/// Update() calls -- so this frame's freshly-ticked PlayerLook.PitchDegrees is always
/// current by the time Cinemachine consumes it.
/// </summary>
public sealed class CameraPitchDriver : MonoBehaviour
{
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private CinemachinePanTilt panTilt;

    private void Update()
    {
        if (playerLook == null || panTilt == null)
            return;

        InputAxis tiltAxis = panTilt.TiltAxis;
        tiltAxis.Value = playerLook.PitchDegrees;
        panTilt.TiltAxis = tiltAxis;
    }
}
