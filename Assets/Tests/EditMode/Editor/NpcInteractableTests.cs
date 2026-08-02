using NUnit.Framework;
using UnityEngine;

// NpcInteractable (charter Step 12) -- subclasses Interactable exactly like Chest/Shrine, does
// NOT override CanInteract (NPCs stay talkable, unlike Chest's one-shot depletion). Kicks off
// dialogue via a direct call to DialogueRunner.Instance.Begin.
public class NpcInteractableTests
{
    private GameObject _npcGo;
    private NpcInteractable _npc;

    private GameObject _runnerGo;
    private DialogueRunner _runner;

    private DialogueTree _tree;

    [SetUp]
    public void SetUp()
    {
        _npcGo = new GameObject("Npc", typeof(BoxCollider));
        _npc = _npcGo.AddComponent<NpcInteractable>();

        _tree = ScriptableObject.CreateInstance<DialogueTree>();

        TestReflectionUtil.SetField(_npc, "dialogueTree", _tree);
        TestReflectionUtil.SetField(_npc, "npcId", "wanderer_1");

        SetStaticInstance(null);
    }

    [TearDown]
    public void TearDown()
    {
        GameState.SetState(GameState.State.Playing);

        if (_npcGo != null) Object.DestroyImmediate(_npcGo);
        if (_runnerGo != null) Object.DestroyImmediate(_runnerGo);
        if (_tree != null) Object.DestroyImmediate(_tree);

        SetStaticInstance(null);
    }

    private static void SetStaticInstance(DialogueRunner instance)
    {
        var field = typeof(DialogueRunner).GetField("<Instance>k__BackingField",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, instance);
    }

    private DialogueRunner CreateRunner()
    {
        _runnerGo = new GameObject("DialogueRunner");
        _runner = _runnerGo.AddComponent<DialogueRunner>();
        TestReflectionUtil.InvokeMethod(_runner, "Awake");
        return _runner;
    }

    [Test]
    public void CanInteract_AlwaysTrue_NotOverridden()
    {
        Assert.IsTrue(_npc.CanInteract(_npcGo.transform));
    }

    [Test]
    public void CanInteract_StaysTrueAfterInteracting_UnlikeChest()
    {
        CreateRunner();

        _npc.Interact(_npcGo.transform);

        Assert.IsTrue(_npc.CanInteract(_npcGo.transform), "NPCs must stay talkable indefinitely, no one-shot depletion.");
    }

    [Test]
    public void Interact_CallsDialogueRunnerInstanceBegin()
    {
        CreateRunner();
        _tree.startNodeId = "greet";
        _tree.nodes.Add(new DialogueNode { id = "greet", speaker = "NPC", text = "Hello." });
        _tree.Rebuild();

        _npc.Interact(_npcGo.transform);

        Assert.AreSame(_tree, _runner.CurrentTree);
        Assert.AreEqual(GameState.State.Dialogue, GameState.CurrentState);
    }

    [Test]
    public void Interact_NoDialogueRunnerInstance_DoesNotThrow()
    {
        // DialogueRunner.Instance is null (no runner created this test) -- Interact must
        // degrade gracefully via the null-conditional call, not throw.
        Assert.DoesNotThrow(() => _npc.Interact(_npcGo.transform));
    }
}
