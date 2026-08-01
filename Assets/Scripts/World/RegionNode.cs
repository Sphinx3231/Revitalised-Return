using UnityEngine;

/// <summary>
/// A single point of interest within a <see cref="RegionGraph"/> (charter Step 9). Plain
/// serializable data — no behavior, matching the charter's "plain C# class" option for the
/// node/edge granularity of this data model (the graph itself is the ScriptableObject; nodes
/// and edges are its serialized payload, same reasoning StanceData's own flat-field layout
/// uses for scalar tuning data).
/// </summary>
[System.Serializable]
public class RegionNode
{
    /// <summary>Locked enum (charter Step 9) — the full set of region point-of-interest kinds.</summary>
    public enum Kind
    {
        Entrance,
        Shrine,
        Encounter,
        Arena,
        Vista,
        Loot,
        Npc,
        Gate,
        Boss,
        SideDomain
    }

    public string id;
    public Kind kind;
    public Vector3 worldPosition;
    public string displayName;
}
