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

    private void Awake()
    {
        _currentHealth = maxHealth;
        _currentPosture = maxHealth;
    }

    public void ApplyDamage(float amount, bool isCritical)
    {
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        Debug.Log($"Dummy took {amount} damage, {_currentHealth}/{maxHealth} HP remaining");

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
