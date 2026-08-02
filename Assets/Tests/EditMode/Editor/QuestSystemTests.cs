using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// QuestSystem (charter Step 12) -- pure static functions over PlayerData.questStates.
// QuestSystem.ClearRegistry() is called in SetUp/TearDown because Quest.OnEnable
// self-registers (see QuestSystem.cs's own doc comment) and ScriptableObject.CreateInstance
// DOES run OnEnable in this Editor/test context (confirmed by ChestTests/ItemDatabaseTests'
// own use of the same pattern) -- without clearing, one test's Quest instances would leak into
// another test's TickOnRest(data) (single-arg) call.
public class QuestSystemTests
{
    [SetUp]
    public void SetUp()
    {
        QuestSystem.ClearRegistry();
    }

    [TearDown]
    public void TearDown()
    {
        QuestSystem.ClearRegistry();
    }

    private static Quest MakeQuest(string questId, List<DialogueCondition> advancementConditions = null)
    {
        var quest = ScriptableObject.CreateInstance<Quest>();
        quest.questId = questId;
        quest.advancementConditions = advancementConditions ?? new List<DialogueCondition>();
        return quest;
    }

    // --- GetState ---

    [Test]
    public void GetState_NeverTouchedQuest_ReturnsUnstarted()
    {
        var data = new PlayerData();

        Assert.AreEqual(QuestState.Unstarted, QuestSystem.GetState(data, "never_touched"));
    }

    [Test]
    public void GetState_NullPlayerData_ReturnsUnstarted_DoesNotThrow()
    {
        Assert.AreEqual(QuestState.Unstarted, QuestSystem.GetState(null, "any"));
    }

    [Test]
    public void GetState_NullOrEmptyQuestId_ReturnsUnstarted()
    {
        var data = new PlayerData();
        Assert.AreEqual(QuestState.Unstarted, QuestSystem.GetState(data, null));
        Assert.AreEqual(QuestState.Unstarted, QuestSystem.GetState(data, string.Empty));
    }

    [Test]
    public void GetState_StoredValue_ReturnsMatchingEnum()
    {
        var data = new PlayerData();
        data.questStates["main"] = (int)QuestState.ObjectiveComplete;

        Assert.AreEqual(QuestState.ObjectiveComplete, QuestSystem.GetState(data, "main"));
    }

    [TestCase(-1)]
    [TestCase(999)]
    public void GetState_OutOfRangeStoredInt_ClampsToUnstarted(int corruptValue)
    {
        // (QuestState)someInt does NOT throw on bad/corrupted save data -- GetState must
        // clamp explicitly rather than handing callers a garbage enum value.
        var data = new PlayerData();
        data.questStates["corrupted"] = corruptValue;

        Assert.AreEqual(QuestState.Unstarted, QuestSystem.GetState(data, "corrupted"));
    }

    // --- SetState ---

    [Test]
    public void SetState_WritesIntoQuestStatesDictionary()
    {
        var data = new PlayerData();

        QuestSystem.SetState(data, "main", QuestState.Active);

        Assert.AreEqual((int)QuestState.Active, data.questStates["main"]);
    }

    [Test]
    public void SetState_ThenGetState_RoundTrips()
    {
        var data = new PlayerData();

        QuestSystem.SetState(data, "main", QuestState.Completed);

        Assert.AreEqual(QuestState.Completed, QuestSystem.GetState(data, "main"));
    }

    [Test]
    public void SetState_RaisesQuestStateUpdatedEvent()
    {
        var data = new PlayerData();
        string receivedId = null;
        int receivedState = -1;
        void Handler(string questId, int state) { receivedId = questId; receivedState = state; }

        EventBus.QuestStateUpdated += Handler;
        try
        {
            QuestSystem.SetState(data, "main", QuestState.Active);
        }
        finally
        {
            EventBus.QuestStateUpdated -= Handler;
        }

        Assert.AreEqual("main", receivedId);
        Assert.AreEqual((int)QuestState.Active, receivedState);
    }

    [Test]
    public void SetState_NullPlayerData_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => QuestSystem.SetState(null, "main", QuestState.Active));
    }

    [Test]
    public void SetState_NullOrEmptyQuestId_DoesNotThrow_DoesNotWrite()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, null, QuestState.Active);
        QuestSystem.SetState(data, string.Empty, QuestState.Active);

        Assert.AreEqual(0, data.questStates.Count);
    }

    // --- TickOnRest(data, quests) explicit-catalogue overload ---

    [Test]
    public void TickOnRest_ActiveQuest_ConditionsMet_AdvancesToObjectiveComplete()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Active);
        data.dialogueSeen.Add("shrine_info");

        var conditions = new List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "shrine_info", intValue = 1 },
        };
        var quest = MakeQuest("main", conditions);

        try
        {
            QuestSystem.TickOnRest(data, new List<Quest> { quest });

            Assert.AreEqual(QuestState.ObjectiveComplete, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_ActiveQuest_ConditionsNotMet_StaysActive()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Active);
        // dialogueSeen deliberately left empty -- condition below will not be met.

        var conditions = new List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "shrine_info", intValue = 1 },
        };
        var quest = MakeQuest("main", conditions);

        try
        {
            QuestSystem.TickOnRest(data, new List<Quest> { quest });

            Assert.AreEqual(QuestState.Active, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_NonActiveQuest_IsIgnored()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Completed);

        var conditions = new List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.PlayerLevel, op = DialogueCondition.Op.GreaterOrEqual, intValue = 0 },
        };
        var quest = MakeQuest("main", conditions);

        try
        {
            QuestSystem.TickOnRest(data, new List<Quest> { quest });

            Assert.AreEqual(QuestState.Completed, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_ActiveQuest_NoAdvancementConditions_NeverAutoAdvances()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Active);
        var quest = MakeQuest("main", new List<DialogueCondition>());

        try
        {
            QuestSystem.TickOnRest(data, new List<Quest> { quest });

            Assert.AreEqual(QuestState.Active, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_NullData_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => QuestSystem.TickOnRest(null, new List<Quest>()));
    }

    [Test]
    public void TickOnRest_NullQuestList_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => QuestSystem.TickOnRest(new PlayerData(), null));
    }

    [Test]
    public void TickOnRest_NullEntryInQuestList_IsSkipped_DoesNotThrow()
    {
        var data = new PlayerData();
        Assert.DoesNotThrow(() => QuestSystem.TickOnRest(data, new List<Quest> { null }));
    }

    // --- TickOnRest(data) single-arg (the exact call Shrine.Interact makes), registry-backed ---

    [Test]
    public void TickOnRest_SingleArg_TicksAgainstRegisteredQuests()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Active);
        data.dialogueSeen.Add("shrine_info");

        var conditions = new List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "shrine_info", intValue = 1 },
        };
        // MakeQuest uses ScriptableObject.CreateInstance, which runs Quest.OnEnable and
        // therefore self-registers with QuestSystem -- no explicit registration call needed
        // here, proving the registry wiring Shrine.Interact relies on.
        var quest = MakeQuest("main", conditions);

        try
        {
            QuestSystem.TickOnRest(data);

            Assert.AreEqual(QuestState.ObjectiveComplete, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_SingleArg_UnregisteredQuest_IsNotTicked()
    {
        var data = new PlayerData();
        QuestSystem.SetState(data, "main", QuestState.Active);
        data.dialogueSeen.Add("shrine_info");

        var conditions = new List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "shrine_info", intValue = 1 },
        };
        var quest = MakeQuest("main", conditions);
        QuestSystem.UnregisterQuest(quest);

        try
        {
            QuestSystem.TickOnRest(data);

            Assert.AreEqual(QuestState.Active, QuestSystem.GetState(data, "main"), "An unregistered quest must not be ticked by the single-arg overload.");
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }

    [Test]
    public void TickOnRest_SingleArg_NoRegisteredQuests_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => QuestSystem.TickOnRest(new PlayerData()));
    }

    [Test]
    public void RegisterQuest_SameQuestTwice_DoesNotDuplicateInRegistry()
    {
        var quest = MakeQuest("main");

        try
        {
            // Constructor already registered it once via OnEnable -- register again explicitly.
            QuestSystem.RegisterQuest(quest);
            QuestSystem.RegisterQuest(quest);

            var data = new PlayerData();
            QuestSystem.SetState(data, "main", QuestState.Active);
            data.dialogueSeen.Add("x");
            quest.advancementConditions = new List<DialogueCondition>
            {
                new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "x", intValue = 1 },
            };

            Assert.DoesNotThrow(() => QuestSystem.TickOnRest(data));
            Assert.AreEqual(QuestState.ObjectiveComplete, QuestSystem.GetState(data, "main"));
        }
        finally
        {
            Object.DestroyImmediate(quest);
        }
    }
}
