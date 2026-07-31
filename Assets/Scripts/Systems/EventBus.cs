using System;
using UnityEngine;

// Static pub/sub messenger decoupling systems (e.g. UI reacting to damage without a direct
// reference to the Player/Enemy GameObject). See CLAUDE.md Step 2.1.
public static class EventBus
{
    // Player & Vital Events
    public static event Action<float, float> PlayerHealthChanged;   // current, max
    public static event Action<float, float> PlayerStaminaChanged;  // current, max
    public static event Action<float, float> PlayerPostureChanged;  // current, max
    public static event Action<StanceData> StanceSwapped;
    public static event Action PlayerDied;

    // Combat & Damage Events
    public static event Action<Transform, float, bool> EntityDamaged;   // target, amount, isCritical
    public static event Action<Transform> PostureBroken;                // target
    public static event Action<Transform, Transform> ParryExecuted;     // attacker, defender
    public static event Action<Transform, int> EnemyKilled;             // enemy, expReward

    // World & UI Events
    public static event Action<string, int> QuestStateUpdated;   // questId, state
    public static event Action<Transform> InteractionTriggered;
    public static event Action<string, float> ShowNotice;        // text, duration

    // Raise-helper methods: one per event, wrapping the null-conditional invoke so calling
    // code never repeats that pattern.

    // Player & Vital Events
    public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);
    public static void RaisePlayerStaminaChanged(float current, float max) => PlayerStaminaChanged?.Invoke(current, max);
    public static void RaisePlayerPostureChanged(float current, float max) => PlayerPostureChanged?.Invoke(current, max);
    public static void RaiseStanceSwapped(StanceData stance) => StanceSwapped?.Invoke(stance);
    public static void RaisePlayerDied() => PlayerDied?.Invoke();

    // Combat & Damage Events
    public static void RaiseEntityDamaged(Transform target, float amount, bool isCritical) => EntityDamaged?.Invoke(target, amount, isCritical);
    public static void RaisePostureBroken(Transform target) => PostureBroken?.Invoke(target);
    public static void RaiseParryExecuted(Transform attacker, Transform defender) => ParryExecuted?.Invoke(attacker, defender);
    public static void RaiseEnemyKilled(Transform enemy, int expReward) => EnemyKilled?.Invoke(enemy, expReward);

    // World & UI Events
    public static void RaiseQuestStateUpdated(string questId, int state) => QuestStateUpdated?.Invoke(questId, state);
    public static void RaiseInteractionTriggered(Transform target) => InteractionTriggered?.Invoke(target);
    public static void RaiseShowNotice(string text, float duration) => ShowNotice?.Invoke(text, duration);
}
