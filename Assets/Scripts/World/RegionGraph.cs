using UnityEngine;

/// <summary>
/// Authored, asset-time description of one region's points of interest and critical path
/// (charter Step 9). ScriptableObject per StanceData's [CreateAssetMenu] precedent — a
/// hand-authored .asset per region, not runtime-constructed state. Runtime save state (e.g.
/// discoveredShrines) belongs on the future PlayerData, never mutated onto this authored SO.
/// </summary>
[CreateAssetMenu(fileName = "NewRegionGraph", menuName = "Return/Region Graph")]
public class RegionGraph : ScriptableObject
{
    public string regionId;
    public string skylineAnchor;
    public RegionNode[] nodes;
    public RegionEdge[] edges;
    public string[] criticalPath;

    /// <summary>
    /// Linear search by node id. Returns null if no node with that id exists. Simple by
    /// design — a region's node count is a handful, not a scale where a Dictionary index is
    /// worth the added complexity/serialization cost.
    /// </summary>
    public RegionNode FindNode(string id)
    {
        if (nodes == null)
            return null;

        foreach (var node in nodes)
        {
            if (node != null && node.id == id)
                return node;
        }

        return null;
    }
}
