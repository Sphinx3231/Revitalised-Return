using UnityEngine;

/// <summary>
/// Attack timing orchestrator (charter S.O.L.I.D. split, mirrors DodgeAbility's explicit-tick
/// pattern). Owns attack-input consumption (via TryLightAttack/TryHeavyAttack, called by
/// PlayerRoot on buffered-action consumption) and hitbox open/close timing. Does NOT own
/// damage math (WeaponHitbox's job) or attack input reading (PlayerRoot's job). No own
/// Update() — PlayerRoot calls TickAttack(deltaTime) explicitly, per its documented
/// single-orchestrator tick order.
///
/// TODO(Step 13): lightAttackWindowSeconds/heavyAttackWindowSeconds are a timed-window
/// placeholder standing in for real Animation-Event-driven hitbox timing (charter 5.2), since
/// no attack animations/Animator exist yet.
/// </summary>
public sealed class AttackController : MonoBehaviour
{
    [SerializeField] private WeaponHitbox weaponHitbox;

    /// <summary>
    /// Serialized as MonoBehaviour rather than the concrete StanceController (Step 8 addendum
    /// to the original DIP field-seam pattern): this component is reused unmodified on both
    /// the player (StanceController) and a boss (BossStanceMirror) — two different concrete
    /// MonoBehaviour types that both implement IStanceSource — so a StanceController-typed
    /// field could not accept a BossStanceMirror reference in the Inspector. Cached to
    /// _stanceSource in Awake(), same seam pattern PlayerRoot uses for its serialized
    /// PlayerInputReader field vs. its private IMovementInput _movementInput.
    /// </summary>
    [SerializeField] private MonoBehaviour stanceController;

    [SerializeField] private float lightAttackWindowSeconds = 0.2f;
    [SerializeField] private float heavyAttackWindowSeconds = 0.35f;
    [SerializeField] private float lightAttackBaseDamage = 10f;
    [SerializeField] private float heavyAttackBaseDamage = 18f;

    private IStanceSource _stanceSource;

    private bool _isAttacking;
    private float _windowRemaining;

    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        _stanceSource = stanceController as IStanceSource;
    }

    public void TryLightAttack()
    {
        TryAttack(lightAttackBaseDamage, lightAttackWindowSeconds);
    }

    public void TryHeavyAttack()
    {
        TryAttack(heavyAttackBaseDamage, heavyAttackWindowSeconds);
    }

    // No attack-canceling this task — a light/heavy attempt while already attacking is a
    // silent no-op. TODO(Step 13): real animation-cancel windows will refine this once
    // combat animations exist to make partial-cancel timing visible/meaningful.
    private void TryAttack(float damage, float windowSeconds)
    {
        if (_isAttacking)
            return;

        if (weaponHitbox == null)
            return;

        _isAttacking = true;

        weaponHitbox.CurrentStance = _stanceSource != null ? _stanceSource.CurrentStance : null;
        weaponHitbox.BaseDamage = damage;
        weaponHitbox.ResetHitTracking();
        weaponHitbox.GetComponent<Collider>().enabled = true;

        float attackSpeedScalar = weaponHitbox.CurrentStance != null ? weaponHitbox.CurrentStance.attackSpeedScalar : 1f;
        if (attackSpeedScalar <= 0f)
        {
            attackSpeedScalar = 1f;
        }

        _windowRemaining = windowSeconds / attackSpeedScalar;
    }

    /// <summary>
    /// Advances the active attack window (if any) by one explicit tick. No-op when not
    /// currently attacking.
    /// </summary>
    public void TickAttack(float deltaTime)
    {
        if (!_isAttacking)
            return;

        _windowRemaining -= deltaTime;

        if (_windowRemaining <= 0f)
        {
            CloseHitbox();
        }
    }

    /// <summary>
    /// Force-closes the active attack early. Called by a parrying defender's WeaponHitbox
    /// resolution (charter 5.2's "attacker gets interrupted" half of the parry check).
    /// </summary>
    public void TryInterrupt()
    {
        if (!_isAttacking)
            return;

        CloseHitbox();
    }

    private void CloseHitbox()
    {
        if (weaponHitbox != null)
        {
            weaponHitbox.GetComponent<Collider>().enabled = false;
        }

        _isAttacking = false;
    }
}
