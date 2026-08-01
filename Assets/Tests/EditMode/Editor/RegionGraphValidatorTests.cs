using NUnit.Framework;
using UnityEngine;

public class RegionGraphValidatorTests
{
    private RegionGraph _graph;

    [SetUp]
    public void SetUp()
    {
        _graph = ScriptableObject.CreateInstance<RegionGraph>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_graph);
    }

    [Test]
    public void HasShrineNearEveryBoss_ReturnsTrue_WhenShrineWithinRange()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "boss", kind = RegionNode.Kind.Boss, worldPosition = new Vector3(0f, 0f, 30f) },
            new RegionNode { id = "shrine", kind = RegionNode.Kind.Shrine, worldPosition = new Vector3(0f, 0f, 20f) },
        };

        bool result = RegionGraphValidator.HasShrineNearEveryBoss(_graph, 15f);

        Assert.IsTrue(result);
    }

    [Test]
    public void HasShrineNearEveryBoss_ReturnsFalse_WhenNoShrineInRange()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "boss", kind = RegionNode.Kind.Boss, worldPosition = new Vector3(0f, 0f, 30f) },
            new RegionNode { id = "shrine", kind = RegionNode.Kind.Shrine, worldPosition = new Vector3(0f, 0f, 0f) },
        };

        bool result = RegionGraphValidator.HasShrineNearEveryBoss(_graph, 15f);

        Assert.IsFalse(result);
    }

    [Test]
    public void HasShrineNearEveryBoss_ReturnsTrue_WhenNoBossNodes()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "entrance", kind = RegionNode.Kind.Entrance, worldPosition = Vector3.zero },
        };

        bool result = RegionGraphValidator.HasShrineNearEveryBoss(_graph, 15f);

        Assert.IsTrue(result, "Vacuously true: no Boss nodes means nothing to violate the convention against.");
    }

    [Test]
    public void HasShrineNearEveryBoss_ReturnsTrue_WhenGraphNodesIsNull()
    {
        bool result = RegionGraphValidator.HasShrineNearEveryBoss(_graph, 15f);

        Assert.IsTrue(result);
    }

    [Test]
    public void HasEntranceShrine_ReturnsTrue_WhenShrineNearEntrance()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "entrance", kind = RegionNode.Kind.Entrance, worldPosition = Vector3.zero },
            new RegionNode { id = "shrine", kind = RegionNode.Kind.Shrine, worldPosition = new Vector3(3f, 0f, 2f) },
        };

        bool result = RegionGraphValidator.HasEntranceShrine(_graph);

        Assert.IsTrue(result);
    }

    [Test]
    public void HasEntranceShrine_ReturnsFalse_WhenNoShrineNearEntrance()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "entrance", kind = RegionNode.Kind.Entrance, worldPosition = Vector3.zero },
            new RegionNode { id = "shrine", kind = RegionNode.Kind.Shrine, worldPosition = new Vector3(500f, 0f, 500f) },
        };

        bool result = RegionGraphValidator.HasEntranceShrine(_graph);

        Assert.IsFalse(result);
    }

    [Test]
    public void HasEntranceShrine_ReturnsTrue_WhenNoEntranceNodes()
    {
        _graph.nodes = new RegionNode[]
        {
            new RegionNode { id = "boss", kind = RegionNode.Kind.Boss, worldPosition = Vector3.zero },
        };

        bool result = RegionGraphValidator.HasEntranceShrine(_graph);

        Assert.IsTrue(result, "Vacuously true: no Entrance nodes means nothing to violate the convention against.");
    }

    [Test]
    public void HasEntranceShrine_ReturnsTrue_WhenGraphNodesIsNull()
    {
        bool result = RegionGraphValidator.HasEntranceShrine(_graph);

        Assert.IsTrue(result);
    }
}
