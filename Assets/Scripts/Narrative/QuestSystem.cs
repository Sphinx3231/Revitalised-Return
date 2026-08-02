using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure static functions over PlayerData.questStates (charter Step 12, Research finding 7) --
/// NOT instance objects. questStates is already the save-file source of truth via the
/// PlayerSaveDto layer; an instance-per-quest object would create a second source of truth
/// requiring hand-written save/load sync, the exact bug class Step 11's DTO layer exists to
/// prevent. Matches the already-proven CompassProjection/InteractionResolver.ScoreCandidate
/// static-pure-function shape.
///
/// TickOnRest is deliberately minimal/real for this task's one proof quest: it auto-advances
/// any Active quest whose advancementConditions are all met to ObjectiveComplete. Nothing
/// speculative (e.g. no auto-Completed transition, no failure-condition evaluation) is added
/// beyond what this task's DoD needs -- a real, tested seam future quest content plugs into,
/// not a fully-general quest-script interpreter.
///
/// Quest-catalogue access for the locked single-arg `TickOnRest(data)` call Shrine.Interact
/// makes: Shrine deliberately has no Quest-asset reference (Research/Approach ruling keeps it
/// from becoming a god-class), so `Quest` itself self-registers into a small static registry
/// via OnEnable/OnDisable -- the same "reach for a well-known access point" convention this
/// project already uses for GameState.Instance/SaveSystem.Current, applied to a ScriptableObject
/// instead of a MonoBehaviour singleton. Any Quest asset referenced (directly or transitively)
/// by a loaded scene is registered automatically the moment Unity loads it; EditMode tests get
/// the same behavior for free via ScriptableObject.CreateInstance (which also runs OnEnable),
/// see ClearRegistry() for the required test-isolation hook.
/// </summary>
public static class QuestSystem
{
    private static readonly List<Quest> _registeredQuests = new List<Quest>();

    /// <summary>Called from Quest.OnEnable -- adds `quest` to the registry TickOnRest(data) (single-arg) ticks against.</summary>
    public static void RegisterQuest(Quest quest)
    {
        if (quest != null && !_registeredQuests.Contains(quest))
            _registeredQuests.Add(quest);
    }

    /// <summary>Called from Quest.OnDisable -- removes `quest` from the registry.</summary>
    public static void UnregisterQuest(Quest quest)
    {
        _registeredQuests.Remove(quest);
    }

    /// <summary>Test-only isolation hook -- clears the static registry so one test's registered Quest instances can't leak into another (EditMode tests never run inside a real Player session where the registry would otherwise only ever grow via real scene loads).</summary>
    public static void ClearRegistry()
    {
        _registeredQuests.Clear();
    }
    /// <summary>
    /// Reads the stored int for `questId` and clamps it to Unstarted if out of QuestState's
    /// enum range -- (QuestState)someInt does NOT throw on bad/corrupted save data, so an
    /// unclamped cast could hand callers a garbage enum value. Missing entries also read as
    /// Unstarted (a quest never touched has no PlayerData.questStates entry at all).
    /// </summary>
    public static QuestState GetState(PlayerData data, string questId)
    {
        if (data == null || data.questStates == null || string.IsNullOrEmpty(questId))
            return QuestState.Unstarted;

        if (!data.questStates.TryGetValue(questId, out int raw))
            return QuestState.Unstarted;

        if (raw < (int)QuestState.Unstarted || raw > (int)QuestState.Failed)
            return QuestState.Unstarted;

        return (QuestState)raw;
    }

    /// <summary>Writes `newState` into PlayerData.questStates and raises EventBus.QuestStateUpdated.</summary>
    public static void SetState(PlayerData data, string questId, QuestState newState)
    {
        if (data == null || string.IsNullOrEmpty(questId))
            return;

        if (data.questStates == null)
            data.questStates = new Dictionary<string, int>();

        data.questStates[questId] = (int)newState;
        EventBus.RaiseQuestStateUpdated(questId, (int)newState);
    }

    /// <summary>
    /// The single deterministic quest-tick point (charter Step 12, "shrine rest"). For each
    /// quest in `allQuests` currently Active whose advancementConditions are all met (or has no
    /// conditions at all -- an empty list is treated as "never auto-advances", not "always
    /// advances", so a quest with no wired conditions stays Active until moved explicitly by a
    /// dialogue mutation), advances it to ObjectiveComplete.
    /// </summary>
    public static void TickOnRest(PlayerData data, IEnumerable<Quest> allQuests)
    {
        if (data == null || allQuests == null)
            return;

        foreach (Quest quest in allQuests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questId))
                continue;

            if (GetState(data, quest.questId) != QuestState.Active)
                continue;

            if (quest.advancementConditions == null || quest.advancementConditions.Count == 0)
                continue;

            if (AllConditionsMet(quest.advancementConditions, data))
                SetState(data, quest.questId, QuestState.ObjectiveComplete);
        }
    }

    /// <summary>
    /// The locked single-arg entry point Shrine.Interact calls. Ticks against every currently-
    /// registered Quest (see class doc comment) -- if nothing is registered (e.g. no Quest asset
    /// has been loaded yet), this is a safe no-op, matching Shrine's own "no active save context
    /// -> degrade gracefully" precedent rather than throwing.
    /// </summary>
    public static void TickOnRest(PlayerData data)
    {
        TickOnRest(data, _registeredQuests);
    }

    private static bool AllConditionsMet(List<DialogueCondition> conditions, PlayerData data)
    {
        foreach (DialogueCondition condition in conditions)
        {
            if (!DialogueConditionEvaluator.Evaluate(condition, data))
                return false;
        }

        return true;
    }
}
