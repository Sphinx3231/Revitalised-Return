using NUnit.Framework;
using UnityEngine;

public class RegionGraphTests
{
    private RegionGraph _graph;

    [SetUp]
    public void SetUp()
    {
        _graph = ScriptableObject.CreateInstance<RegionGraph>();
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "entrance", kind = RegionNode.Kind.Entrance, worldPosition = Vector3.zero, displayName = "Region Entrance" },
            new RegionNode { id = "shrine", kind = RegionNode.Kind.Shrine, worldPosition = new Vector3(3f, 0f, 2f), displayName = "Rest Shrine" },
            new RegionNode { id = "boss", kind = RegionNode.Kind.Boss, worldPosition = new Vector3(0f, 0f, 30f), displayName = "Captain Renzo" },
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_graph);
    }

    [Test]
    public void FindNode_ReturnsCorrectNode_WhenIdExists()
    {
        var node = _graph.FindNode("shrine");

        Assert.IsNotNull(node);
        Assert.AreEqual(RegionNode.Kind.Shrine, node.kind);
        Assert.AreEqual("Rest Shrine", node.displayName);
    }

    [Test]
    public void FindNode_ReturnsNull_WhenIdMissing()
    {
        var node = _graph.FindNode("does-not-exist");

        Assert.IsNull(node);
    }

    [Test]
    public void FindNode_ReturnsNull_WhenNodesArrayIsNull()
    {
        var emptyGraph = ScriptableObject.CreateInstance<RegionGraph>();

        var node = emptyGraph.FindNode("entrance");

        Assert.IsNull(node);
        Object.DestroyImmediate(emptyGraph);
    }
}
