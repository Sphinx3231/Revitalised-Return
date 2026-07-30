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

    // TODO(Step 2): raise-helper methods / null-conditional invoke wrappers once real
    // systems start firing these events.
}
