using NUnit.Framework;
using UnityEngine;

// DialogueRunner (charter Step 12) -- owns the GameState.Dialogue transition and mutation
// application. Research's single highest-risk item this task: dialogue must never leave the
// player permanently softlocked in GameState.Dialogue. Begin() sets Dialogue, mutations apply
// on node DISPLAY (not exit), and End() unconditionally restores Playing -- all three are
// asserted directly. Constructed via `new GameObject().AddComponent<DialogueRunner>()`, then
// Awake() is invoked explicitly via reflection -- AddComponent does NOT call Awake() in an
// EditMode test context (established pattern, see SandboxAutoPlayTests).
public class DialogueRunnerTests
{
    private GameObject _go;
    private DialogueRunner _runner;

    private DialogueTree _tree;
    private PlayerData _previousPlayerData;

    [SetUp]
    public void SetUp()
    {
        _previousPlayerData = SaveSystem.CurrentPlayerData;
        SaveSystem.CurrentPlayerData = new PlayerData();

        SetStaticInstance(null);

        _go = new GameObject("DialogueRunner");
        _runner = _go.AddComponent<DialogueRunner>();
        // AddComponent does NOT invoke Awake() in an EditMode test context (established
        // pattern -- see SandboxAutoPlayTests) -- invoke it explicitly so Instance is set.
        TestReflectionUtil.InvokeMethod(_runner, "Awake");

        _tree = ScriptableObject.CreateInstance<DialogueTree>();
    }

    [TearDown]
    public void TearDown()
    {
        GameState.SetState(GameState.State.Playing);
        SaveSystem.CurrentPlayerData = _previousPlayerData;

        if (_go != null) Object.DestroyImmediate(_go);
        if (_tree != null) Object.DestroyImmediate(_tree);

        SetStaticInstance(null);
    }

    private static void SetStaticInstance(DialogueRunner instance)
    {
        // Matches GameStateTests' own established pattern for resetting an auto-property's
        // static backing field directly (private-setter properties can't be written via a
        // public PropertyInfo.SetValue call).
        var field = typeof(DialogueRunner).GetField("<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, instance);
    }

    private static DialogueNode MakeNode(string id, string speaker, string text)
    {
        return new DialogueNode { id = id, speaker = speaker, text = text };
    }

    [Test]
    public void Awake_SetsInstance()
    {
        Assert.AreSame(_runner, DialogueRunner.Instance);
    }

    [Test]
    public void Begin_SetsGameStateToDialogue()
    {
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.AreEqual(GameState.State.Dialogue, GameState.CurrentState);
    }

    [Test]
    public void Begin_SetsCurrentNodeToStartNode()
    {
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.IsNotNull(_runner.CurrentNode);
        Assert.AreEqual("greet", _runner.CurrentNode.id);
    }

    [Test]
    public void Begin_AddsStartNodeIdToDialogueSeen()
    {
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.IsTrue(SaveSystem.CurrentPlayerData.dialogueSeen.Contains("greet"));
    }

    [Test]
    public void Begin_AppliesStartNodeMutations_OnDisplay()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.QuestState, key = "main", intValue = (int)QuestState.Active });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.AreEqual(QuestState.Active, QuestSystem.GetState(SaveSystem.CurrentPlayerData, "main"));
    }

    [Test]
    public void Begin_UnknownStartNodeId_EndsDialogue_RestoresPlaying_DoesNotSoftlock()
    {
        _tree.startNodeId = "nonexistent";
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        // The single-highest-risk case: an authoring error (bad startNodeId) must not leave
        // the player stuck in GameState.Dialogue with nothing displayed.
        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
        Assert.IsFalse(_runner.IsActive);
    }

    [Test]
    public void Begin_NullTree_DoesNotThrow_DoesNotChangeState()
    {
        GameState.SetState(GameState.State.Playing);

        Assert.DoesNotThrow(() => _runner.Begin(null, "npc_1"));
        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
    }

    [Test]
    public void SelectChoice_TargetNodeId_MovesToThatNode_AppliesItsMutationsOnDisplay()
    {
        var greet = MakeNode("greet", "NPC", "Hello.");
        var branch = MakeNode("branch", "NPC", "Good choice.");
        branch.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.QuestState, key = "main", intValue = (int)QuestState.Active });
        greet.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "branch" });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(greet);
        _tree.nodes.Add(branch);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");
        _runner.SelectChoice(greet.choices[0]);

        Assert.AreEqual("branch", _runner.CurrentNode.id);
        Assert.AreEqual(QuestState.Active, QuestSystem.GetState(SaveSystem.CurrentPlayerData, "main"));
    }

    [Test]
    public void SelectChoice_EmptyTargetNodeId_EndsDialogue_RestoresPlaying()
    {
        var greet = MakeNode("greet", "NPC", "Hello.");
        greet.choices.Add(new DialogueChoice { text = "Leave", targetNodeId = "" });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(greet);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");
        _runner.SelectChoice(greet.choices[0]);

        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
        Assert.IsFalse(_runner.IsActive);
    }

    [Test]
    public void End_UnconditionallyRestoresPlaying_ClearsActiveState()
    {
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();
        _runner.Begin(_tree, "npc_1");

        _runner.End();

        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
        Assert.IsFalse(_runner.IsActive);
        Assert.IsNull(_runner.CurrentNode);
    }

    [Test]
    public void End_CalledWithoutBegin_DoesNotThrow_StillSetsPlaying()
    {
        GameState.SetState(GameState.State.Dialogue);

        Assert.DoesNotThrow(() => _runner.End());
        Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
    }

    [Test]
    public void Begin_NoActiveSaveContext_DoesNotThrow_StillSetsDialogueState()
    {
        SaveSystem.CurrentPlayerData = null;
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();

        Assert.DoesNotThrow(() => _runner.Begin(_tree, "npc_1"));
        Assert.AreEqual(GameState.State.Dialogue, GameState.CurrentState);
    }

    [Test]
    public void VisibleChoices_NoConditions_AllReturned()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.choices.Add(new DialogueChoice { text = "A", targetNodeId = "a" });
        node.choices.Add(new DialogueChoice { text = "B", targetNodeId = "b" });

        var visible = DialogueRunner.VisibleChoices(node);

        Assert.AreEqual(2, visible.Count);
    }

    [Test]
    public void VisibleChoices_FailingCondition_HidesChoice()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        var choice = new DialogueChoice { text = "A", targetNodeId = "a" };
        choice.visibilityConditions.Add(new DialogueCondition { source = DialogueCondition.Source.PlayerLevel, op = DialogueCondition.Op.GreaterOrEqual, intValue = 99 });
        node.choices.Add(choice);

        var visible = DialogueRunner.VisibleChoices(node);

        Assert.AreEqual(0, visible.Count);
    }

    [Test]
    public void VisibleChoices_NullNode_ReturnsEmptyList_DoesNotThrow()
    {
        System.Collections.Generic.List<DialogueChoice> result = null;
        Assert.DoesNotThrow(() => result = DialogueRunner.VisibleChoices(null));
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void VisibleChoices_PassingCondition_ChoiceIncluded()
    {
        // Default PlayerData.level is 0, which satisfies ">= 0" without any extra setup.
        var node = MakeNode("greet", "NPC", "Hello.");
        var choice = new DialogueChoice { text = "A", targetNodeId = "a" };
        choice.visibilityConditions.Add(new DialogueCondition { source = DialogueCondition.Source.PlayerLevel, op = DialogueCondition.Op.GreaterOrEqual, intValue = 0 });
        node.choices.Add(choice);

        var visible = DialogueRunner.VisibleChoices(node);

        Assert.AreEqual(1, visible.Count);
    }

    // --- ApplyMutations coverage (private static, exercised via Begin's on-display application) ---

    [Test]
    public void Begin_WorldFlagMutation_SetsWorldFlag()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.WorldFlag, key = "gate_open", intValue = 1 });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.IsTrue(SaveSystem.CurrentPlayerData.worldFlags["gate_open"]);
    }

    [Test]
    public void Begin_NpcStateMutation_SetsNpcState()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.NpcState, key = "elder", stringValue = "friendly" });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.AreEqual("friendly", SaveSystem.CurrentPlayerData.npcStates["elder"]);
    }

    [Test]
    public void Begin_UnsupportedMutationSource_SilentlyIgnored_DoesNotThrow()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.ItemOwned, key = "sword", intValue = 1 });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        Assert.DoesNotThrow(() => _runner.Begin(_tree, "npc_1"));
        Assert.AreEqual("greet", _runner.CurrentNode.id);
    }

    [Test]
    public void Begin_NullMutationEntry_SkippedGracefully_DoesNotThrow()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(null);
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        Assert.DoesNotThrow(() => _runner.Begin(_tree, "npc_1"));
    }

    [Test]
    public void Begin_MutationWithEmptyKey_SkippedForAllSources()
    {
        var node = MakeNode("greet", "NPC", "Hello.");
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.QuestState, key = "", intValue = (int)QuestState.Active });
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.WorldFlag, key = "", intValue = 1 });
        node.mutations.Add(new DialogueMutation { source = DialogueCondition.Source.NpcState, key = "", stringValue = "x" });
        _tree.startNodeId = "greet";
        _tree.nodes.Add(node);
        _tree.Rebuild();

        _runner.Begin(_tree, "npc_1");

        Assert.IsTrue(SaveSystem.CurrentPlayerData.worldFlags == null || SaveSystem.CurrentPlayerData.worldFlags.Count == 0);
        Assert.IsTrue(SaveSystem.CurrentPlayerData.npcStates == null || SaveSystem.CurrentPlayerData.npcStates.Count == 0);
    }

    // --- SelectChoice guard coverage ---

    [Test]
    public void SelectChoice_WhenNotActive_DoesNothing_DoesNotThrow()
    {
        var choice = new DialogueChoice { text = "A", targetNodeId = "a" };

        Assert.DoesNotThrow(() => _runner.SelectChoice(choice));
        Assert.IsFalse(_runner.IsActive);
    }

    [Test]
    public void SelectChoice_NullChoice_DoesNothing_DoesNotThrow()
    {
        _tree.startNodeId = "greet";
        _tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
        _tree.Rebuild();
        _runner.Begin(_tree, "npc_1");

        Assert.DoesNotThrow(() => _runner.SelectChoice(null));
        Assert.AreEqual("greet", _runner.CurrentNode.id);
    }

    // --- OnDestroy coverage ---

    [Test]
    public void OnDestroy_ClearsInstance_WhenThisIsCurrentInstance()
    {
        TestReflectionUtil.InvokeMethod(_runner, "OnDestroy");

        Assert.IsNull(DialogueRunner.Instance);
    }

    [Test]
    public void OnDestroy_DoesNotClearInstance_WhenAnotherInstanceIsCurrent()
    {
        var otherGo = new GameObject("OtherRunner");
        try
        {
            var other = otherGo.AddComponent<DialogueRunner>();
            // Deliberately do NOT call other.Awake() naturally here -- Instance is currently
            // _runner (set in SetUp), so a real Awake() call would take Awake()'s
            // "Instance != this" branch and call Destroy(gameObject), which is invalid in an
            // EditMode test context (must use DestroyImmediate) and would also make `other`
            // never exist for OnDestroy to run against. Set Instance to `other` directly via
            // the same static-backing-field pattern SetUp/TearDown already use, so `other`
            // becomes "the current instance" without going through Awake's self-destroy path.
            SetStaticInstance(other);

            // Now put it back to _runner the same way (not via Awake, same reasoning), so
            // `other` is a real component that was never the live Instance.
            SetStaticInstance(_runner);

            TestReflectionUtil.InvokeMethod(other, "OnDestroy");

            Assert.AreSame(_runner, DialogueRunner.Instance);
        }
        finally
        {
            Object.DestroyImmediate(otherGo);
        }
    }

    // --- Update() coverage (ShouldAdvance factored out as a protected virtual seam so tests
    // can drive Update()'s branches without real UnityEngine.Input state) ---

    private class TestableDialogueRunner : DialogueRunner
    {
        public bool ForceAdvance;

        protected override bool ShouldAdvance() => ForceAdvance;
    }

    // TestReflectionUtil.InvokeMethod resolves via target.GetType(), which for a
    // TestableDialogueRunner instance would look for "Awake"/"Update" declared on the subclass
    // itself -- they're private methods declared on the DialogueRunner base, so a
    // GetType()-rooted lookup misses them (private members are not returned via ordinary
    // inherited-lookup without walking the hierarchy). Invoke directly against the base type
    // instead, which finds them and still dispatches correctly through the real (non-private)
    // virtual ShouldAdvance() override.
    private static void InvokeBase(DialogueRunner target, string methodName)
    {
        var method = typeof(DialogueRunner).GetMethod(methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Invoke(target, null);
    }

    [Test]
    public void Update_WhenNotActive_DoesNothing_DoesNotThrow()
    {
        // SetUp's _runner already holds DialogueRunner.Instance -- clear it first so this new
        // TestableDialogueRunner's own Awake() doesn't take the "Instance != this" self-destroy
        // branch (Destroy(gameObject) is invalid outside Play Mode; TearDown resets Instance to
        // null regardless, so _runner doesn't need it back inside this test).
        SetStaticInstance(null);
        var go = new GameObject("TestableRunner");
        try
        {
            var runner = go.AddComponent<TestableDialogueRunner>();
            InvokeBase(runner, "Awake");
            runner.ForceAdvance = true;

            Assert.DoesNotThrow(() => InvokeBase(runner, "Update"));
            Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Update_ActiveWithChoicesPresent_DoesNotAdvanceEvenWhenInputTrue()
    {
        // SetUp's _runner already holds DialogueRunner.Instance -- clear it first so this new
        // TestableDialogueRunner's own Awake() doesn't take the "Instance != this" self-destroy
        // branch (Destroy(gameObject) is invalid outside Play Mode; TearDown resets Instance to
        // null regardless, so _runner doesn't need it back inside this test).
        SetStaticInstance(null);
        var go = new GameObject("TestableRunner");
        try
        {
            var runner = go.AddComponent<TestableDialogueRunner>();
            InvokeBase(runner, "Awake");

            var greet = MakeNode("greet", "NPC", "Hello.");
            greet.choices.Add(new DialogueChoice { text = "Ask", targetNodeId = "branch" });
            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.startNodeId = "greet";
            tree.nodes.Add(greet);
            tree.nodes.Add(MakeNode("branch", "NPC", "Ok."));
            tree.Rebuild();

            runner.Begin(tree, "npc_1");
            runner.ForceAdvance = true;

            InvokeBase(runner, "Update");

            Assert.AreEqual(GameState.State.Dialogue, GameState.CurrentState);
            Assert.IsTrue(runner.IsActive);

            Object.DestroyImmediate(tree);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Update_ActiveNoChoices_ShouldAdvanceTrue_EndsDialogue()
    {
        // SetUp's _runner already holds DialogueRunner.Instance -- clear it first so this new
        // TestableDialogueRunner's own Awake() doesn't take the "Instance != this" self-destroy
        // branch (Destroy(gameObject) is invalid outside Play Mode; TearDown resets Instance to
        // null regardless, so _runner doesn't need it back inside this test).
        SetStaticInstance(null);
        var go = new GameObject("TestableRunner");
        try
        {
            var runner = go.AddComponent<TestableDialogueRunner>();
            InvokeBase(runner, "Awake");

            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.startNodeId = "greet";
            tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
            tree.Rebuild();

            runner.Begin(tree, "npc_1");
            runner.ForceAdvance = true;

            InvokeBase(runner, "Update");

            Assert.AreEqual(GameState.State.Playing, GameState.CurrentState);
            Assert.IsFalse(runner.IsActive);

            Object.DestroyImmediate(tree);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Update_ActiveNoChoices_ShouldAdvanceFalse_StaysActive()
    {
        // SetUp's _runner already holds DialogueRunner.Instance -- clear it first so this new
        // TestableDialogueRunner's own Awake() doesn't take the "Instance != this" self-destroy
        // branch (Destroy(gameObject) is invalid outside Play Mode; TearDown resets Instance to
        // null regardless, so _runner doesn't need it back inside this test).
        SetStaticInstance(null);
        var go = new GameObject("TestableRunner");
        try
        {
            var runner = go.AddComponent<TestableDialogueRunner>();
            InvokeBase(runner, "Awake");

            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.startNodeId = "greet";
            tree.nodes.Add(MakeNode("greet", "NPC", "Hello."));
            tree.Rebuild();

            runner.Begin(tree, "npc_1");
            runner.ForceAdvance = false;

            InvokeBase(runner, "Update");

            Assert.AreEqual(GameState.State.Dialogue, GameState.CurrentState);
            Assert.IsTrue(runner.IsActive);

            Object.DestroyImmediate(tree);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
