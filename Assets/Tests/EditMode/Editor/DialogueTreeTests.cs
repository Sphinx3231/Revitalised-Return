using NUnit.Framework;
using UnityEngine;

// DialogueTree's Rebuild()/Lookup() (charter Step 12) -- the third use of the
// Inventory.Rebuild()/ItemDatabase.Rebuild() pattern (Research finding 1). Nodes is a
// List<DialogueNode>, not a Dictionary, since Unity 6000.5's Inspector cannot author Dictionary
// fields -- the runtime index is what actually gives ID-keyed lookup.
public class DialogueTreeTests
{
    private DialogueTree _tree;

    [SetUp]
    public void SetUp()
    {
        _tree = ScriptableObject.CreateInstance<DialogueTree>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_tree != null) Object.DestroyImmediate(_tree);
    }

    private static DialogueNode MakeNode(string id, string speaker)
    {
        return new DialogueNode { id = id, speaker = speaker, text = "text-" + id };
    }

    [Test]
    public void Lookup_KnownId_ReturnsNode()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "Speaker A") };
        _tree.Rebuild();

        DialogueNode result = _tree.Lookup("a");

        Assert.IsNotNull(result);
        Assert.AreEqual("Speaker A", result.speaker);
    }

    [Test]
    public void Lookup_UnknownId_ReturnsNull()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "Speaker A") };
        _tree.Rebuild();

        Assert.IsNull(_tree.Lookup("nonexistent"));
    }

    [Test]
    public void Lookup_NullOrEmptyId_ReturnsNull()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "Speaker A") };
        _tree.Rebuild();

        Assert.IsNull(_tree.Lookup(null));
        Assert.IsNull(_tree.Lookup(string.Empty));
    }

    [Test]
    public void Lookup_NullNodeEntryInList_IsSkipped_DoesNotThrow()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { null, MakeNode("a", "Speaker A") };

        Assert.DoesNotThrow(() => _tree.Rebuild());
        Assert.IsNotNull(_tree.Lookup("a"));
    }

    [Test]
    public void Lookup_NodeWithEmptyId_IsSkipped()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("", "No Id"), MakeNode("a", "Speaker A") };
        _tree.Rebuild();

        Assert.IsNotNull(_tree.Lookup("a"));
    }

    [Test]
    public void Lookup_NullNodesList_DoesNotThrow_ReturnsNull()
    {
        _tree.nodes = null;

        Assert.DoesNotThrow(() => _tree.Rebuild());
        Assert.IsNull(_tree.Lookup("a"));
    }

    [Test]
    public void Lookup_WithoutExplicitRebuild_LazilyBuildsIndex()
    {
        // ScriptableObject.CreateInstance DOES run OnEnable in this Editor/test context
        // (confirmed empirically by ChestTests/ItemDatabaseTests' own use of this exact
        // pattern) -- so to specifically exercise Lookup's *lazy* rebuild path (the case
        // Research flagged: an index built despite OnEnable timing not having fired), this
        // test forces the private index field back to null via reflection before calling
        // Lookup, simulating a construction path where OnEnable never ran.
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "Speaker A") };
        TestReflectionUtil.SetField(_tree, "_index", null);

        DialogueNode result = _tree.Lookup("a");

        Assert.IsNotNull(result, "Lookup must lazily rebuild the index when it is null.");
    }

    [Test]
    public void Rebuild_CalledAgain_ReplacesPreviousIndex_NotAccumulates()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "A") };
        _tree.Rebuild();
        Assert.IsNotNull(_tree.Lookup("a"));

        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("b", "B") };
        _tree.Rebuild();

        Assert.IsNull(_tree.Lookup("a"), "Stale entries from a previous Rebuild() must not survive.");
        Assert.IsNotNull(_tree.Lookup("b"));
    }

    [Test]
    public void OnEnable_CallsRebuild()
    {
        _tree.nodes = new System.Collections.Generic.List<DialogueNode> { MakeNode("a", "A") };

        TestReflectionUtil.InvokeMethod(_tree, "OnEnable");

        Assert.IsNotNull(_tree.Lookup("a"));
    }
}
