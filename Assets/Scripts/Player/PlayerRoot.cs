using UnityEngine;

/// <summary>
/// Thin orchestrator (charter S.O.L.I.D. split). Gates on GameState.IsPlayerInputLocked(),
/// then this is the SINGLE actual per-frame driver for the player: it calls each
/// component's explicit Tick method in order — input read -> buffer consume/dodge
/// trigger check -> dodge tick -> motor tick -> vitals regen -> lean tick -> attack
/// consume/tick -> parry consume/tick — rather than relying on Unity's Script Execution
/// Order settings. PlayerMotor, DodgeAbility, PlayerVitals, MeshLean, AttackController, and
/// ParryController do NOT implement their own Update() for this reason; they are purely
/// driven from here, so there is no risk of a component reading input/state that is a frame
/// stale. Stance switching (StanceController) is invoked directly from the input events
/// instead, since it's a discrete action, not continuous per-frame state. Attack and parry
/// are independent state machines this task (no animation layer yet to make mutual
/// exclusion with dodge visible/meaningful) — order between them doesn't matter much, kept
/// consistent here for documentation purposes.
/// </summary>
public sealed class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private DodgeAbility dodgeAbility;
    [SerializeField] private MeshLean meshLean;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private StanceController stanceController;
    [SerializeField] private AttackController attackController;
    [SerializeField] private ParryController parryController;

    /// <summary>
    /// Internal reads of raw movement input go through this interface (Dependency
    /// Inversion), not the concrete PlayerInputReader type — the serialized
    /// PlayerInputReader field above exists only so Unity's Inspector can wire the
    /// concrete MonoBehaviour, since interfaces aren't directly serializable.
    /// </summary>
    private IMovementInput _movementInput;

    private Vector3 _lastNonZeroDirection = Vector3.forward;

    private void Awake()
    {
        _movementInput = inputReader;

        if (inputReader != null)
        {
            inputReader.StanceNextPressed += HandleStanceNext;
            inputReader.StancePrevPressed += HandleStancePrev;
        }
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.StanceNextPressed -= HandleStanceNext;
            inputReader.StancePrevPressed -= HandleStancePrev;
        }
    }

    private void HandleStanceNext()
    {
        stanceController?.CycleNext();
    }

    private void HandleStancePrev()
    {
        stanceController?.CyclePrevious();
    }

    private void Update()
    {
        if (GameState.IsPlayerInputLocked())
            return;

        if (inputReader == null || motor == null || cameraTransform == null || _movementInput == null)
            return;

        float deltaTime = Time.deltaTime;

        // 1. Read raw input and transform to a camera-relative, flattened direction.
        Vector2 moveRaw = _movementInput.MoveRaw;
        Vector3 direction = CameraRelativeInput.ToCameraRelative(moveRaw, cameraTransform);

        motor.SetDesiredDirection(direction);

        if (direction != Vector3.zero)
        {
            _lastNonZeroDirection = direction;
        }

        // 2. Consume a buffered dodge action, if any, and trigger the dodge.
        if (dodgeAbility != null && inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.Dodge, Time.time))
        {
            Vector3 dodgeDirection = direction != Vector3.zero ? direction : _lastNonZeroDirection;
            dodgeAbility.TryDodge(dodgeDirection);
        }

        // 3. Dodge tick (may set/clear the motor's VelocityOverride for this frame).
        if (dodgeAbility != null)
        {
            dodgeAbility.TickDodge(deltaTime);
        }

        // 4. Motor tick (consumes VelocityOverride if set, else the lerped kinematics).
        motor.TickMotor(deltaTime);

        // 5. Vitals regen tick (stamina regen after the locked pause window).
        if (vitals != null)
        {
            vitals.TickRegen(deltaTime);
        }

        // 6. Lean tick (reads the motor's freshly-updated HorizontalVelocity).
        if (meshLean != null)
        {
            meshLean.TickLean(deltaTime);
        }

        // 7. Consume buffered light/heavy attack actions and trigger the attack.
        if (attackController != null)
        {
            if (inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.LightAttack, Time.time))
            {
                attackController.TryLightAttack();
            }

            if (inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.HeavyAttack, Time.time))
            {
                attackController.TryHeavyAttack();
            }

            attackController.TickAttack(deltaTime);
        }

        // 8. Consume a buffered parry action and tick the parry window.
        if (parryController != null)
        {
            if (inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.Parry, Time.time))
            {
                parryController.TryParry();
            }

            parryController.TickParry(deltaTime);
        }
    }
}
