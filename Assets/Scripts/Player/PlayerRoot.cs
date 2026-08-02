using UnityEngine;

/// <summary>
/// Thin orchestrator (charter S.O.L.I.D. split). Gates on GameState.IsPlayerInputLocked(),
/// then this is the SINGLE actual per-frame driver for the player: it calls each
/// component's explicit Tick method in order — look tick -> input read -> buffer
/// consume/dodge trigger check -> dodge tick -> motor tick -> vitals regen -> lean tick ->
/// attack consume/tick -> parry consume/tick — rather than relying on Unity's Script
/// Execution Order settings. PlayerMotor, DodgeAbility, PlayerVitals, MeshLean, PlayerLook,
/// AttackController, and ParryController do NOT implement their own Update() for this
/// reason; they are purely driven from here, so there is no risk of a component reading
/// input/state that is a frame stale. Stance switching (StanceController) is invoked
/// directly from the input events instead, since it's a discrete action, not continuous
/// per-frame state. Attack and parry are independent state machines this task (no animation
/// layer yet to make mutual exclusion with dodge visible/meaningful) — order between them
/// doesn't matter much, kept consistent here for documentation purposes.
///
/// PlayerLook is ticked FIRST (charter first-person camera pivot, 2026-08-02) so the root's
/// yaw for this frame is set before CameraRelativeInput derives a movement direction and
/// before Cinemachine's own LateUpdate runs later this frame — see PlayerLook.cs.
/// </summary>
public sealed class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private DodgeAbility dodgeAbility;
    [SerializeField] private MeshLean meshLean;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private StanceController stanceController;
    [SerializeField] private AttackController attackController;
    [SerializeField] private ParryController parryController;
    [SerializeField] private KnockbackAbility knockbackAbility;
    [SerializeField] private InteractionResolver interactionResolver;

    /// <summary>
    /// Internal reads of raw movement/look input go through these interfaces (Dependency
    /// Inversion), not the concrete PlayerInputReader type — the serialized
    /// PlayerInputReader field above exists only so Unity's Inspector can wire the
    /// concrete MonoBehaviour, since interfaces aren't directly serializable.
    /// </summary>
    private IMovementInput _movementInput;
    private ILookInput _lookInput;

    private Vector3 _lastNonZeroDirection = Vector3.forward;

    private void Awake()
    {
        _movementInput = inputReader;
        _lookInput = inputReader;

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

        // 1. Look tick (charter first-person camera pivot, 2026-08-02): applies this
        // frame's yaw directly to the root transform BEFORE the camera-relative direction
        // is derived below, so Cinemachine's later LateUpdate (which reads the EyeSocket
        // child's transform) sees an already-current-frame yaw. See PlayerLook's own doc
        // comment for why this ordering avoids a feedback-loop/1-frame-lag risk.
        if (playerLook != null && _lookInput != null)
        {
            playerLook.Tick(_lookInput.LookRaw);
        }

        // 2. Read raw input and transform to a camera-relative, flattened direction.
        Vector2 moveRaw = _movementInput.MoveRaw;
        Vector3 direction = CameraRelativeInput.ToCameraRelative(moveRaw, cameraTransform);

        motor.SetDesiredDirection(direction);

        if (direction != Vector3.zero)
        {
            _lastNonZeroDirection = direction;
        }

        // 3. Consume a buffered dodge action, if any, and trigger the dodge.
        if (dodgeAbility != null && inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.Dodge, Time.time))
        {
            Vector3 dodgeDirection = direction != Vector3.zero ? direction : _lastNonZeroDirection;
            dodgeAbility.TryDodge(dodgeDirection);
        }

        // 4. Dodge tick (may set/clear the motor's VelocityOverride for this frame).
        if (dodgeAbility != null)
        {
            dodgeAbility.TickDodge(deltaTime);
        }

        // 4b. Knockback tick (Step 8: externally triggered by BossPhaseController, not by
        // player input — ticked here, after dodge and before the motor tick, so its
        // VelocityOverride write (if active) is fresh for this frame's TickMotor call, same
        // ordering reasoning as DodgeAbility's tick placement above).
        if (knockbackAbility != null)
        {
            knockbackAbility.TickKnockback(deltaTime);
        }

        // 5. Motor tick (consumes VelocityOverride if set, else the lerped kinematics).
        motor.TickMotor(deltaTime);

        // 6. Vitals regen tick (stamina regen after the locked pause window).
        if (vitals != null)
        {
            vitals.TickRegen(deltaTime);
        }

        // 7. Lean tick (reads the motor's freshly-updated HorizontalVelocity).
        if (meshLean != null)
        {
            meshLean.TickLean(deltaTime);
        }

        // 8. Consume buffered light/heavy attack actions and trigger the attack.
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

        // 9. Consume a buffered parry action and tick the parry window.
        if (parryController != null)
        {
            if (inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.Parry, Time.time))
            {
                parryController.TryParry();
            }

            parryController.TickParry(deltaTime);
        }

        // 10. Interaction resolver tick (re-ranks candidates every frame -- the camera-dot
        // term changes continuously) followed by consuming a buffered Interact action. The
        // consume-and-act call site lives here (not inside InteractionResolver itself) so
        // there is exactly ONE place that decides when a buffered Interact actually fires,
        // matching the LightAttack/HeavyAttack/Parry/Dodge pattern above.
        if (interactionResolver != null)
        {
            interactionResolver.Tick(deltaTime);

            if (inputReader.InputBuffer.TryConsume(InputBuffer.BufferedAction.Interact, Time.time) && interactionResolver.CurrentCandidate != null)
            {
                interactionResolver.CurrentCandidate.Interact(transform);
            }
        }
    }
}
