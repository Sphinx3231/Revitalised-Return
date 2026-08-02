using UnityEngine;
using RevitalisedReturn.Generated;

/// <summary>
/// MonoBehaviour adapter over the generated Input Actions class
/// (Assets/Settings/PlayerControls.inputactions). Owns the InputBuffer and feeds it
/// from the button actions' `performed` callbacks; exposes raw move input via
/// IMovementInput for PlayerRoot to transform into a camera-relative direction, and raw
/// look input (mouse delta / gamepad right-stick, charter first-person camera pivot --
/// docs/Tasks/2026-08-02-first-person-camera-and-weapon.md) via ILookInput for PlayerLook.
/// </summary>
public sealed class PlayerInputReader : MonoBehaviour, IMovementInput, ILookInput
{
    private PlayerControls _controls;
    private readonly InputBuffer _inputBuffer = new InputBuffer();

    public InputBuffer InputBuffer => _inputBuffer;

    public Vector2 MoveRaw => _controls != null ? _controls.Player.move.ReadValue<Vector2>() : Vector2.zero;

    public Vector2 LookRaw => _controls != null ? _controls.Player.look.ReadValue<Vector2>() : Vector2.zero;

    public event System.Action StanceNextPressed;
    public event System.Action StancePrevPressed;

    private void Awake()
    {
        _controls = new PlayerControls();

        _controls.Player.light_attack.performed += OnLightAttackPerformed;
        _controls.Player.heavy_attack.performed += OnHeavyAttackPerformed;
        _controls.Player.parry.performed += OnParryPerformed;
        _controls.Player.dodge.performed += OnDodgePerformed;
        _controls.Player.stance_next.performed += OnStanceNextPerformed;
        _controls.Player.stance_prev.performed += OnStancePrevPerformed;
        _controls.Player.interact.performed += OnInteractPerformed;
    }

    private void OnEnable()
    {
        _controls?.Enable();
    }

    private void OnDisable()
    {
        _controls?.Disable();
    }

    private void OnDestroy()
    {
        if (_controls == null)
            return;

        _controls.Player.light_attack.performed -= OnLightAttackPerformed;
        _controls.Player.heavy_attack.performed -= OnHeavyAttackPerformed;
        _controls.Player.parry.performed -= OnParryPerformed;
        _controls.Player.dodge.performed -= OnDodgePerformed;
        _controls.Player.stance_next.performed -= OnStanceNextPerformed;
        _controls.Player.stance_prev.performed -= OnStancePrevPerformed;
        _controls.Player.interact.performed -= OnInteractPerformed;

        _controls.Dispose();
        _controls = null;
    }

    private void OnLightAttackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        _inputBuffer.Push(InputBuffer.BufferedAction.LightAttack, Time.time);
    }

    private void OnHeavyAttackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        _inputBuffer.Push(InputBuffer.BufferedAction.HeavyAttack, Time.time);
    }

    private void OnParryPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        _inputBuffer.Push(InputBuffer.BufferedAction.Parry, Time.time);
    }

    private void OnDodgePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        _inputBuffer.Push(InputBuffer.BufferedAction.Dodge, Time.time);
    }

    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        _inputBuffer.Push(InputBuffer.BufferedAction.Interact, Time.time);
    }

    private void OnStanceNextPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        StanceNextPressed?.Invoke();
    }

    private void OnStancePrevPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (GameState.IsPlayerInputLocked())
            return;
        StancePrevPressed?.Invoke();
    }
}
