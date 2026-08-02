/// <summary>
/// Pure static evaluator for DialogueCondition (charter Step 12) -- a closed switch over
/// Source/Op, structurally incapable of an eval-equivalent path (see DialogueCondition's own
/// doc comment for the security rationale). Handles null PlayerData/collections gracefully by
/// returning false rather than throwing -- an author-time condition referencing state that
/// doesn't exist yet (or a caller with no active save context) should just read as "not met",
/// not crash dialogue playback.
/// </summary>
public static class DialogueConditionEvaluator
{
    public static bool Evaluate(DialogueCondition condition, PlayerData data)
    {
        if (condition == null || data == null)
            return false;

        switch (condition.source)
        {
            case DialogueCondition.Source.QuestState:
                return EvaluateInt(GetQuestStateInt(data, condition.key), condition.op, condition.intValue);

            case DialogueCondition.Source.WorldFlag:
                return EvaluateBoolAsInt(GetWorldFlag(data, condition.key), condition.op, condition.intValue);

            case DialogueCondition.Source.ItemOwned:
                // Inventory.HasItem is boolean-only (no quantity accessor) -- ItemOwned stays
                // boolean-only this task, a named gap per the Approach doc, not silently filled.
                return EvaluateBoolAsInt(HasItem(data, condition.key), condition.op, condition.intValue);

            case DialogueCondition.Source.NpcState:
                return EvaluateNpcState(data, condition);

            case DialogueCondition.Source.DialogueSeen:
                return EvaluateBoolAsInt(HasSeenDialogue(data, condition.key), condition.op, condition.intValue);

            case DialogueCondition.Source.PlayerLevel:
                return EvaluateInt(data.level, condition.op, condition.intValue);

            default:
                return false;
        }
    }

    private static int GetQuestStateInt(PlayerData data, string key)
    {
        if (string.IsNullOrEmpty(key) || data.questStates == null)
            return (int)QuestState.Unstarted;

        return data.questStates.TryGetValue(key, out int value) ? value : (int)QuestState.Unstarted;
    }

    private static bool GetWorldFlag(PlayerData data, string key)
    {
        if (string.IsNullOrEmpty(key) || data.worldFlags == null)
            return false;

        return data.worldFlags.TryGetValue(key, out bool value) && value;
    }

    private static bool HasItem(PlayerData data, string itemId)
    {
        return data.inventory != null && data.inventory.HasItem(itemId);
    }

    private static bool HasSeenDialogue(PlayerData data, string nodeId)
    {
        return !string.IsNullOrEmpty(nodeId) && data.dialogueSeen != null && data.dialogueSeen.Contains(nodeId);
    }

    /// <summary>
    /// NpcState is a free-form string with no total ordering -- GreaterOrEqual/LessOrEqual are
    /// meaningless against it and deliberately return false (not throw), per Research finding 4.
    /// </summary>
    private static bool EvaluateNpcState(PlayerData data, DialogueCondition condition)
    {
        if (condition.op != DialogueCondition.Op.Equals && condition.op != DialogueCondition.Op.NotEquals)
            return false;

        string actual = null;
        if (!string.IsNullOrEmpty(condition.key) && data.npcStates != null)
            data.npcStates.TryGetValue(condition.key, out actual);

        bool equal = actual == condition.stringValue;
        return condition.op == DialogueCondition.Op.Equals ? equal : !equal;
    }

    private static bool EvaluateInt(int actual, DialogueCondition.Op op, int expected)
    {
        switch (op)
        {
            case DialogueCondition.Op.Equals: return actual == expected;
            case DialogueCondition.Op.NotEquals: return actual != expected;
            case DialogueCondition.Op.GreaterOrEqual: return actual >= expected;
            case DialogueCondition.Op.LessOrEqual: return actual <= expected;
            default: return false;
        }
    }

    private static bool EvaluateBoolAsInt(bool actual, DialogueCondition.Op op, int expected)
    {
        return EvaluateInt(actual ? 1 : 0, op, expected);
    }
}
