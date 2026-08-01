using UnityEngine;

/// <summary>
/// Minimal IDamageable for the passive training dummy (Step 5). No stance-multiplier
/// concerns of its own — multipliers are applied by the attacker's WeaponHitbox before
/// ApplyDamage/ApplyPostureDamage are called. "Visible health" is a Console log per the
/// task's explicit minimal-stub allowance (a full enemy HUD is Step 11 territory, player
/// only).
/// </summary>
public sealed class DummyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    private float _currentHealth;
    private float _currentPosture;

    /// <summary>
    /// Fires whenever ApplyDamage changes the health value (current, max). Boss-agnostic —
    /// added for Step 8's BossPhaseController (subscribes via composition to detect the 50%
    /// HP threshold crossing, charter 8.1) but also generically useful for a future Step 11
    /// enemy HUD.
    /// </summary>
    public event System.Action<float, float> HealthChanged;

    /// <summary>
    /// When true, ApplyDamage no-ops entirely (no health change, no HealthChanged event,
    /// existing Debug.Log calls skipped too) — the invincibility-window mechanism
    /// BossPhaseController sets during its Phase 2 transition (charter 8.1's "temporary
    /// invincibility" step). Defaults to false (normal damageable behavior).
    /// </summary>
    public bool IsInvincible { get; set; }

    /// <summary>Current health as a [0,1] fraction of maxHealth (0 if maxHealth is non-positive).</summary>
    public float HealthFraction => maxHealth > 0f ? _currentHealth / maxHealth : 0f;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _currentPosture = maxHealth;
    }

    public void ApplyDamage(float amount, bool isCritical)
    {
        if (IsInvincible)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        Debug.Log($"Dummy took {amount} damage, {_currentHealth}/{maxHealth} HP remaining");
        HealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
        {
            Debug.Log("Dummy defeated");
        }
    }

    public void ApplyPostureDamage(float amount)
    {
        _currentPosture = Mathf.Max(0f, _currentPosture - amount);
        Debug.Log($"Dummy took {amount} posture damage, {_currentPosture}/{maxHealth} posture remaining");
    }

    public Transform DamageTransform => transform;
}
