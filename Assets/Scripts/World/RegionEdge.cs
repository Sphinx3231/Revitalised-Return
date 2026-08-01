/// <summary>
/// A directed connection between two <see cref="RegionNode"/> ids within a
/// <see cref="RegionGraph"/> (charter Step 9). String-id references, not direct node object
/// references, per Research's finding: self-referencing node objects / Dictionary fields
/// serialize poorly on a ScriptableObject.
/// </summary>
[System.Serializable]
public class RegionEdge
{
    public string fromId;
    public string toId;
}
