using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored, asset-time itemId -&gt; ItemData lookup (charter Step 11, Research finding 2a).
/// Nothing else in the project can resolve a saved itemId string back to a real ItemData
/// asset -- SaveSystem needs this to reconstruct Inventory on load. ScriptableObject per the
/// StanceData/RegionGraph/ItemData [CreateAssetMenu] precedent. Runtime Dictionary index
/// rebuilt via Rebuild(), same pattern as Inventory.cs's own _index.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Return/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> _index;

    private void OnEnable()
    {
        Rebuild();
    }

    /// <summary>Clears and repopulates the runtime index from `items`, keyed by itemId.</summary>
    public void Rebuild()
    {
        _index = new Dictionary<string, ItemData>();

        foreach (ItemData item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            _index[item.itemId] = item;
        }
    }

    /// <summary>Returns the ItemData for `itemId`, or null if unknown. Lazily rebuilds the index if needed (e.g. asset constructed via ScriptableObject.CreateInstance in a test, bypassing OnEnable timing).</summary>
    public ItemData Lookup(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (_index == null)
            Rebuild();

        return _index.TryGetValue(itemId, out ItemData item) ? item : null;
    }
}
