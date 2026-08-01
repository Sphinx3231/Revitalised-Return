using UnityEngine;

/// <summary>
/// Multi-sensory perception engine (charter 7.1). Owns ONLY sensing — vision cone + raycast
/// line-of-sight, and an acoustic detection sphere keyed off the player's existing
/// DodgeAbility.IsDodging / AttackController.IsAttacking flags (no sprint mechanic exists in
/// this project yet — charter 14's "sprinting" trigger is explicitly not implemented here;
/// see the Step 7 task log's logged gap). Does NOT decide FSM transitions itself
/// (EnemyBrain's job) — only exposes CanSeePlayer/HeardNoise/LastKnownPlayerPosition as
/// read-only state for EnemyBrain to react to.
///
/// Explicit-Tick pattern (no Update()), matching every other component in this codebase.
/// Perception checks run at most once per 0.1s (charter 7.1's locked interval), staggered by
/// a per-instance _perceptionOffset (charter 14's anti-synchronized-spike-frame note) rather
/// than every enemy sampling on the exact same frame.
///
/// Research finding (verified live, not assumed): Physics.autoSyncTransforms is false in
/// EditMode, so a raycast against a just-moved transform silently returns nothing unless
/// Physics.SyncTransforms() runs first — called immediately before every Physics.Raycast
/// below. Physics.queriesHitTriggers defaults true, so the LOS raycast explicitly passes
/// QueryTriggerInteraction.Ignore or the player's/enemy's own trigger hurtboxes would
/// register as false obstructions.
/// </summary>
public sealed class EnemyPerception : MonoBehaviour
{
    private const float TickInterval = 0.1f;

    [SerializeField] private float visionRange = 18.0f;
    [SerializeField] private float visionHalfAngleDegrees = 45.0f;
    [SerializeField] private float soundRadius = 8.0f;

    /// <summary>Raycast origin for line-of-sight checks. Falls back to transform.position if unassigned.</summary>
    [SerializeField] private Transform eyePoint;

    /// <summary>
    /// Layers that count as "blocking sight" (and, since it's also the layer the player's
    /// own CharacterController collider sits on in this project's layer layout, doubles as
    /// the "is this hit actually the player" resolution layer) — excludes the
    /// Player/EnemyHitbox/Hurtbox trigger layers per charter 7.1.
    /// </summary>
    [SerializeField] private LayerMask obstructionMask;

    [SerializeField] private Transform playerTransform;

    /// <summary>Acoustic trigger reference: player is "loud" while dodging (charter 7.1's sprint-trigger gap noted above).</summary>
    [SerializeField] private DodgeAbility playerDodgeAbility;

    /// <summary>Acoustic trigger reference: player is "loud" while swinging a weapon.</summary>
    [SerializeField] private AttackController playerAttackController;

    private float _perceptionOffset;
    private float _accumulator;

    public bool CanSeePlayer { get; private set; }
    public bool HeardNoise { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private void Awake()
    {
        // charter 14: _perceptionOffset = Random.value * 0.1f, staggers this instance's
        // first (and every subsequent) check away from other enemies' identical intervals.
        _perceptionOffset = Random.value * TickInterval;
        _accumulator = -_perceptionOffset;
    }

    /// <summary>
    /// Advances the perception accumulator by one explicit tick. Runs the actual
    /// vision/sound evaluation at most once per 0.1s. Called by EnemyBrain.Tick(), not on
    /// its own Update().
    /// </summary>
    public void Tick(float deltaTime)
    {
        _accumulator += deltaTime;

        if (_accumulator < TickInterval)
            return;

        _accumulator -= TickInterval;
        EvaluatePerception();
    }

    /// <summary>
    /// Runs one immediate vision-cone/raycast + acoustic-sphere evaluation, bypassing the
    /// 0.1s tick gating. Separated out from Tick() so EditMode tests can exercise the sensing
    /// math deterministically without depending on Awake()'s randomized stagger offset.
    /// </summary>
    private void EvaluatePerception()
    {
        bool canSee = EvaluateVision();
        bool heard = EvaluateSound();

        CanSeePlayer = canSee;
        HeardNoise = heard;

        if (playerTransform != null && (canSee || heard))
        {
            LastKnownPlayerPosition = playerTransform.position;
        }
    }

    private bool EvaluateVision()
    {
        if (playerTransform == null)
            return false;

        Vector3 eyePos = eyePoint != null ? eyePoint.position : transform.position;
        Vector3 toPlayer = playerTransform.position - eyePos;
        float distance = toPlayer.magnitude;

        if (distance > visionRange || distance <= 0.0001f)
            return false;

        Vector3 directionToPlayer = toPlayer / distance;
        float cosHalfAngle = Mathf.Cos(visionHalfAngleDegrees * Mathf.Deg2Rad);
        float dot = Vector3.Dot(transform.forward, directionToPlayer);

        if (dot < cosHalfAngle)
            return false;

        // Research finding: EditMode does not auto-sync transforms, so a query against a
        // just-moved transform (this frame's patrol/turn motion) can silently miss without
        // this call.
        Physics.SyncTransforms();

        if (Physics.Raycast(eyePos, directionToPlayer, out RaycastHit hit, visionRange, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform);
        }

        // Nothing hit at all on the obstruction mask (which the player's own collider sits
        // on in this project's layer layout) within range — no line of sight to report.
        return false;
    }

    private bool EvaluateSound()
    {
        if (playerTransform == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > soundRadius)
            return false;

        bool dodging = playerDodgeAbility != null && playerDodgeAbility.IsDodging;
        bool attacking = playerAttackController != null && playerAttackController.IsAttacking;
        return dodging || attacking;
    }
}
