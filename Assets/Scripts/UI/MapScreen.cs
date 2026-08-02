using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen map, inactive by default, toggled by the new `map` input action (M key,
/// charter Step 11). Reads RegionGraph for POI position/name/kind -- authored, static data --
/// and SaveSystem.CurrentPlayerData.discoveredShrines for discovery state -- runtime data.
/// This split is deliberate (Research finding 5 / RegionGraph.cs's own doc comment): discovery
/// state is never mutated onto the authored RegionGraph asset.
///
/// Placeholder visual treatment (still placeholder-art territory per every prior UI step):
/// undiscovered shrine nodes are skipped entirely (not greyed -- simplest option that still
/// reads correctly at a glance, no new "discovered vs not" sprite state needed). All non-shrine
/// node kinds are always shown -- this task doesn't add a fog-of-war system for the whole map,
/// only shrine-specific reveal-on-rest per the charter's locked wording ("per-region reveal on
/// first shrine rest").
/// </summary>
public class MapScreen : MonoBehaviour
{
    [SerializeField] private RegionGraph regionGraph;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private float worldToMapScale = 1f;

    private readonly List<GameObject> _spawnedMarkers = new List<GameObject>();

    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>Toggles this screen's active state -- called by PlayerInputReader's `map` action handler.</summary>
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void Rebuild()
    {
        for (int i = 0; i < _spawnedMarkers.Count; i++)
        {
            if (_spawnedMarkers[i] != null)
                Destroy(_spawnedMarkers[i]);
        }
        _spawnedMarkers.Clear();

        if (regionGraph == null || regionGraph.nodes == null || mapRect == null || markerPrefab == null)
            return;

        HashSet<string> discovered = SaveSystem.CurrentPlayerData != null ? SaveSystem.CurrentPlayerData.discoveredShrines : null;

        foreach (RegionNode node in regionGraph.nodes)
        {
            if (node == null)
                continue;

            bool isShrine = node.kind == RegionNode.Kind.Shrine;
            if (isShrine && (discovered == null || !discovered.Contains(node.id)))
                continue;

            GameObject markerGo = Instantiate(markerPrefab, mapRect);
            var markerRect = markerGo.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.anchoredPosition = new Vector2(
                    node.worldPosition.x * worldToMapScale,
                    node.worldPosition.z * worldToMapScale);
            }

            Text label = markerGo.GetComponentInChildren<Text>();
            if (label != null)
                label.text = node.displayName;

            _spawnedMarkers.Add(markerGo);
        }
    }
}
