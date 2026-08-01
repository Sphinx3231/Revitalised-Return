using UnityEngine;

/// <summary>
/// Owns boss phase-transition state (charter 8.1) via composition over DummyHealth's
/// HealthChanged event — never owns the health value itself (Research's explicit ruling,
/// since DummyHealth is sealed: composition, not inheritance). Latches the 50%-HP crossing
/// exactly once per encounter (guards against re-triggering on further damage once already in
/// Phase 2), then in order: grants temporary invincibility, fires an AoE knockback, activates
/// arena barrier GameObjects, tints the boss's weapon trail as a Phase-2 visual cue, and
/// engages BossStanceMirror for the enraged Phase 2 stance-mirroring behavior.
/// CameraTrauma.AddTrauma() supplies the "phase transition punch" (Research's flavor
/// recommendation, reused rather than reinvented).
///
/// No coroutines anywhere in this project's convention — the invincibility window is a
/// counted-down explicit Tick(deltaTime), called by EnemyRoot alongside its brain tick.
///
/// Scope note (explicitly logged, not silently omitted): a full "behavior-tree switch to an
/// enraged attack-timing profile" is NOT implemented here. The task constraints forbid
/// modifying EnemyBrain.cs's core FSM logic in this task ("only EnemyRoot wiring changes, no
/// FSM behavior changes"), so there is no existing seam on EnemyBrain/AttackController to
/// safely retarget a faster attack-timing preset without touching that FSM. The weapon-trail
/// tint below is this task's stand-in "Phase 2 profile switch" visual cue, consistent with the
/// charter's own "no boss-specific flaming blade particle trails... tint via startColor/
/// endColor" Phase-2 flavor ruling. A real attack-speed/combo-string enrage profile is deferred
/// to whichever future task is allowed to touch EnemyBrain's FSM.
/// </summary>
public sealed class BossPhaseController : MonoBehaviour
{
    [SerializeField] private DummyHealth bossHealth;
    [SerializeField] private EnemyBrain enemyBrain;
    [SerializeField] private CameraTrauma cameraTrauma;
    [SerializeField] private TrailActivator trailActivator;
    [SerializeField] private BossStanceMirror stanceMirror;
    [SerializeField] private GameObject[] arenaBarriers;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private KnockbackAbility playerKnockback;

    /// <summary>
    /// Optional (charter 8.2's camera midpoint tracking). When wired, StartEncounter() is
    /// called once from OnEnable per Approach's "keep it simple" scoping note (deliverable 10:
    /// "calling StartEncounter() once when the boss's OnEnable/encounter-start fires is
    /// sufficient" — no real encounter-trigger-Collider exists yet, Step 9 territory).
    /// </summary>
    [SerializeField] private BossCameraFraming bossCameraFraming;

    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float knockbackDuration = 0.4f;
    [SerializeField] private float knockbackRadius = 6f;
    [SerializeField] private float invincibilityDuration = 1.5f;
    [SerializeField] private Color enrageTrailColor = new Color(1f, 0.25f, 0.1f, 1f);

    private const float Phase2TraumaPunch = 0.5f;

    private bool _phase2Triggered;
    private bool _defeated;
    private float _invincibilityRemaining;

    /// <summary>True once the 50% HP crossing has fired Phase 2. Latches permanently — never resets.</summary>
    public bool Phase2Triggered => _phase2Triggered;

    /// <summary>True once the boss has been defeated (HP reached 0). Latches permanently — never resets.</summary>
    public bool Defeated => _defeated;

    /// <summary>True while the Phase-2 transition's temporary invincibility window is still counting down.</summary>
    public bool IsInvincibilityWindowActive => _invincibilityRemaining > 0f;

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.HealthChanged += HandleHealthChanged;
        }

        // No real encounter-trigger-collider exists yet (Step 9 territory) -- per Approach's
        // explicit "keep it simple" scoping note, this task activates arena barriers
        // immediately on enable to prove the sealing mechanism rather than building a real
        // boss-aggro trigger Collider.
        SetBarriersActive(true);

        bossCameraFraming?.StartEncounter();
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (max > 0f && !_phase2Triggered && current / max <= 0.5f)
        {
            _phase2Triggered = true;
            TriggerPhase2();
        }

        // Independent latch from the Phase-2 threshold above: a boss can be defeated in the
        // same HealthChanged event that also crosses 50% (e.g. a single hit taking it from
        // 100% straight to 0%), so this must not be an "else" of the Phase-2 check.
        if (!_defeated && current <= 0f)
        {
            _defeated = true;
            TriggerDefeat();
        }
    }

    private void TriggerPhase2()
    {
        if (bossHealth != null)
        {
            bossHealth.IsInvincible = true;
        }
        _invincibilityRemaining = invincibilityDuration;

        FireAoEKnockback();

        SetBarriersActive(true);

        ApplyEnrageTrailTint();

        cameraTrauma?.AddTrauma(Phase2TraumaPunch);

        stanceMirror?.SetActive(true);
    }

    /// <summary>
    /// Fires once when the boss's HP reaches 0. Unseals the arena barriers (charter Step 8 DoD:
    /// "defeating the boss unseals them") and reverts the camera to normal player-tracking via
    /// BossCameraFraming.EndEncounter(). Independent of the Phase-2 latch — see HandleHealthChanged.
    /// </summary>
    private void TriggerDefeat()
    {
        SetBarriersActive(false);

        bossCameraFraming?.EndEncounter();
    }

    private void FireAoEKnockback()
    {
        if (playerTransform == null || playerKnockback == null)
            return;

        // Research's Step 7-era finding applies again here: EditMode/just-moved transforms
        // need an explicit sync before any Physics query against them.
        Physics.SyncTransforms();

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > knockbackRadius)
            return;

        Vector3 away = playerTransform.position - transform.position;
        away.y = 0f;
        Vector3 direction = away.sqrMagnitude > 0.0001f ? away.normalized : playerTransform.forward;

        playerKnockback.ApplyKnockback(direction, knockbackForce, knockbackDuration);
    }

    private void ApplyEnrageTrailTint()
    {
        var trail = trailActivator != null ? trailActivator.TrailRenderer : null;
        if (trail == null)
            return;

        trail.startColor = enrageTrailColor;
        trail.endColor = enrageTrailColor;
    }

    private void SetBarriersActive(bool active)
    {
        if (arenaBarriers == null)
            return;

        foreach (var barrier in arenaBarriers)
        {
            if (barrier != null)
            {
                barrier.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Counts down the Phase-2 invincibility window, clearing bossHealth.IsInvincible when it
    /// expires. Called by EnemyRoot's Update() alongside its brain tick. No-op if the window
    /// isn't currently active (covers both "never triggered" and "already expired").
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_invincibilityRemaining <= 0f)
            return;

        _invincibilityRemaining -= deltaTime;
        if (_invincibilityRemaining <= 0f)
        {
            _invincibilityRemaining = 0f;
            if (bossHealth != null)
            {
                bossHealth.IsInvincible = false;
            }
        }
    }
}
