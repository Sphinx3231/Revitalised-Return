using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// DialogueConditionEvaluator (charter Step 12) -- a closed switch over Source/Op, structurally
// incapable of an eval-equivalent path. Covers all 6 sources, the relevant ops per source, the
// NpcState+ordering-op "false, not throw" edge (Research finding 4), and null-data handling.
public class DialogueConditionEvaluatorTests
{
    private PlayerData _data;
    private ItemData _ore;

    [SetUp]
    public void SetUp()
    {
        _data = new PlayerData();
        _ore = ScriptableObject.CreateInstance<ItemData>();
        _ore.itemId = "tamahagane_ore";
        _ore.maxStack = 99;
    }

    [TearDown]
    public void TearDown()
    {
        if (_ore != null) Object.DestroyImmediate(_ore);
    }

    private static DialogueCondition Make(DialogueCondition.Source source, DialogueCondition.Op op, string key, int intValue = 0, string stringValue = null)
    {
        return new DialogueCondition { source = source, op = op, key = key, intValue = intValue, stringValue = stringValue };
    }

    [Test]
    public void Evaluate_NullCondition_ReturnsFalse()
    {
        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(null, _data));
    }

    [Test]
    public void Evaluate_NullPlayerData_ReturnsFalse()
    {
        var condition = Make(DialogueCondition.Source.PlayerLevel, DialogueCondition.Op.Equals, null, 1);
        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, null));
    }

    // --- QuestState ---

    [Test]
    public void QuestState_Equals_MatchesStoredValue()
    {
        _data.questStates["main"] = (int)QuestState.Active;
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.Equals, "main", (int)QuestState.Active);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void QuestState_MissingKey_ReadsAsUnstarted()
    {
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.Equals, "unset_quest", (int)QuestState.Unstarted);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void QuestState_GreaterOrEqual_ComparesOrderedInts()
    {
        _data.questStates["main"] = (int)QuestState.ObjectiveComplete;
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.GreaterOrEqual, "main", (int)QuestState.Active);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void QuestState_LessOrEqual_ComparesOrderedInts()
    {
        _data.questStates["main"] = (int)QuestState.Active;
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.LessOrEqual, "main", (int)QuestState.Completed);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void QuestState_NotEquals_TrueWhenDifferent()
    {
        _data.questStates["main"] = (int)QuestState.Active;
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.NotEquals, "main", (int)QuestState.Completed);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    // --- WorldFlag ---

    [Test]
    public void WorldFlag_Equals_TrueMatchesSetFlag()
    {
        _data.worldFlags["gate_open"] = true;
        var condition = Make(DialogueCondition.Source.WorldFlag, DialogueCondition.Op.Equals, "gate_open", 1);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void WorldFlag_MissingKey_ReadsAsFalse()
    {
        var condition = Make(DialogueCondition.Source.WorldFlag, DialogueCondition.Op.Equals, "never_set", 0);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void WorldFlag_NotEquals_TrueWhenFlagDiffers()
    {
        _data.worldFlags["gate_open"] = false;
        var condition = Make(DialogueCondition.Source.WorldFlag, DialogueCondition.Op.NotEquals, "gate_open", 1);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    // --- ItemOwned ---

    [Test]
    public void ItemOwned_Equals_TrueWhenItemInInventory()
    {
        _data.inventory.AddItem(_ore, 1);
        var condition = Make(DialogueCondition.Source.ItemOwned, DialogueCondition.Op.Equals, "tamahagane_ore", 1);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void ItemOwned_Equals_FalseWhenNotOwned()
    {
        var condition = Make(DialogueCondition.Source.ItemOwned, DialogueCondition.Op.Equals, "tamahagane_ore", 1);

        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    // --- NpcState ---

    [Test]
    public void NpcState_Equals_MatchesStoredString()
    {
        _data.npcStates["blacksmith"] = "relocated";
        var condition = Make(DialogueCondition.Source.NpcState, DialogueCondition.Op.Equals, "blacksmith", stringValue: "relocated");

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void NpcState_NotEquals_TrueWhenDifferentString()
    {
        _data.npcStates["blacksmith"] = "relocated";
        var condition = Make(DialogueCondition.Source.NpcState, DialogueCondition.Op.NotEquals, "blacksmith", stringValue: "gone");

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [TestCase(DialogueCondition.Op.GreaterOrEqual)]
    [TestCase(DialogueCondition.Op.LessOrEqual)]
    public void NpcState_OrderingOps_ReturnFalse_NotThrow(DialogueCondition.Op op)
    {
        _data.npcStates["blacksmith"] = "relocated";
        var condition = Make(DialogueCondition.Source.NpcState, op, "blacksmith", stringValue: "relocated");

        bool result = false;
        Assert.DoesNotThrow(() => result = DialogueConditionEvaluator.Evaluate(condition, _data));
        Assert.IsFalse(result);
    }

    // --- DialogueSeen ---

    [Test]
    public void DialogueSeen_Equals_TrueWhenNodeIdInSet()
    {
        _data.dialogueSeen.Add("intro_line_1");
        var condition = Make(DialogueCondition.Source.DialogueSeen, DialogueCondition.Op.Equals, "intro_line_1", 1);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void DialogueSeen_Equals_FalseWhenNotSeen()
    {
        var condition = Make(DialogueCondition.Source.DialogueSeen, DialogueCondition.Op.Equals, "never_shown", 1);

        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    // --- PlayerLevel ---

    [Test]
    public void PlayerLevel_GreaterOrEqual_TrueWhenAtOrAboveThreshold()
    {
        _data.level = 5;
        var condition = Make(DialogueCondition.Source.PlayerLevel, DialogueCondition.Op.GreaterOrEqual, null, 5);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void PlayerLevel_LessOrEqual_FalseWhenAboveThreshold()
    {
        _data.level = 10;
        var condition = Make(DialogueCondition.Source.PlayerLevel, DialogueCondition.Op.LessOrEqual, null, 5);

        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    [Test]
    public void PlayerLevel_Equals_ExactMatch()
    {
        _data.level = 7;
        var condition = Make(DialogueCondition.Source.PlayerLevel, DialogueCondition.Op.Equals, null, 7);

        Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data));
    }

    // --- Null-collection defensive handling ---

    [Test]
    public void Evaluate_NullQuestStatesDictionary_DoesNotThrow_ReadsUnstarted()
    {
        _data.questStates = null;
        var condition = Make(DialogueCondition.Source.QuestState, DialogueCondition.Op.Equals, "main", (int)QuestState.Unstarted);

        Assert.DoesNotThrow(() => Assert.IsTrue(DialogueConditionEvaluator.Evaluate(condition, _data)));
    }

    [Test]
    public void Evaluate_NullInventory_DoesNotThrow_ReadsFalse()
    {
        _data.inventory = null;
        var condition = Make(DialogueCondition.Source.ItemOwned, DialogueCondition.Op.Equals, "tamahagane_ore", 1);

        Assert.DoesNotThrow(() => Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data)));
    }

    [Test]
    public void Evaluate_NullDialogueSeenSet_DoesNotThrow_ReadsFalse()
    {
        _data.dialogueSeen = null;
        var condition = Make(DialogueCondition.Source.DialogueSeen, DialogueCondition.Op.Equals, "intro", 1);

        Assert.DoesNotThrow(() => Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data)));
    }

    [Test]
    public void Evaluate_NullNpcStatesDictionary_DoesNotThrow_ReadsFalse()
    {
        _data.npcStates = null;
        var condition = Make(DialogueCondition.Source.NpcState, DialogueCondition.Op.Equals, "blacksmith", stringValue: "relocated");

        Assert.DoesNotThrow(() => Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data)));
    }

    [Test]
    public void Evaluate_UnknownSource_ReturnsFalse()
    {
        var condition = Make((DialogueCondition.Source)999, DialogueCondition.Op.Equals, "x");

        Assert.IsFalse(DialogueConditionEvaluator.Evaluate(condition, _data));
    }
}
