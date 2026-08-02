using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored, asset-time dialogue tree (charter Step 12). ScriptableObject per
/// StanceData/ItemData/RegionGraph's [CreateAssetMenu] precedent. `nodes` is a List, not a
/// Dictionary&lt;string,DialogueNode&gt; -- Research finding 1 confirmed (against version-pinned
/// 6000.5.5f1 docs) that Unity's Inspector cannot author Dictionary fields until 6000.6, so the
/// ID-keyed lookup charter Step 12 asks for is built at runtime instead: a non-serialized
/// Dictionary index rebuilt via Rebuild(), the third use of this exact pattern
/// (Inventory.Rebuild(), ItemDatabase.Rebuild()).
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Return/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    public string dialogueId;
    public string startNodeId;
    public List<DialogueNode> nodes = new List<DialogueNode>();

    private Dictionary<string, DialogueNode> _index;

    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>Clears and repopulates the runtime index from `nodes`, keyed by id.</summary>
    public void Rebuild()
    {
        _index = new Dictionary<string, DialogueNode>();

        if (nodes == null)
            return;

        foreach (DialogueNode node in nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id))
                continue;

            _index[node.id] = node;
        }
    }

    /// <summary>
    /// Returns the DialogueNode for `nodeId`, or null if unknown. Lazily rebuilds the index if
    /// needed (e.g. asset constructed via ScriptableObject.CreateInstance in a test, bypassing
    /// OnEnable timing).
    /// </summary>
    public DialogueNode Lookup(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return null;

        if (_index == null)
            Rebuild();

        return _index.TryGetValue(nodeId, out DialogueNode node) ? node : null;
    }
}
