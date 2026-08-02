using NUnit.Framework;
using UnityEngine;

// Shrine's Interact is a rest behavior (charter Step 10 placeholder, checkpoint-save wiring
// added Step 11 -- see docs/Tasks/2026-08-02-step-11-hud-persistence.md). Covers both the
// original placeholder ShowNotice behavior AND the new SaveSystem.CurrentPlayerData wiring
// -- proving the "at least one real checkpoint trigger calls SaveSystem.Save() end-to-end"
// DoD item. SaveSystem's static Current/CurrentPlayerData/CurrentSlot holders are reset in
// SetUp/TearDown so this suite never leaks state into (or is polluted by) any other test that
// might touch those same statics.
public class ShrineTests
{
    private GameObject _go;
    private Shrine _shrine;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Shrine", typeof(BoxCollider));
        _shrine = _go.AddComponent<Shrine>();

        SaveSystem.Current = null;
        SaveSystem.CurrentPlayerData = null;
        SaveSystem.CurrentSlot = 0;

        // QuestSystem's Quest-registry is static (Step 12) -- clear it so no other test's
        // registered Quest instances leak into Shrine.Interact's QuestSystem.TickOnRest(data)
        // call, and vice versa.
        QuestSystem.ClearRegistry();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);

        SaveSystem.Current = null;
        SaveSystem.CurrentPlayerData = null;
        SaveSystem.CurrentSlot = 0;

        QuestSystem.ClearRegistry();
    }

    [Test]
    public void Interact_RaisesShowNotice()
    {
        string receivedText = null;
        float receivedDuration = -1f;

        void Handler(string text, float duration)
        {
            receivedText = text;
            receivedDuration = duration;
        }

        EventBus.ShowNotice += Handler;
        try
        {
            _shrine.Interact(_go.transform);
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Rested at the shrine.", receivedText);
        Assert.AreEqual(3f, receivedDuration);
    }

    [Test]
    public void Interact_IsRepeatable_NoOneShotGate()
    {
        // Unlike Chest/HarvestNode, Shrine has no one-shot depletion -- CanInteract must
        // remain true across repeated interacts (this task's scope: real rest/cooldown
        // behavior is Step 11's job).
        Assert.DoesNotThrow(() => _shrine.Interact(_go.transform));
        Assert.IsTrue(_shrine.CanInteract(_go.transform));
        Assert.DoesNotThrow(() => _shrine.Interact(_go.transform));
        Assert.IsTrue(_shrine.CanInteract(_go.transform));
    }

    [Test]
    public void Interact_NoActiveSaveContext_DoesNotThrow_StillRaisesNotice()
    {
        // SaveSystem.CurrentPlayerData is null (SetUp default) -- e.g. a scene entered
        // directly without going through slot-select. Must degrade gracefully, not throw.
        string receivedText = null;
        void Handler(string text, float duration) => receivedText = text;

        EventBus.ShowNotice += Handler;
        try
        {
            Assert.DoesNotThrow(() => _shrine.Interact(_go.transform));
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Rested at the shrine.", receivedText);
    }

    [Test]
    public void Interact_ActiveSaveContext_AddsShrineIdToDiscoveredShrines()
    {
        TestReflectionUtil.SetField(_shrine, "shrineId", "prologue_entrance");
        SaveSystem.CurrentPlayerData = new PlayerData();

        _shrine.Interact(_go.transform);

        Assert.IsTrue(SaveSystem.CurrentPlayerData.discoveredShrines.Contains("prologue_entrance"));
    }

    [Test]
    public void Interact_ActiveSaveContext_CallsSaveSystemSave_EndToEnd()
    {
        string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ReturnShrineTests_" + System.Guid.NewGuid().ToString("N"));
        var saveSystem = new SaveSystem(tempRoot);

        try
        {
            TestReflectionUtil.SetField(_shrine, "shrineId", "prologue_entrance");
            SaveSystem.Current = saveSystem;
            SaveSystem.CurrentSlot = 0;
            SaveSystem.CurrentPlayerData = new PlayerData();

            _shrine.Interact(_go.transform);

            // Proves this is a REAL end-to-end SaveSystem.Save() call, not just an in-memory
            // discoveredShrines mutation -- a live file must exist on disk afterward.
            string livePath = System.IO.Path.Combine(tempRoot, "slot0.json");
            Assert.IsTrue(System.IO.File.Exists(livePath));
            StringAssert.Contains("prologue_entrance", System.IO.File.ReadAllText(livePath));
        }
        finally
        {
            if (System.IO.Directory.Exists(tempRoot))
                System.IO.Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public void Interact_QuestSystemTickOnRest_RunsBeforeSaveSystemSave()
    {
        // Charter Step 12: shrine rest is the single deterministic quest-tick point, and the
        // ordering is locked -- QuestSystem.TickOnRest(data) must run BEFORE
        // SaveSystem.Current.Save(...), or every rest persists the pre-tick state and a reload
        // loses one tick of progression. Proven end-to-end via the real SaveSystem/QuestSystem
        // registry: a quest that ticks Active -> ObjectiveComplete on rest must already show
        // ObjectiveComplete in the file SaveSystem.Save() wrote, not the pre-tick Active value.
        string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ReturnShrineTests_" + System.Guid.NewGuid().ToString("N"));
        var saveSystem = new SaveSystem(tempRoot);

        Quest quest = ScriptableObject.CreateInstance<Quest>();
        quest.questId = "prologue_wanderers_path";
        quest.advancementConditions = new System.Collections.Generic.List<DialogueCondition>
        {
            new DialogueCondition { source = DialogueCondition.Source.DialogueSeen, op = DialogueCondition.Op.Equals, key = "shrine_info", intValue = 1 },
        };

        try
        {
            TestReflectionUtil.SetField(_shrine, "shrineId", "prologue_entrance");
            SaveSystem.Current = saveSystem;
            SaveSystem.CurrentSlot = 0;
            SaveSystem.CurrentPlayerData = new PlayerData();
            QuestSystem.SetState(SaveSystem.CurrentPlayerData, "prologue_wanderers_path", QuestState.Active);
            SaveSystem.CurrentPlayerData.dialogueSeen.Add("shrine_info");

            _shrine.Interact(_go.transform);

            // In-memory state reflects the post-tick value.
            Assert.AreEqual(QuestState.ObjectiveComplete, QuestSystem.GetState(SaveSystem.CurrentPlayerData, "prologue_wanderers_path"));

            // The persisted file reflects it too -- proving the tick ran BEFORE Save(), not
            // after. Parsed back through the real DTO type rather than raw string-matching so
            // this assertion isn't sensitive to JsonUtility's pretty-print spacing.
            string livePath = System.IO.Path.Combine(tempRoot, "slot0.json");
            string json = System.IO.File.ReadAllText(livePath);
            PlayerSaveDto persistedDto = JsonUtility.FromJson<PlayerSaveDto>(json);

            bool found = false;
            foreach (var entry in persistedDto.questStates)
            {
                if (entry.questId == "prologue_wanderers_path")
                {
                    Assert.AreEqual((int)QuestState.ObjectiveComplete, entry.state);
                    found = true;
                }
            }
            Assert.IsTrue(found, "Persisted save file must contain the post-tick quest state.");
        }
        finally
        {
            Object.DestroyImmediate(quest);
            if (System.IO.Directory.Exists(tempRoot))
                System.IO.Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public void Interact_NoShrineIdConfigured_SkipsSave_StillRaisesNotice()
    {
        // shrineId left unset (default null/empty) -- Interact must not attempt to save an
        // un-attributable discovery entry, but the notice still fires (documented behavior,
        // not an error path).
        SaveSystem.CurrentPlayerData = new PlayerData();

        string receivedText = null;
        void Handler(string text, float duration) => receivedText = text;

        EventBus.ShowNotice += Handler;
        try
        {
            _shrine.Interact(_go.transform);
        }
        finally
        {
            EventBus.ShowNotice -= Handler;
        }

        Assert.AreEqual("Rested at the shrine.", receivedText);
        Assert.AreEqual(0, SaveSystem.CurrentPlayerData.discoveredShrines.Count);
    }
}
