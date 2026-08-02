using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// Full-screen map (charter Step 11). Targets Rebuild()'s discovery-filtering branch logic
// specifically (QA finding, docs/Tasks/2026-08-02-step-11-hud-persistence.md): undiscovered
// shrine nodes are skipped, discovered shrines and every non-shrine kind are always spawned,
// plus the null-guard early-outs and the marker position/label wiring.
public class MapScreenTests
{
    private GameObject _go;
    private MapScreen _map;

    private RegionGraph _graph;
    private GameObject _mapRectGo;
    private RectTransform _mapRect;
    private GameObject _markerPrefab;

    private PlayerData _previousPlayerData;

    [SetUp]
    public void SetUp()
    {
        _previousPlayerData = SaveSystem.CurrentPlayerData;
        SaveSystem.CurrentPlayerData = null;

        _go = new GameObject("MapScreen");
        _map = _go.AddComponent<MapScreen>();
        // AddComponent may or may not auto-invoke OnEnable (=> Rebuild) depending on Editor
        // behavior -- tests invoke Rebuild explicitly via reflection instead of relying on it,
        // same defensive pattern as HealthBarTests/StanceDiamondTests.

        _graph = ScriptableObject.CreateInstance<RegionGraph>();

        _mapRectGo = new GameObject("MapRect");
        _mapRect = _mapRectGo.AddComponent<RectTransform>();

        _markerPrefab = new GameObject("MarkerPrefab", typeof(RectTransform));
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(_markerPrefab.transform);

        TestReflectionUtil.SetField(_map, "regionGraph", _graph);
        TestReflectionUtil.SetField(_map, "mapRect", _mapRect);
        TestReflectionUtil.SetField(_map, "markerPrefab", _markerPrefab);
        TestReflectionUtil.SetField(_map, "worldToMapScale", 1f);
    }

    [TearDown]
    public void TearDown()
    {
        SaveSystem.CurrentPlayerData = _previousPlayerData;

        if (_go != null) Object.DestroyImmediate(_go);
        if (_graph != null) Object.DestroyImmediate(_graph);
        if (_mapRectGo != null) Object.DestroyImmediate(_mapRectGo);
        if (_markerPrefab != null) Object.DestroyImmediate(_markerPrefab);

        // Any markers Rebuild() spawned under _mapRect were destroyed along with _mapRectGo.
    }

    private static RegionNode MakeNode(string id, RegionNode.Kind kind, Vector3 pos, string name)
    {
        return new RegionNode { id = id, kind = kind, worldPosition = pos, displayName = name };
    }

    private List<GameObject> SpawnedMarkers()
    {
        return TestReflectionUtil.GetField<List<GameObject>>(_map, "_spawnedMarkers");
    }

    [Test]
    public void Rebuild_NullRegionGraph_SpawnsNothing_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_map, "regionGraph", null);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_map, "Rebuild"));

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_NullNodesArray_SpawnsNothing_DoesNotThrow()
    {
        _graph.nodes = null;

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_map, "Rebuild"));

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_NullMapRect_SpawnsNothing_DoesNotThrow()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };
        TestReflectionUtil.SetField(_map, "mapRect", null);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_map, "Rebuild"));

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_NullMarkerPrefab_SpawnsNothing_DoesNotThrow()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };
        TestReflectionUtil.SetField(_map, "markerPrefab", null);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_map, "Rebuild"));

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_NullEntryInNodesArray_IsSkipped()
    {
        _graph.nodes = new[] { null, MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(1, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_NonShrineNode_AlwaysSpawned_EvenWithNoDiscoveryState()
    {
        SaveSystem.CurrentPlayerData = null;
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(1, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_ShrineNode_NoPlayerData_IsSkipped()
    {
        SaveSystem.CurrentPlayerData = null;
        _graph.nodes = new[] { MakeNode("shrine_1", RegionNode.Kind.Shrine, Vector3.zero, "Shrine") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_ShrineNode_NotYetDiscovered_IsSkipped()
    {
        SaveSystem.CurrentPlayerData = new PlayerData();
        _graph.nodes = new[] { MakeNode("shrine_1", RegionNode.Kind.Shrine, Vector3.zero, "Shrine") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(0, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_ShrineNode_Discovered_IsSpawned()
    {
        SaveSystem.CurrentPlayerData = new PlayerData();
        SaveSystem.CurrentPlayerData.discoveredShrines.Add("shrine_1");
        _graph.nodes = new[] { MakeNode("shrine_1", RegionNode.Kind.Shrine, Vector3.zero, "Shrine") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(1, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_MixOfDiscoveredAndUndiscoveredShrines_OnlyDiscoveredSpawned()
    {
        SaveSystem.CurrentPlayerData = new PlayerData();
        SaveSystem.CurrentPlayerData.discoveredShrines.Add("shrine_a");
        _graph.nodes = new[]
        {
            MakeNode("shrine_a", RegionNode.Kind.Shrine, Vector3.zero, "Shrine A"),
            MakeNode("shrine_b", RegionNode.Kind.Shrine, Vector3.zero, "Shrine B"),
            MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista"),
        };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(2, SpawnedMarkers().Count);
    }

    [Test]
    public void Rebuild_MarkerPosition_UsesWorldXZ_ScaledByWorldToMapScale()
    {
        TestReflectionUtil.SetField(_map, "worldToMapScale", 2f);
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, new Vector3(3f, 99f, 5f), "Vista") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        var marker = SpawnedMarkers()[0];
        var rect = marker.GetComponent<RectTransform>();
        Assert.AreEqual(new Vector2(6f, 10f), rect.anchoredPosition);
    }

    [Test]
    public void Rebuild_MarkerLabel_SetToNodeDisplayName()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Sunlit Vista") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        var marker = SpawnedMarkers()[0];
        var label = marker.GetComponentInChildren<Text>();
        Assert.AreEqual("Sunlit Vista", label.text);
    }

    [Test]
    public void Rebuild_MarkerIsParentedUnderMapRect()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };

        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        var marker = SpawnedMarkers()[0];
        Assert.AreEqual(_mapRect, marker.transform.parent);
    }

    [Test]
    public void Rebuild_CalledAgain_ClearsPreviousSpawnedMarkersList()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };
        TestReflectionUtil.InvokeMethod(_map, "Rebuild");
        Assert.AreEqual(1, SpawnedMarkers().Count);

        // A second Rebuild() destroys the previously spawned markers via Object.Destroy(),
        // which logs an error-level message when called outside Play Mode (same documented,
        // expected behavior as GameStateTests' Awake_SecondInstance_DestroysDuplicateGameObject).
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode.*", System.Text.RegularExpressions.RegexOptions.Singleline));

        _graph.nodes = new[]
        {
            MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista"),
            MakeNode("vista_2", RegionNode.Kind.Vista, Vector3.zero, "Vista 2"),
        };
        TestReflectionUtil.InvokeMethod(_map, "Rebuild");

        Assert.AreEqual(2, SpawnedMarkers().Count, "The list must reflect only the latest Rebuild(), not accumulate across calls.");
    }

    [Test]
    public void OnEnable_CallsRebuild()
    {
        _graph.nodes = new[] { MakeNode("vista_1", RegionNode.Kind.Vista, Vector3.zero, "Vista") };

        TestReflectionUtil.InvokeMethod(_map, "OnEnable");

        Assert.AreEqual(1, SpawnedMarkers().Count, "OnEnable() must trigger Rebuild().");
    }

    [Test]
    public void Toggle_TogglesActiveState()
    {
        _go.SetActive(true);

        _map.Toggle();
        Assert.IsFalse(_go.activeSelf);

        _map.Toggle();
        Assert.IsTrue(_go.activeSelf);
    }
}
