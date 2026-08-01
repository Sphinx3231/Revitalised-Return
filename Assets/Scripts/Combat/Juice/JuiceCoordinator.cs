using UnityEngine;

/// <summary>
/// Thin fan-out coordinator (charter Step 6 approved approach) — the single per-frame driver
/// for the Juice engine's Tick-based components, matching PlayerRoot's established role for
/// player components. Subscribes to EventBus.EntityDamaged/ParryExecuted (the charter's own
/// "Signal Up" decoupling point) and fans out to HitStopCoordinator/CameraTrauma; HitFlash and
/// SparkPool are called directly by WeaponHitbox instead (Step 6 Ruling 2 — same-frame,
/// same-object-graph VFX calls don't need the EventBus channel). Kept genuinely thin, no
/// logic of its own beyond the fan-out and per-frame Tick calls.
/// </summary>
public sealed class JuiceCoordinator : MonoBehaviour
{
    [SerializeField] private HitStopCoordinator hitStopCoordinator;
    [SerializeField] private CameraTrauma cameraTrauma;
    [SerializeField] private HitFlash hitFlash;
    [SerializeField] private TrailActivator trailActivator;

    private const float NormalHitTrauma = 0.3f;
    private const float ParryTrauma = 0.5f;

    private void OnEnable()
    {
        EventBus.EntityDamaged += HandleEntityDamaged;
        EventBus.ParryExecuted += HandleParryExecuted;
    }

    private void OnDisable()
    {
        EventBus.EntityDamaged -= HandleEntityDamaged;
        EventBus.ParryExecuted -= HandleParryExecuted;
    }

    private void HandleEntityDamaged(Transform target, float amount, bool isCritical)
    {
        hitStopCoordinator?.Request(0.045f);
        cameraTrauma?.AddTrauma(NormalHitTrauma);
    }

    private void HandleParryExecuted(Transform attacker, Transform defender)
    {
        hitStopCoordinator?.Request(0.045f);
        cameraTrauma?.AddTrauma(ParryTrauma);
    }

    private void Update()
    {
        hitStopCoordinator?.Tick(Time.unscaledDeltaTime);
        cameraTrauma?.Tick(Time.deltaTime);
        hitFlash?.Tick(Time.deltaTime);
        trailActivator?.Tick(Time.deltaTime);
    }
}
