using UnityEngine;

/// <summary>
/// Pure, stateless checks against the charter Step 9 shrine-spacing convention: a Shrine
/// within walking distance of every Boss node, and a Shrine at the region's Entrance. This is
/// a queryable pass/fail used by tests and (optionally, later) editor tooling — not enforced
/// at authoring time in this task (no in-Editor blocking validation, that's tooling polish
/// out of scope per the Approach doc).
/// </summary>
public static class RegionGraphValidator
{
    /// <summary>
    /// Straight-line distance proxy for "within maxDistance of a Boss node" — a greybox-stage
    /// stand-in for a real pathfinding-based "15-25s walk" check (charter Step 9), not a
    /// promise of actual walkable distance. Vacuously true when the graph has no Boss nodes
    /// at all (nothing to violate the convention against) — documented interpretation, not an
    /// oversight.
    /// </summary>
    public static bool HasShrineNearEveryBoss(RegionGraph graph, float maxDistance)
    {
        if (graph == null || graph.nodes == null)
            return true;

        foreach (var node in graph.nodes)
        {
            if (node == null || node.kind != RegionNode.Kind.Boss)
                continue;

            bool foundNearbyShrine = false;
            foreach (var candidate in graph.nodes)
            {
                if (candidate == null || candidate.kind != RegionNode.Kind.Shrine)
                    continue;

                if (Vector3.Distance(node.worldPosition, candidate.worldPosition) <= maxDistance)
                {
                    foundNearbyShrine = true;
                    break;
                }
            }

            if (!foundNearbyShrine)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks that at least one Shrine node exists at (or very near) an Entrance node's
    /// position — the "rest shrine at every region entrance" half of charter Step 9's
    /// convention. Interpretation: "at the entrance" is treated as within a small fixed
    /// tolerance of an Entrance node's worldPosition, not literal coincidence, since a
    /// hand-authored shrine marker will rarely sit at the exact same point. Vacuously true
    /// when the graph has no Entrance nodes at all, same reasoning as the Boss check above.
    /// </summary>
    public static bool HasEntranceShrine(RegionGraph graph, float tolerance = 10f)
    {
        if (graph == null || graph.nodes == null)
            return true;

        bool hasEntrance = false;

        foreach (var node in graph.nodes)
        {
            if (node == null || node.kind != RegionNode.Kind.Entrance)
                continue;

            hasEntrance = true;

            foreach (var candidate in graph.nodes)
            {
                if (candidate == null || candidate.kind != RegionNode.Kind.Shrine)
                    continue;

                if (Vector3.Distance(node.worldPosition, candidate.worldPosition) <= tolerance)
                    return true;
            }
        }

        return !hasEntrance;
    }
}
