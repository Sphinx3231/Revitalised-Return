using UnityEngine;

/// <summary>
/// Dependency-inversion seam for anything that can take health/posture damage (charter
/// S.O.L.I.D. split, matching the existing IStaminaSource/IMovementInput pattern).
/// Implemented by PlayerVitals and DummyHealth so WeaponHitbox's hit-resolution code never
/// needs to know whether it hit the player or an enemy.
/// </summary>
public interface IDamageable
{
    void ApplyDamage(float amount, bool isCritical);
    void ApplyPostureDamage(float amount);
    Transform DamageTransform { get; }
}
