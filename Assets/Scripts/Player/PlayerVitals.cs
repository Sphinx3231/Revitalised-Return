using UnityEngine;

/// <summary>
/// Single-responsibility owner of the player's health/stamina/posture state (charter
/// S.O.L.I.D. split). Fires the EventBus vitals events the Phase 2 HUD already subscribes
/// to. Does NOT implement its own Update() — PlayerRoot calls TickRegen(deltaTime)
/// explicitly, per its documented single-orchestrator tick order. Health/posture have no
/// drain source yet this slice, so only stamina regenerates.
/// </summary>
public sealed class PlayerVitals : MonoBehaviour, IStaminaSource
{
    private const float StaminaRegenRate = 10.0f;
    private const float StaminaRegenPause = 1.2f;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float maxPosture = 100f;

    private float _currentHealth;
    private float _currentStamina;
    private float _currentPosture;
    private float _regenPauseTimer;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _currentStamina = maxStamina;
        _currentPosture = maxPosture;
    }

    private void Start()
    {
        EventBus.RaisePlayerHealthChanged(_currentHealth, maxHealth);
        EventBus.RaisePlayerStaminaChanged(_currentStamina, maxStamina);
        EventBus.RaisePlayerPostureChanged(_currentPosture, maxPosture);
    }

    public bool TrySpend(float amount)
    {
        if (_currentStamina < amount)
            return false;

        _currentStamina -= amount;
        EventBus.RaisePlayerStaminaChanged(_currentStamina, maxStamina);
        _regenPauseTimer = StaminaRegenPause;
        return true;
    }

    /// <summary>
    /// Advances stamina regen by one explicit tick. Called by PlayerRoot's Update(), not
    /// driven by this component's own Update().
    /// </summary>
    public void TickRegen(float deltaTime)
    {
        if (_regenPauseTimer > 0f)
        {
            _regenPauseTimer -= deltaTime;
            return;
        }

        if (_currentStamina >= maxStamina)
            return;

        float previous = _currentStamina;
        _currentStamina = Mathf.Min(maxStamina, _currentStamina + StaminaRegenRate * deltaTime);

        if (!Mathf.Approximately(previous, _currentStamina))
        {
            EventBus.RaisePlayerStaminaChanged(_currentStamina, maxStamina);
        }
    }
}
