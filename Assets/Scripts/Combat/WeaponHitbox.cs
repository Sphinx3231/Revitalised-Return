using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger-Collider hit resolver (charter 5.2). Only resolves overlap via IDamageable/
/// ParryController — does not own attack timing (AttackController's job) or attack input
/// (PlayerRoot's job). Expects a Collider (BoxCollider) already present on this GameObject,
/// isTrigger=true, starting enabled=false; AttackController toggles Collider.enabled to
/// open/close the active window (never GameObject.SetActive, per charter 14).
///
/// Resolution order per charter 5.2 (parry -> block -> hit):
///   1. Parry check: if the struck target is currently parrying, the ATTACKER (this
///      hitbox's owner) is interrupted and takes 40% of baseDamage as posture damage. No
///      damage is applied to the defender. EventBus.ParryExecuted fires.
///   2. Block check: if the target is blocking (and didn't parry), damage is reduced 80%
///      but posture damage bleeds through unreduced (charter 5.2's explicit rule).
///   3. Hit check: normal ApplyDamage/ApplyPostureDamage + EventBus.EntityDamaged.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private float baseDamage = 10f;

    /// <summary>Assigned externally by AttackController before each hitbox opening.</summary>
    public StanceData CurrentStance { get; set; }

    /// <summary>
    /// baseDamage stays [SerializeField] for an Inspector default; AttackController
    /// reassigns it per-attack (light vs heavy) via this settable property before opening.
    /// </summary>
    public float BaseDamage { get => baseDamage; set => baseDamage = value; }

    private readonly HashSet<Collider> _alreadyHit = new HashSet<Collider>();

    /// <summary>Clears the per-activation hit-tracking set. Call when the hitbox opens.</summary>
    public void ResetHitTracking()
    {
        _alreadyHit.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_alreadyHit.Contains(other))
            return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null)
        {
            target = other.GetComponentInParent<IDamageable>();
        }

        if (target == null)
            return;

        _alreadyHit.Add(other);

        float stanceBaseMult = CurrentStance != null ? CurrentStance.baseDamageMultiplier : 1f;
        float stancePostureMult = CurrentStance != null ? CurrentStance.postureDamageMultiplier : 1f;

        // Damage = (Base + Weapon) x (1 - Armor/(Armor+100)); Armor=0 for both sides this
        // task, written as an explicit no-op term (charter 2, brief constraint), not
        // hardcoded away, so armor can be wired in later without touching this math.
        const float armor = 0f;
        float mitigation = 1f - (armor / (armor + 100f));

        float damage = baseDamage * stanceBaseMult * mitigation;
        float postureDamage = baseDamage * stancePostureMult;

        // 1. Parry check.
        ParryController defenderParry = other.GetComponent<ParryController>();
        if (defenderParry == null)
        {
            defenderParry = other.GetComponentInParent<ParryController>();
        }

        if (defenderParry != null && defenderParry.IsParrying)
        {
            IDamageable ownerDamageable = GetComponentInParent<IDamageable>();
            ownerDamageable?.ApplyPostureDamage(baseDamage * 0.4f);

            AttackController ownerAttack = GetComponentInParent<AttackController>();
            ownerAttack?.TryInterrupt();

            EventBus.RaiseParryExecuted(transform, target.DamageTransform);
            return;
        }

        // 2. Block check (only reached if not parried).
        if (defenderParry != null && defenderParry.IsBlocking)
        {
            damage *= 0.2f;
            // Posture damage bleeds through unreduced per charter 5.2 (postureDamage
            // is left untouched here on purpose).
        }

        // 3. Hit check.
        target.ApplyDamage(damage, false);
        target.ApplyPostureDamage(postureDamage);
        EventBus.RaiseEntityDamaged(target.DamageTransform, damage, false);
    }
}
