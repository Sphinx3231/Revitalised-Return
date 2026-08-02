/// <summary>
/// Whitelisted condition data type (charter Step 12) -- source/key/op/value, never a
/// dynamically-evaluated expression string on authored data. This is a hard security/
/// correctness rule carried verbatim from the original Godot charter's Expression.parse() ban,
/// generalized to this engine: DialogueConditionEvaluator is a closed switch over Source/Op,
/// structurally incapable of an eval-equivalent path.
///
/// Op is meaningless against NpcState's free-form strings (no total ordering) --
/// DialogueConditionEvaluator returns false for that combination rather than throwing (Research
/// finding 4).
/// </summary>
[System.Serializable]
public class DialogueCondition
{
    public enum Source
    {
        QuestState,
        WorldFlag,
        ItemOwned,
        NpcState,
        DialogueSeen,
        PlayerLevel,
    }

    public enum Op
    {
        Equals,
        NotEquals,
        GreaterOrEqual,
        LessOrEqual,
    }

    public Source source;
    public Op op;
    public string key;
    public int intValue;
    public string stringValue;
}
