using UnityEngine;

/// <summary>
/// Enemy FSM orchestrator (charter 7.2). Single enum + switch-based explicit Tick, matching
/// GameState.cs's own house style (Research explicitly rejected a full per-state-class State
/// pattern as over-engineering for this closed, fully-specified 5-state graph with exactly
/// one enemy type). Owns the State enum and TransitionTo entry logic; reads EnemyPerception's
/// exposed read-only state to decide transitions; reuses AttackController/WeaponHitbox
/// unmodified for the Attack state (Research: bespoke enemy attack logic would silently break
/// the parry resolution this task exists to make manually verifiable).
///
/// No own Update() — this is the SINGLE per-frame driver for the enemy, matching PlayerRoot's
/// role on the player side: it ticks perception, runs this frame's state logic, then ticks
/// the motor, in that explicit order every call.
/// </summary>
public sealed class EnemyBrain : MonoBehaviour
{
    public enum State { Patrol, Investigate, Telegraph, Attack, Recovery }

    // Timing constants (charter 7.2's locked ranges).
    private const float PatrolSpeedScale = 0.5f;
    private const float ChaseSpeedScale = 1.0f;
    private const float InvestigateLookAroundDuration = 3.0f;
    private const float TelegraphDuration = 0.45f; // within 0.3s-0.6s
    private const float RecoveryDuration = 1.0f; // within 0.8s-1.5s
    private const float EngagementRange = 3.0f; // reused "reasonable melee range"
    private const float ArriveThreshold = 0.5f;
    private const float TelegraphTurnSpeed = 8.0f; // Slerp rate, degrees-scale via Quaternion.Slerp t

    [SerializeField] private Transform[] waypoints;
    [SerializeField] private EnemyMotor motor;
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private AttackController attackController;
    [SerializeField] private Transform playerTransform;

    private int _currentWaypointIndex;
    private float _investigateTimer;
    private float _telegraphTimer;
    private float _recoveryTimer;

    public State CurrentState { get; private set; } = State.Patrol;

    private void TransitionTo(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Patrol:
                break;

            case State.Investigate:
                _investigateTimer = InvestigateLookAroundDuration;
                break;

            case State.Telegraph:
                motor?.SetDesiredDirection(Vector3.zero);
                _telegraphTimer = TelegraphDuration;
                break;

            case State.Attack:
                motor?.SetDesiredDirection(Vector3.zero);
                attackController?.TryLightAttack();
                break;

            case State.Recovery:
                _recoveryTimer = RecoveryDuration;
                break;
        }
    }

    /// <summary>
    /// Advances the enemy by one explicit tick: perception first, then this frame's active
    /// state's logic, then the motor. Called externally (scene driver) — no own Update().
    /// </summary>
    public void Tick(float deltaTime)
    {
        perception?.Tick(deltaTime);

        switch (CurrentState)
        {
            case State.Patrol:
                TickPatrol(deltaTime);
                break;
            case State.Investigate:
                TickInvestigate(deltaTime);
                break;
            case State.Telegraph:
                TickTelegraph(deltaTime);
                break;
            case State.Attack:
                TickAttackState(deltaTime);
                break;
            case State.Recovery:
                TickRecovery(deltaTime);
                break;
        }

        motor?.TickMotor(deltaTime);
    }

    private void TickPatrol(float deltaTime)
    {
        if (motor != null)
        {
            motor.SpeedScale = PatrolSpeedScale;
        }

        if (waypoints != null && waypoints.Length > 0)
        {
            Transform target = waypoints[_currentWaypointIndex];
            if (target != null)
            {
                Vector3 toTarget = target.position - transform.position;
                toTarget.y = 0f;

                if (toTarget.magnitude <= ArriveThreshold)
                {
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
                    motor?.SetDesiredDirection(Vector3.zero);
                }
                else
                {
                    motor?.SetDesiredDirection(toTarget.normalized);
                }
            }
        }
        else
        {
            motor?.SetDesiredDirection(Vector3.zero);
        }

        if (perception != null && (perception.CanSeePlayer || perception.HeardNoise))
        {
            TransitionTo(State.Investigate);
        }
    }

    private void TickInvestigate(float deltaTime)
    {
        if (motor != null)
        {
            motor.SpeedScale = ChaseSpeedScale;
        }

        if (perception == null)
        {
            motor?.SetDesiredDirection(Vector3.zero);
        }
        else
        {
            Vector3 toLastKnown = perception.LastKnownPlayerPosition - transform.position;
            toLastKnown.y = 0f;
            float distanceToLastKnown = toLastKnown.magnitude;

            if (distanceToLastKnown <= ArriveThreshold)
            {
                motor?.SetDesiredDirection(Vector3.zero);
                _investigateTimer -= deltaTime;
            }
            else
            {
                motor?.SetDesiredDirection(toLastKnown.normalized);
            }

            if (perception.CanSeePlayer && playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer <= EngagementRange)
                {
                    TransitionTo(State.Telegraph);
                    return;
                }
            }
        }

        if (_investigateTimer <= 0f)
        {
            TransitionTo(State.Patrol);
        }
    }

    private void TickTelegraph(float deltaTime)
    {
        motor?.SetDesiredDirection(Vector3.zero);

        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, TelegraphTurnSpeed * deltaTime);
            }
        }

        _telegraphTimer -= deltaTime;
        if (_telegraphTimer <= 0f)
        {
            TransitionTo(State.Attack);
        }
    }

    private void TickAttackState(float deltaTime)
    {
        motor?.SetDesiredDirection(Vector3.zero);

        if (attackController == null)
        {
            TransitionTo(State.Recovery);
            return;
        }

        attackController.TickAttack(deltaTime);

        if (!attackController.IsAttacking)
        {
            TransitionTo(State.Recovery);
        }
    }

    private void TickRecovery(float deltaTime)
    {
        // Small backstep away from the player (charter 7.2's "circling or backstepping" —
        // kept to a simple backstep per Approach's "don't over-build" note).
        if (playerTransform != null)
        {
            Vector3 away = transform.position - playerTransform.position;
            away.y = 0f;
            motor?.SetDesiredDirection(away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.zero);
        }
        else
        {
            motor?.SetDesiredDirection(Vector3.zero);
        }

        _recoveryTimer -= deltaTime;
        if (_recoveryTimer <= 0f)
        {
            // charter 7.2's own diagram: Recovery -> Investigate ("Animation End"), not
            // straight back to Attack, so perception gets a fresh chance to re-decide.
            TransitionTo(State.Investigate);
        }
    }
}
